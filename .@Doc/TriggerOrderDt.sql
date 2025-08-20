CREATE TRIGGER dbo.trg_OrderDt_Stock_AUD
ON dbo.OrderDt
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        DECLARE @userId INT = TRY_CAST(SESSION_CONTEXT(N'user_id') AS INT);

        -- สรุปผลต่างจำนวนต่อ (ProductId, LocationId)
        DECLARE @deltas TABLE (
            ProductId  INT NOT NULL,
            LocationId INT NOT NULL,
            DeltaQty   INT NOT NULL
        );

        INSERT INTO @deltas (ProductId, LocationId, DeltaQty)
        SELECT
            COALESCE(i.ProductId, d.ProductId)   AS ProductId,
            COALESCE(i.LocationId, d.LocationId) AS LocationId,
            SUM(ISNULL(i.Quantity,0) - ISNULL(d.Quantity,0)) AS DeltaQty
        FROM inserted i
        FULL OUTER JOIN deleted d ON i.Id = d.Id
        GROUP BY COALESCE(i.ProductId, d.ProductId),
                 COALESCE(i.LocationId, d.LocationId);

        -- ถ้าไม่มีการเปลี่ยนแปลง ปิดงาน
        IF NOT EXISTS (SELECT 1 FROM @deltas WHERE DeltaQty <> 0)
            RETURN;

        -- 0) ต้องมีแถวใน ProductStocks สำหรับทุกคู่ Prod/Loc
        IF EXISTS (
            SELECT 1
            FROM @deltas x
            LEFT JOIN dbo.ProductStocks ps
              ON ps.ProductId = x.ProductId AND ps.LocationId = x.LocationId
            WHERE x.DeltaQty <> 0 AND ps.ProductId IS NULL
        )
        BEGIN
            RAISERROR(N'ProductStocks row not found for some Product/Location in OrderDt trigger.', 16, 1);
            ROLLBACK TRANSACTION; RETURN;
        END

        -- 1) ตรวจของพอในเคสขายเพิ่ม (ตัดออก)
        IF EXISTS (
            SELECT 1
            FROM @deltas x
            JOIN dbo.ProductStocks ps
              ON ps.ProductId = x.ProductId AND ps.LocationId = x.LocationId
            WHERE x.DeltaQty > 0
              AND (ps.QtyOnHand - ps.QtyReserved - ps.QtyDamaged) < x.DeltaQty
        )
        BEGIN
            RAISERROR(N'Insufficient available quantity to ship (OrderDt).', 16, 1);
            ROLLBACK TRANSACTION; RETURN;
        END

        -- 2) อัปเดตยอดคงเหลือใน ProductStocks
        -- ขายเพิ่ม → ตัดออก
        UPDATE ps
           SET ps.QtyOnHand = ps.QtyOnHand - x.DeltaQty,
               ps.UpdatedAt = SYSUTCDATETIME()
        FROM dbo.ProductStocks ps
        JOIN @deltas x
          ON x.ProductId = ps.ProductId AND x.LocationId = ps.LocationId
        WHERE x.DeltaQty > 0;

        -- ยกเลิก/ลดจำนวน → รับคืน
        UPDATE ps
           SET ps.QtyOnHand = ps.QtyOnHand + ABS(x.DeltaQty),
               ps.UpdatedAt = SYSUTCDATETIME()
        FROM dbo.ProductStocks ps
        JOIN @deltas x
          ON x.ProductId = ps.ProductId AND x.LocationId = ps.LocationId
        WHERE x.DeltaQty < 0;

        -- 3) อัปเดตยอดรวม Products.Quantity (ถ้าตารางนี้เก็บยอดรวม)
        UPDATE p
           SET p.Quantity = p.Quantity - x.DeltaQty
        FROM dbo.Products p
        JOIN @deltas x ON x.ProductId = p.Id
        WHERE x.DeltaQty > 0;

        UPDATE p
           SET p.Quantity = p.Quantity + ABS(x.DeltaQty)
        FROM dbo.Products p
        JOIN @deltas x ON x.ProductId = p.Id
        WHERE x.DeltaQty < 0;

        /* 4) ลงประวัติ StockTransactions
              - INSERT only  → SALE
              - DELETE only  → SALE_VOID
              - UPDATE (prod/loc เดิม qty เปลี่ยน) → SALE/SALE_VOID ตามทิศทาง
              - UPDATE (เปลี่ยน prod/loc) → VOID ของเก่า + SALE ของใหม่
           ใช้ OrderNo จาก OrderHd (fallback เป็น OrderHdId) เป็น ReferenceId
        */

        -- INSERT only → SALE
        INSERT INTO dbo.StockTransactions
            (ProductId, FromLocationId, ToLocationId, QtyChange,
             ReasonCode, ReferenceType, ReferenceId, PerformedByUserId, Note)
        SELECT  i.ProductId,
                i.LocationId,         -- ตัดออกจาก loc นี้
                NULL,
                i.Quantity,           -- เก็บเป็นจำนวนบวก
                'SALE',
                'SO',
                COALESCE(oh.OrderNo, CONVERT(nvarchar(50), i.OrderHdId)),
                @userId,
                CONCAT('OrderDt#', i.Id)
        FROM inserted i
        LEFT JOIN deleted d ON d.Id = i.Id
        LEFT JOIN dbo.OrderHd oh ON oh.Id = i.OrderHdId
        WHERE d.Id IS NULL AND i.Quantity > 0;

        -- DELETE only → SALE_VOID (รับคืน)
        INSERT INTO dbo.StockTransactions
            (ProductId, FromLocationId, ToLocationId, QtyChange,
             ReasonCode, ReferenceType, ReferenceId, PerformedByUserId, Note)
        SELECT  d.ProductId,
                NULL,
                d.LocationId,
                d.Quantity,
                'SALE_VOID',
                'SO',
                COALESCE(oh.OrderNo, CONVERT(nvarchar(50), d.OrderHdId)),
                @userId,
                CONCAT('OrderDt#', d.Id)
        FROM deleted d
        LEFT JOIN inserted i ON i.Id = d.Id
        LEFT JOIN dbo.OrderHd oh ON oh.Id = d.OrderHdId
        WHERE i.Id IS NULL AND d.Quantity > 0;

        -- UPDATE: product/location เดิมแต่ Quantity เปลี่ยน
        INSERT INTO dbo.StockTransactions
            (ProductId, FromLocationId, ToLocationId, QtyChange,
             ReasonCode, ReferenceType, ReferenceId, PerformedByUserId, Note)
        SELECT  i.ProductId,
                CASE WHEN (i.Quantity - d.Quantity) > 0 THEN i.LocationId ELSE NULL END,
                CASE WHEN (i.Quantity - d.Quantity) < 0 THEN i.LocationId ELSE NULL END,
                ABS(i.Quantity - d.Quantity),
                CASE WHEN (i.Quantity - d.Quantity) > 0 THEN 'SALE' ELSE 'SALE_VOID' END,
                'SO',
                COALESCE(oh.OrderNo, CONVERT(nvarchar(50), i.OrderHdId)),
                @userId,
                CONCAT('OrderDt#', i.Id, ' (QTY EDIT)')
        FROM inserted i
        JOIN deleted d ON d.Id = i.Id
        LEFT JOIN dbo.OrderHd oh ON oh.Id = i.OrderHdId
        WHERE i.ProductId = d.ProductId
          AND i.LocationId = d.LocationId
          AND i.Quantity <> d.Quantity;

        -- UPDATE: เปลี่ยน Product/Location → VOID ของเก่า
        INSERT INTO dbo.StockTransactions
            (ProductId, FromLocationId, ToLocationId, QtyChange,
             ReasonCode, ReferenceType, ReferenceId, PerformedByUserId, Note)
        SELECT  d.ProductId,
                NULL,
                d.LocationId,
                d.Quantity,
                'SALE_VOID',
                'SO',
                COALESCE(oh.OrderNo, CONVERT(nvarchar(50), d.OrderHdId)),
                @userId,
                CONCAT('OrderDt#', d.Id, ' (MOVE-OLD)')
        FROM inserted i
        JOIN deleted d ON d.Id = i.Id
        LEFT JOIN dbo.OrderHd oh ON oh.Id = d.OrderHdId
        WHERE (i.ProductId <> d.ProductId OR i.LocationId <> d.LocationId)
          AND d.Quantity > 0;

        -- UPDATE: เปลี่ยน Product/Location → SALE ของใหม่
        INSERT INTO dbo.StockTransactions
            (ProductId, FromLocationId, ToLocationId, QtyChange,
             ReasonCode, ReferenceType, ReferenceId, PerformedByUserId, Note)
        SELECT  i.ProductId,
                i.LocationId,
                NULL,
                i.Quantity,
                'SALE',
                'SO',
                COALESCE(oh.OrderNo, CONVERT(nvarchar(50), i.OrderHdId)),
                @userId,
                CONCAT('OrderDt#', i.Id, ' (MOVE-NEW)')
        FROM inserted i
        JOIN deleted d ON d.Id = i.Id
        LEFT JOIN dbo.OrderHd oh ON oh.Id = i.OrderHdId
        WHERE (i.ProductId <> d.ProductId OR i.LocationId <> d.LocationId)
          AND i.Quantity > 0;

    END TRY
    BEGIN CATCH
        DECLARE @msg NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @sev INT = ERROR_SEVERITY();
        DECLARE @st  INT = ERROR_STATE();
        RAISERROR(@msg, @sev, @st);
        ROLLBACK TRANSACTION; RETURN;
    END CATCH
END
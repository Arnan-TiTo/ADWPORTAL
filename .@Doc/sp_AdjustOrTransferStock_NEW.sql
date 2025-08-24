CREATE OR ALTER PROCEDURE dbo.sp_AdjustOrTransferStock
    @ProductId          int,
    @FromLocationId     int = NULL,   -- NULL = รับเข้า (Adjust IN)
    @ToLocationId       int = NULL,   -- NULL = ตัดออก (Adjust OUT)
    @Qty                int,          -- โอน: ต้องเป็นบวก; Single-leg: ใช้ค่าสัมบูรณ์
    @ReasonCode         nvarchar(50),
    @ReferenceType      nvarchar(50) = NULL,
    @ReferenceId        nvarchar(100) = NULL,
    @PerformedByUserId  int = NULL,
    @Note               nvarchar(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @QtyAbs int = ABS(@Qty);

    IF (@FromLocationId IS NOT NULL AND @ToLocationId IS NOT NULL AND @QtyAbs <= 0)
        THROW 50001, 'For transfer, @Qty must be positive.', 1;

    BEGIN TRAN;

    /* ==================== TRANSFER ==================== */
    IF (@FromLocationId IS NOT NULL AND @ToLocationId IS NOT NULL)
    BEGIN
        -- ensure rows
        IF NOT EXISTS (SELECT 1 FROM dbo.ProductStocks WHERE ProductId=@ProductId AND LocationId=@FromLocationId)
            INSERT INTO dbo.ProductStocks(ProductId, LocationId, QtyOnHand, QtyReserved, QtyDamaged, QtyReceive, UpdatedAt)
            VALUES (@ProductId, @FromLocationId, 0,0,0,0, SYSUTCDATETIME());

        IF NOT EXISTS (SELECT 1 FROM dbo.ProductStocks WHERE ProductId=@ProductId AND LocationId=@ToLocationId)
            INSERT INTO dbo.ProductStocks(ProductId, LocationId, QtyOnHand, QtyReserved, QtyDamaged, QtyReceive, UpdatedAt)
            VALUES (@ProductId, @ToLocationId, 0,0,0,0, SYSUTCDATETIME());

        -- check stock
        DECLARE @fromOnHand int;
        SELECT @fromOnHand = QtyOnHand
          FROM dbo.ProductStocks WITH (UPDLOCK, HOLDLOCK)
         WHERE ProductId=@ProductId AND LocationId=@FromLocationId;

        IF (@fromOnHand < @QtyAbs) THROW 50002, 'Insufficient stock at source location.', 1;

        -- flags
        DECLARE @fromIsWh bit=0, @fromIsStore bit=0, @toIsWh bit=0, @toIsStore bit=0;
        SELECT @fromIsWh = CAST(IsWarehouse AS bit),
               @fromIsStore = CAST(ISNULL(IsStorehouse,0) AS bit)
          FROM dbo.Locations WHERE Id=@FromLocationId;

        SELECT @toIsWh = CAST(IsWarehouse AS bit),
               @toIsStore = CAST(ISNULL(IsStorehouse,0) AS bit)
          FROM dbo.Locations WHERE Id=@ToLocationId;

        -- 1) ตัดออกจาก From
        UPDATE dbo.ProductStocks
           SET QtyOnHand = QtyOnHand - @QtyAbs,
               UpdatedAt = SYSUTCDATETIME()
         WHERE ProductId=@ProductId AND LocationId=@FromLocationId;

        -- 2) กติกาใหม่: From=(1,0) และ To=(0,1) → ไปลง QtyReceive ปลายทาง
        IF (ISNULL(@fromIsWh,0)=1 AND ISNULL(@fromIsStore,0)=0
            AND ISNULL(@toIsWh,0)=0 AND ISNULL(@toIsStore,0)=1)
        BEGIN
            UPDATE dbo.ProductStocks
               SET QtyReceive = QtyReceive + @QtyAbs,
                   UpdatedAt  = SYSUTCDATETIME()
             WHERE ProductId=@ProductId AND LocationId=@ToLocationId;
        END
        ELSE
        BEGIN
            -- ปกติ → บวกเข้า OnHand ปลายทาง
            UPDATE dbo.ProductStocks
               SET QtyOnHand = QtyOnHand + @QtyAbs,
                   UpdatedAt  = SYSUTCDATETIME()
             WHERE ProductId=@ProductId AND LocationId=@ToLocationId;
        END

        -- log
        INSERT INTO dbo.StockTransactions
            (ProductId, FromLocationId, ToLocationId, QtyChange,
             ReasonCode, ReferenceType, ReferenceId, PerformedByUserId, Note)
        VALUES
            (@ProductId, @FromLocationId, @ToLocationId, @QtyAbs,
             'TRANSFER', @ReferenceType, @ReferenceId, @PerformedByUserId, @Note);
    END
    /* ==================== SINGLE-LEG ==================== */
    ELSE
    BEGIN
        DECLARE @targetLocId int = COALESCE(@ToLocationId, @FromLocationId);
        IF (@targetLocId IS NULL) THROW 50003, 'Either FromLocationId or ToLocationId must be provided.', 1;

        IF NOT EXISTS (SELECT 1 FROM dbo.ProductStocks WHERE ProductId=@ProductId AND LocationId=@targetLocId)
            INSERT INTO dbo.ProductStocks(ProductId, LocationId, QtyOnHand, QtyReserved, QtyDamaged, QtyReceive, UpdatedAt)
            VALUES (@ProductId, @targetLocId, 0,0,0,0, SYSUTCDATETIME());

        -- OUT
        IF (@FromLocationId IS NOT NULL)
        BEGIN
            DECLARE @oh int;
            SELECT @oh = QtyOnHand
              FROM dbo.ProductStocks WITH (UPDLOCK, HOLDLOCK)
             WHERE ProductId=@ProductId AND LocationId=@targetLocId;

            IF (@oh < @QtyAbs) THROW 50004, 'Insufficient stock for adjust-out.', 1;

            UPDATE dbo.ProductStocks
               SET QtyOnHand = QtyOnHand - @QtyAbs,
                   UpdatedAt  = SYSUTCDATETIME()
             WHERE ProductId=@ProductId AND LocationId=@targetLocId;

            INSERT INTO dbo.StockTransactions
                (ProductId, FromLocationId, ToLocationId, QtyChange, ReasonCode, ReferenceType, ReferenceId, PerformedByUserId, Note)
            VALUES
                (@ProductId, @targetLocId, NULL, -@QtyAbs, @ReasonCode, @ReferenceType, @ReferenceId, @PerformedByUserId, @Note);
        END
        ELSE
        BEGIN
            -- IN
            DECLARE @isWh bit=0, @isStore bit=0;
            SELECT @isWh = CAST(IsWarehouse AS bit),
                   @isStore = CAST(ISNULL(IsStorehouse,0) AS bit)
              FROM dbo.Locations WHERE Id=@targetLocId;

            IF (@isWh = 1 AND @isStore = 0)
            BEGIN
                -- Warehouse: รับเข้า OnHand ปกติ
                UPDATE dbo.ProductStocks
                   SET QtyOnHand = QtyOnHand + @QtyAbs,
                       UpdatedAt  = SYSUTCDATETIME()
                 WHERE ProductId=@ProductId AND LocationId=@targetLocId;

                INSERT INTO dbo.StockTransactions
                    (ProductId, FromLocationId, ToLocationId, QtyChange, ReasonCode, ReferenceType, ReferenceId, PerformedByUserId, Note)
                VALUES
                    (@ProductId, NULL, @targetLocId, @QtyAbs, @ReasonCode, @ReferenceType, @ReferenceId, @PerformedByUserId, @Note);
            END
            ELSE IF (@isWh = 0 AND @isStore = 1)
            BEGIN
                -- Storehouse: ตัดจาก QtyReceive -> OnHand
                DECLARE @recv int;
                SELECT @recv = QtyReceive
                  FROM dbo.ProductStocks WITH (UPDLOCK, HOLDLOCK)
                 WHERE ProductId=@ProductId AND LocationId=@targetLocId;

                IF (@recv < @QtyAbs) THROW 50006, 'Insufficient QtyReceive to convert to OnHand.', 1;

                UPDATE dbo.ProductStocks
                   SET QtyReceive = QtyReceive - @QtyAbs,
                       QtyOnHand  = QtyOnHand  + @QtyAbs,
                       UpdatedAt  = SYSUTCDATETIME()
                 WHERE ProductId=@ProductId AND LocationId=@targetLocId;

                INSERT INTO dbo.StockTransactions
                    (ProductId, FromLocationId, ToLocationId, QtyChange, ReasonCode, ReferenceType, ReferenceId, PerformedByUserId, Note)
                VALUES
                    (@ProductId, NULL, @targetLocId, @QtyAbs, @ReasonCode, @ReferenceType, @ReferenceId, @PerformedByUserId, @Note);
            END
            ELSE IF (@isWh = 0 AND @isStore = 0)
            BEGIN
                THROW 50007, 'Adjust IN is not allowed for this location.', 1;
            END
            ELSE
            BEGIN
                -- fallback (ทั้งสองเป็น 1 หรือข้อมูลผิดปกติ) → รับเข้า OnHand
                UPDATE dbo.ProductStocks
                   SET QtyOnHand = QtyOnHand + @QtyAbs,
                       UpdatedAt  = SYSUTCDATETIME()
                 WHERE ProductId=@ProductId AND LocationId=@targetLocId;

                INSERT INTO dbo.StockTransactions
                    (ProductId, FromLocationId, ToLocationId, QtyChange, ReasonCode, ReferenceType, ReferenceId, PerformedByUserId, Note)
                VALUES
                    (@ProductId, NULL, @targetLocId, @QtyAbs, @ReasonCode, @ReferenceType, @ReferenceId, @PerformedByUserId, @Note);
            END
        END
    END

    COMMIT;
END


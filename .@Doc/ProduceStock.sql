USE [VCIN_WEBAPP];
GO

--ExcCommandLog เก็บประวัติการ Call excCmd 
CREATE TABLE dbo.ExcCommandLog (
    Id int IDENTITY(1,1) NOT NULL,
    UserId int NOT NULL,
    SqlCommand nvarchar(MAX) COLLATE Thai_CI_AI NOT NULL,
    Response nvarchar(MAX) COLLATE Thai_CI_AI NOT NULL,
    CONSTRAINT PK_ExcCommandLog PRIMARY KEY (Id)
);


--แก้ไขการเข้าถึงแอพ
ALTER TABLE dbo.Users ALTER COLUMN [Role] nvarchar(MAX) COLLATE Thai_CI_AI NOT NULL;
ALTER TABLE dbo.Users ADD isActive int DEFAULT 0 NOT NULL;
ALTER TABLE dbo.Users ADD isDelete int DEFAULT 0 NOT NULL;


--Map User Location สร้างตาราง โดย CASCADE เฉพาะฝั่ง Location
CREATE TABLE dbo.UserLocations (
    UserId     int NOT NULL,
    LocationId int NOT NULL,
    CONSTRAINT PK_UserLocations PRIMARY KEY (UserId, LocationId),
    CONSTRAINT FK_UserLocations_Users 
        FOREIGN KEY (UserId) 
        REFERENCES dbo.Users(Id) ON DELETE NO ACTION,   -- ไม่ cascade ฝั่ง Users
    CONSTRAINT FK_UserLocations_Locations 
        FOREIGN KEY (LocationId) 
        REFERENCES dbo.Locations(Id) ON DELETE CASCADE  -- cascade ฝั่ง Locations
);



CREATE NONCLUSTERED INDEX IX_UserLocations_Location 
    ON dbo.UserLocations(LocationId);


-- Trigger ลบความสัมพันธ์เมื่อมีการลบ Users
CREATE OR ALTER TRIGGER dbo.trg_Users_Delete_UserLocations
ON dbo.Users
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;
    DELETE ul
    FROM dbo.UserLocations ul
    JOIN deleted d ON ul.UserId = d.Id;
END


--ProductStocks ตาม Location
CREATE TABLE dbo.ProductStocks (
    Id            int IDENTITY(1,1) NOT NULL,
    ProductId     int NOT NULL,
    LocationId    int NOT NULL,
    QtyOnHand     int NOT NULL CONSTRAINT DF_ProductStocks_QtyOnHand DEFAULT (0),
    QtyReserved   int NOT NULL CONSTRAINT DF_ProductStocks_QtyReserved DEFAULT (0),
    QtyDamaged    int NOT NULL CONSTRAINT DF_ProductStocks_QtyDamaged DEFAULT (0),
    QtyAvailable  AS (QtyOnHand - QtyReserved - QtyDamaged) PERSISTED,
    MinLevel      int NULL,
    MaxLevel      int NULL,
    ReorderPoint  int NULL,
    Cost          decimal(18,2) NULL,
    UpdatedAt     datetime2 NOT NULL CONSTRAINT DF_ProductStocks_UpdatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_ProductStocks PRIMARY KEY (Id),
    CONSTRAINT FK_ProductStocks_Products_ProductId  FOREIGN KEY (ProductId)  REFERENCES dbo.Products(Id)  ON DELETE CASCADE,
    CONSTRAINT FK_ProductStocks_Locations_LocationId FOREIGN KEY (LocationId) REFERENCES dbo.Locations(Id) ON DELETE CASCADE,
    CONSTRAINT CK_ProductStocks_NonNegative CHECK (QtyOnHand >= 0 AND QtyReserved >= 0 AND QtyDamaged >= 0)
);


CREATE UNIQUE NONCLUSTERED INDEX UX_ProductStocks_Product_Location
ON dbo.ProductStocks(ProductId, LocationId);


CREATE NONCLUSTERED INDEX IX_ProductStocks_Location
ON dbo.ProductStocks(LocationId, ProductId);


--เก็บประวัติ StockTransactions
CREATE TABLE dbo.StockTransactions (
    Id               bigint IDENTITY(1,1) NOT NULL,
    ProductId        int NOT NULL,
    FromLocationId   int NULL,  -- null = ไม่มีจุดต้นทาง
    ToLocationId     int NULL,  -- null = ไม่มีจุดปลายทาง
    QtyChange        int NOT NULL,   -- ค่าบวก/ลบ (กรณี single-leg)
    ReasonCode       nvarchar(50) COLLATE Thai_CI_AI NOT NULL, -- 'PURCHASE','SALE','TRANSFER','ADJUST','RETURN','DAMAGE'
    ReferenceType    nvarchar(50) COLLATE Thai_CI_AI NULL,     -- 'SO','PO','ADJ'
    ReferenceId      nvarchar(100) COLLATE Thai_CI_AI NULL,
    UnitCost         decimal(18,2) NULL,
    PerformedByUserId int NULL,
    CreatedAt        datetime2 NOT NULL CONSTRAINT DF_StockTransactions_CreatedAt DEFAULT (SYSUTCDATETIME()),
    Note             nvarchar(MAX) COLLATE Thai_CI_AI NULL,
    CONSTRAINT PK_StockTransactions PRIMARY KEY (Id),
    CONSTRAINT FK_ST_Products  FOREIGN KEY (ProductId)      REFERENCES dbo.Products(Id),
    CONSTRAINT FK_ST_FromLoc   FOREIGN KEY (FromLocationId) REFERENCES dbo.Locations(Id),
    CONSTRAINT FK_ST_ToLoc     FOREIGN KEY (ToLocationId)   REFERENCES dbo.Locations(Id)
);
CREATE NONCLUSTERED INDEX IX_StockTransactions_Product_Date
ON dbo.StockTransactions(ProductId, CreatedAt DESC);
CREATE NONCLUSTERED INDEX IX_StockTransactions_From_To_Date
ON dbo.StockTransactions(FromLocationId, ToLocationId, CreatedAt DESC);


--รวมสต็อกทุก Location กลับไปที่ Products.Quantity
CREATE OR ALTER TRIGGER dbo.trg_ProductStocksRecalcTotal
ON dbo.ProductStocks
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH changed(ProductId) AS (
        SELECT ProductId FROM inserted
        UNION
        SELECT ProductId FROM deleted
    )
    UPDATE p
       SET p.Quantity = s.TotalQty
    FROM dbo.Products p
    JOIN (
        SELECT ps.ProductId, SUM(ps.QtyOnHand) AS TotalQty
        FROM dbo.ProductStocks ps
        JOIN changed c ON c.ProductId = ps.ProductId
        GROUP BY ps.ProductId
    ) s ON s.ProductId = p.Id;
END
GO


--ปรับสต็อก/โอนย้ายแบบ
CREATE OR ALTER PROCEDURE dbo.sp_AdjustOrTransferStock
    @ProductId     int,
    @FromLocationId int = NULL,  -- NULL = รับเข้า
    @ToLocationId   int = NULL,  -- NULL = ตัดออก
    @Qty           int,          -- บวก = เพิ่ม, ลบ = ลด (กรณี single-leg); ถ้าโอน ให้เป็นจำนวนบวก
    @ReasonCode    nvarchar(50),
    @ReferenceType nvarchar(50) = NULL,
    @ReferenceId   nvarchar(100) = NULL,
    @PerformedByUserId int = NULL,
    @Note          nvarchar(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF (@FromLocationId IS NOT NULL AND @ToLocationId IS NOT NULL AND @Qty <= 0)
        THROW 50001, 'For transfer, @Qty must be positive.', 1;

    BEGIN TRAN;

    -- กรณีโอนย้าย: ตัดออกจาก From แล้วเพิ่มเข้า To
    IF (@FromLocationId IS NOT NULL AND @ToLocationId IS NOT NULL)
    BEGIN
        -- ensure row exists
        IF NOT EXISTS (SELECT 1 FROM dbo.ProductStocks WHERE ProductId=@ProductId AND LocationId=@FromLocationId)
            INSERT INTO dbo.ProductStocks(ProductId, LocationId, QtyOnHand) VALUES (@ProductId, @FromLocationId, 0);

        IF NOT EXISTS (SELECT 1 FROM dbo.ProductStocks WHERE ProductId=@ProductId AND LocationId=@ToLocationId)
            INSERT INTO dbo.ProductStocks(ProductId, LocationId, QtyOnHand) VALUES (@ProductId, @ToLocationId, 0);

        -- ตรวจพอไหม
        DECLARE @fromOnHand int;
        SELECT @fromOnHand = QtyOnHand FROM dbo.ProductStocks WHERE ProductId=@ProductId AND LocationId=@FromLocationId;
        IF (@fromOnHand < @Qty) THROW 50002, 'Insufficient stock at source location.', 1;

        -- ตัดออก
        UPDATE dbo.ProductStocks
           SET QtyOnHand = QtyOnHand - @Qty, UpdatedAt = SYSUTCDATETIME()
         WHERE ProductId=@ProductId AND LocationId=@FromLocationId;

        -- เพิ่มเข้า
        UPDATE dbo.ProductStocks
           SET QtyOnHand = QtyOnHand + @Qty, UpdatedAt = SYSUTCDATETIME()
         WHERE ProductId=@ProductId AND LocationId=@ToLocationId;

        INSERT INTO dbo.StockTransactions
            (ProductId, FromLocationId, ToLocationId, QtyChange, ReasonCode, ReferenceType, ReferenceId, PerformedByUserId, Note)
        VALUES
            (@ProductId, @FromLocationId, @ToLocationId, @Qty, 'TRANSFER', @ReferenceType, @ReferenceId, @PerformedByUserId, @Note);
    END
    ELSE
    BEGIN
        -- single-leg (รับเข้า/ตัดออก/ปรับยอด)
        DECLARE @targetLocId int = COALESCE(@ToLocationId, @FromLocationId);
        IF (@targetLocId IS NULL) THROW 50003, 'Either FromLocationId or ToLocationId must be provided.', 1;

        IF NOT EXISTS (SELECT 1 FROM dbo.ProductStocks WHERE ProductId=@ProductId AND LocationId=@targetLocId)
            INSERT INTO dbo.ProductStocks(ProductId, LocationId, QtyOnHand) VALUES (@ProductId, @targetLocId, 0);

        -- ถ้าเป็นตัดออกให้ @Qty เป็นค่าลบ
        UPDATE dbo.ProductStocks
           SET QtyOnHand = CASE WHEN @Qty >= 0 THEN QtyOnHand + @Qty ELSE
                                 CASE WHEN QtyOnHand + @Qty < 0 THEN 0 ELSE QtyOnHand + @Qty END END,
               UpdatedAt = SYSUTCDATETIME()
         WHERE ProductId=@ProductId AND LocationId=@targetLocId;

        INSERT INTO dbo.StockTransactions
            (ProductId, FromLocationId, ToLocationId, QtyChange, ReasonCode, ReferenceType, ReferenceId, PerformedByUserId, Note)
        VALUES
            (@ProductId, @FromLocationId, @ToLocationId, @Qty, @ReasonCode, @ReferenceType, @ReferenceId, @PerformedByUserId, @Note);
    END

    COMMIT;
END


--คำนวณยอดรวมครั้งแรก
UPDATE p
   SET p.Quantity = ISNULL(s.TotalQty, 0)
FROM dbo.Products p
LEFT JOIN (
    SELECT ProductId, SUM(QtyOnHand) AS TotalQty
    FROM dbo.ProductStocks
    GROUP BY ProductId
) s ON s.ProductId = p.Id;


--vwProductStockByLocation
CREATE OR ALTER VIEW dbo.vwProductStockByLocation
AS
SELECT 
    p.Id            AS ProductId,
    p.Name,
    p.Sku,
    ps.LocationId,
    l.Name          AS LocationName,
    ps.QtyOnHand,
    ps.QtyReserved,
    ps.QtyDamaged,
    ps.QtyAvailable,
    p.Price,
    p.Quantity      AS TotalQtyAllLocations
FROM dbo.ProductStocks ps
JOIN dbo.Products  p ON p.Id = ps.ProductId
JOIN dbo.Locations l ON l.Id = ps.LocationId;

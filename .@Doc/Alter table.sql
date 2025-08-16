ALTER TABLE miniapp.dbo.OrderHd
ADD Social NVARCHAR(MAX) NULL;

ALTER TABLE miniapp.dbo.OrderHd
ADD CONSTRAINT CK_OrderHd_Social_IsJson
CHECK (Social IS NULL OR ISJSON(Social) = 1);

ALTER TABLE Locations
ADD PlaceName NVARCHAR(255) NULL,
    Building NVARCHAR(255) NULL,
    Address NVARCHAR(255) NULL,
    District NVARCHAR(255) NULL,
    Province NVARCHAR(255) NULL,
    Postcode NVARCHAR(255) NULL,
    ContractPerson NVARCHAR(255) NULL,
    ContractPhone NVARCHAR(255) NULL;

DROP TABLE miniapp.dbo.OrderDt;

CREATE TABLE miniapp.dbo.OrderDt (
	Id int IDENTITY(1,1) NOT NULL,
	OrderHdId int NOT NULL,
	ProductId int NOT NULL,
	ProductName nvarchar(MAX) COLLATE Thai_CI_AI NOT NULL,
	UnitPrice decimal(18,2) NOT NULL,
	Quantity int NOT NULL,
	Discount decimal(18,2) NOT NULL,
	CONSTRAINT PK_OrderDt PRIMARY KEY (Id),
	CONSTRAINT FK_OrderDt_OrderHd_OrderHdId FOREIGN KEY (OrderHdId) REFERENCES miniapp.dbo.OrderHd(Id) ON DELETE CASCADE
);
 CREATE NONCLUSTERED INDEX IX_OrderDt_OrderHdId ON miniapp.dbo.OrderDt (  OrderHdId ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;

DROP TABLE miniapp.dbo.OrderHd;

CREATE TABLE miniapp.dbo.OrderHd (
	Id int IDENTITY(1,1) NOT NULL,
	OrderNo nvarchar(MAX) COLLATE Thai_CI_AI NOT NULL,
	OrderDate datetime2 NOT NULL,
	CustomerName nvarchar(MAX) COLLATE Thai_CI_AI NOT NULL,
	Gender nvarchar(MAX) COLLATE Thai_CI_AI NOT NULL,
	BirthDate datetime2 NULL,
	Occupation nvarchar(MAX) COLLATE Thai_CI_AI NULL,
	Nationality nvarchar(MAX) COLLATE Thai_CI_AI NULL,
	CustomerPhone nvarchar(MAX) COLLATE Thai_CI_AI NOT NULL,
	CustomerEmail nvarchar(MAX) COLLATE Thai_CI_AI NOT NULL,
	AddressLine nvarchar(MAX) COLLATE Thai_CI_AI NULL,
	SubDistrict nvarchar(MAX) COLLATE Thai_CI_AI NULL,
	District nvarchar(MAX) COLLATE Thai_CI_AI NULL,
	Province nvarchar(MAX) COLLATE Thai_CI_AI NULL,
	ZipCode nvarchar(MAX) COLLATE Thai_CI_AI NULL,
	MayIAsk bit NOT NULL,
	SlipImage nvarchar(MAX) COLLATE Thai_CI_AI NULL,
	PaymentMethod nvarchar(MAX) COLLATE Thai_CI_AI DEFAULT N'' NOT NULL,
	Social nvarchar(MAX) COLLATE Thai_CI_AI NULL,
	CONSTRAINT PK_OrderHd PRIMARY KEY (Id)
);
ALTER TABLE miniapp.dbo.OrderHd WITH NOCHECK ADD CONSTRAINT CK_OrderHd_Social_IsJson CHECK (([Social] IS NULL OR isjson([Social])=(1)));
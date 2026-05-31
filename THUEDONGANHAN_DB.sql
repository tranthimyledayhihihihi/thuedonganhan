-- ============================================================
-- HỆ THỐNG THUÊ & BÁN ĐỒ NGẮN HẠN - ĐẠI HỌC SƯ PHẠM KỸ THUẬT
-- Database Script Hoàn Chỉnh
-- Phiên bản: 4.0 | Ngày: 2026
-- ============================================================
-- THAY ĐỔI SO VỚI v3.0:
--   [1] StudentCode bỏ số 0 đầu (vd: 23115053122326 thay vì 023115053122326)
--   [2] Email sinh viên khớp StudentCode không có số 0 đầu
--   [3] Users.StudentId NOT NULL + UNIQUE (bắt buộc liên kết sinh viên)
--   [4] CHECK constraint email User phải đúng domain trường
--   [5] Trigger TR_Users_ValidateStudent: email User phải khớp email sinh viên
--   [6] Trigger TR_Rentals_RequireVerified: chặn giao dịch khi chưa verify
--   [7] Trigger TR_SaleOrders_RequireVerified: tương tự cho mua bán
--   [8] Bảng OTPVerifications: quản lý OTP xác thực email trường
-- ============================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'THUEDONGANHAN_DB')
BEGIN
    CREATE DATABASE THUEDONGANHAN_DB COLLATE Vietnamese_CI_AS;
    PRINT N'✓ Đã tạo database THUEDONGANHAN_DB';
END
GO

USE THUEDONGANHAN_DB;
GO

-- ============================================================
-- PHẦN 1: XÓA TOÀN BỘ FK CONSTRAINTS TRƯỚC, SAU ĐÓ MỚI DROP BẢNG
-- Dùng dynamic SQL để tự động tìm và xóa tất cả FK trong database
-- ============================================================

-- Bước 1: Xóa tất cả FOREIGN KEY constraints
DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql += N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id))
             + N'.' + QUOTENAME(OBJECT_NAME(parent_object_id))
             + N' DROP CONSTRAINT ' + QUOTENAME(name) + N';' + CHAR(10)
FROM sys.foreign_keys;
IF LEN(@sql) > 0 EXEC sp_executesql @sql;
GO

-- Bước 2: Drop bảng (không còn lo thứ tự FK)
IF OBJECT_ID('dbo.OTPVerifications',   'U') IS NOT NULL DROP TABLE dbo.OTPVerifications;
IF OBJECT_ID('dbo.SaleOrders',         'U') IS NOT NULL DROP TABLE dbo.SaleOrders;
IF OBJECT_ID('dbo.Disputes',           'U') IS NOT NULL DROP TABLE dbo.Disputes;
IF OBJECT_ID('dbo.Notifications',      'U') IS NOT NULL DROP TABLE dbo.Notifications;
IF OBJECT_ID('dbo.Messages',           'U') IS NOT NULL DROP TABLE dbo.Messages;
IF OBJECT_ID('dbo.Payments',           'U') IS NOT NULL DROP TABLE dbo.Payments;
IF OBJECT_ID('dbo.Transactions',     'U') IS NOT NULL DROP TABLE dbo.Transactions;
IF OBJECT_ID('dbo.Reviews',            'U') IS NOT NULL DROP TABLE dbo.Reviews;
IF OBJECT_ID('dbo.Rentals',            'U') IS NOT NULL DROP TABLE dbo.Rentals;
IF OBJECT_ID('dbo.ProductAvailability','U') IS NOT NULL DROP TABLE dbo.ProductAvailability;
IF OBJECT_ID('dbo.ProductImages',      'U') IS NOT NULL DROP TABLE dbo.ProductImages;
IF OBJECT_ID('dbo.Products',           'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID('dbo.RefreshTokens',      'U') IS NOT NULL DROP TABLE dbo.RefreshTokens;
IF OBJECT_ID('dbo.Users',              'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID('dbo.Students',           'U') IS NOT NULL DROP TABLE dbo.Students;
IF OBJECT_ID('dbo.Categories',         'U') IS NOT NULL DROP TABLE dbo.Categories;
GO
PRINT N'✓ Đã xóa các bảng cũ';
GO

-- ============================================================
-- PHẦN 2: TẠO BẢNG
-- ============================================================

-- ------------------------------------------------------------
-- 2.1 Categories
-- ------------------------------------------------------------
CREATE TABLE dbo.Categories (
    CategoryId   INT           NOT NULL IDENTITY(1,1),
    CategoryName NVARCHAR(100) NOT NULL,
    Description  NVARCHAR(500) NULL,
    IconUrl      NVARCHAR(500) NULL,
    SortOrder    INT           NOT NULL DEFAULT 0,
    IsActive     BIT           NOT NULL DEFAULT 1,
    CreatedAt    DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Categories      PRIMARY KEY (CategoryId),
    CONSTRAINT UQ_Categories_Name UNIQUE      (CategoryName)
);
GO

-- ------------------------------------------------------------
-- 2.2 Students
-- [v4] StudentCode KHÔNG có số 0 đầu (vd: 23115053122326)
--      Email = StudentCode@sv.ute.udn.vn (không có số 0 đầu)
-- ------------------------------------------------------------
CREATE TABLE dbo.Students (
    StudentId    INT           NOT NULL IDENTITY(1,1),
    -- MSSV không có số 0 dẫn đầu: 23115053122326
    StudentCode  NVARCHAR(20)  NOT NULL,
    FullName     NVARCHAR(100) NOT NULL,
    -- Email = StudentCode@sv.ute.udn.vn (không có số 0)
    Email        NVARCHAR(100) NOT NULL,
    PhoneNumber  NVARCHAR(20)  NULL,
    Major        NVARCHAR(100) NULL,
    Faculty      NVARCHAR(100) NULL,
    ClassName    NVARCHAR(50)  NULL,
    AcademicYear INT           NULL,
    Status       NVARCHAR(20)  NOT NULL DEFAULT 'Active',
    CreatedAt    DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt    DATETIME2     NULL,

    CONSTRAINT PK_Students             PRIMARY KEY (StudentId),
    CONSTRAINT UQ_Students_StudentCode UNIQUE      (StudentCode),
    CONSTRAINT UQ_Students_Email       UNIQUE      (Email),
    CONSTRAINT CK_Students_Email       CHECK       (Email LIKE '%@sv.ute.udn.vn'),
    CONSTRAINT CK_Students_Status      CHECK       (Status IN ('Active','Graduated','Suspended','LeaveOfAbsence'))
);
GO

-- ------------------------------------------------------------
-- 2.3 Users
-- [v4] StudentId NOT NULL + UNIQUE: bắt buộc mỗi User phải là sinh viên hợp lệ
--      và mỗi sinh viên chỉ có đúng 1 tài khoản
--      Email phải đúng domain trường
-- ------------------------------------------------------------
CREATE TABLE dbo.Users (
    UserId       INT           NOT NULL IDENTITY(1,1),
    FullName     NVARCHAR(100) NOT NULL,
    -- [v4] Email bắt buộc đúng domain @sv.ute.udn.vn hoặc @ute.udn.vn (admin)
    Email        NVARCHAR(100) NOT NULL,
    PhoneNumber  NVARCHAR(20)  NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    AvatarUrl    NVARCHAR(500) NULL,
    Address      NVARCHAR(255) NULL,
    Role         NVARCHAR(20)  NOT NULL DEFAULT 'User',  -- User | Admin
    UserType     NVARCHAR(20)  NOT NULL DEFAULT 'Both',  -- Renter | Owner | Both
    Balance      DECIMAL(18,2) NOT NULL DEFAULT 0,         -- [v4.1] Ti kho?n v
    IsActive     BIT           NOT NULL DEFAULT 1,
    -- [v4] IsVerified: phải xác thực OTP qua email trường mới được giao dịch
    IsVerified   BIT           NOT NULL DEFAULT 0,
    -- [v4] StudentId NOT NULL (trừ Admin) + UNIQUE
    StudentId    INT           NULL,
    LastLoginAt  DATETIME2     NULL,
    CreatedAt    DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt    DATETIME2     NULL,

    CONSTRAINT PK_Users          PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Email    UNIQUE      (Email),
    CONSTRAINT UQ_Users_Phone    UNIQUE      (PhoneNumber),
    -- [v4] 1 sinh viên chỉ được tạo 1 tài khoản
    CONSTRAINT UQ_Users_StudentId UNIQUE     (StudentId),
    CONSTRAINT CK_Users_Role     CHECK       (Role     IN ('User','Admin')),
    CONSTRAINT CK_Users_UserType CHECK       (UserType IN ('Renter','Owner','Both')),
    -- [v4] Email phải đúng domain trường (sinh viên hoặc admin)
    CONSTRAINT CK_Users_Email    CHECK       (
        Email LIKE '%@sv.ute.udn.vn' OR
        Email LIKE '%@ute.udn.vn'
    ),
    CONSTRAINT FK_Users_Students FOREIGN KEY (StudentId)
        REFERENCES dbo.Students (StudentId) ON DELETE SET NULL
);
GO

-- ------------------------------------------------------------
-- 2.4 OTPVerifications
-- [v4 MỚI] Lưu OTP xác thực email trường khi đăng ký
-- ------------------------------------------------------------
CREATE TABLE dbo.OTPVerifications (
    OTPId       INT           NOT NULL IDENTITY(1,1),
    UserId      INT           NOT NULL,
    OTPCode     NVARCHAR(10)  NOT NULL,   -- mã 6 chữ số
    OTPType     NVARCHAR(30)  NOT NULL DEFAULT 'EmailVerification',
    ExpiresAt   DATETIME2     NOT NULL,   -- thường 10 phút
    IsUsed      BIT           NOT NULL DEFAULT 0,
    AttemptCount INT          NOT NULL DEFAULT 0,  -- đếm số lần nhập sai
    CreatedAt   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_OTPVerifications      PRIMARY KEY (OTPId),
    CONSTRAINT CK_OTP_Type             CHECK (OTPType IN ('EmailVerification','PasswordReset')),
    CONSTRAINT FK_OTPVerifications_Users FOREIGN KEY (UserId)
        REFERENCES dbo.Users (UserId) ON DELETE CASCADE
);
GO

-- ------------------------------------------------------------
-- 2.5 RefreshTokens
-- ------------------------------------------------------------
CREATE TABLE dbo.RefreshTokens (
    TokenId   INT           NOT NULL IDENTITY(1,1),
    UserId    INT           NOT NULL,
    Token     NVARCHAR(500) NOT NULL,
    ExpiresAt DATETIME2     NOT NULL,
    IsRevoked BIT           NOT NULL DEFAULT 0,
    CreatedAt DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_RefreshTokens       PRIMARY KEY (TokenId),
    CONSTRAINT UQ_RefreshTokens_Token UNIQUE      (Token),
    CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId)
        REFERENCES dbo.Users (UserId) ON DELETE CASCADE
);
GO

-- ------------------------------------------------------------
-- 2.6 Products
-- ------------------------------------------------------------
CREATE TABLE dbo.Products (
    ProductId         INT           NOT NULL IDENTITY(1,1),
    ProductName       NVARCHAR(200) NOT NULL,
    Description       NVARCHAR(MAX) NOT NULL,
    Condition         NVARCHAR(20)  NOT NULL DEFAULT 'Good',
    -- Giá thuê
    PricePerHour      DECIMAL(18,2) NULL,
    PricePerDay       DECIMAL(18,2) NULL,
    PricePerWeek      DECIMAL(18,2) NULL,
    PricePerMonth     DECIMAL(18,2) NULL,
    -- Giá bán
    IsForSale         BIT           NOT NULL DEFAULT 0,
    SalePrice         DECIMAL(18,2) NULL,
    -- Loại sản phẩm
    ProductType       NVARCHAR(10)  NOT NULL DEFAULT 'Rent',  -- Rent | Sale | Both
    -- Đặt cọc & số lượng
    Deposit           DECIMAL(18,2) NOT NULL DEFAULT 0,
    Quantity          INT           NOT NULL DEFAULT 1,
    AvailableQuantity INT           NOT NULL DEFAULT 1,
    -- Hình ảnh & vị trí
    ImageUrl          NVARCHAR(500) NULL,
    Location          NVARCHAR(255) NULL,
    IsAvailable       BIT           NOT NULL DEFAULT 1,
    IsApproved        BIT           NOT NULL DEFAULT 0,
    -- Thống kê
    ViewCount         INT           NOT NULL DEFAULT 0,
    RentCount         INT           NOT NULL DEFAULT 0,
    SaleCount         INT           NOT NULL DEFAULT 0,
    AverageRating     DECIMAL(3,2)  NOT NULL DEFAULT 0,
    ReviewCount       INT           NOT NULL DEFAULT 0,
    CreatedAt         DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt         DATETIME2     NULL,
    -- FK
    OwnerId           INT           NOT NULL,
    CategoryId        INT           NOT NULL,

    CONSTRAINT PK_Products              PRIMARY KEY (ProductId),
    CONSTRAINT CK_Products_Condition    CHECK (Condition    IN ('New','LikeNew','Good','Fair','Poor')),
    CONSTRAINT CK_Products_ProductType  CHECK (ProductType  IN ('Rent','Sale','Both')),
    CONSTRAINT CK_Products_Quantity     CHECK (Quantity          >= 1),
    CONSTRAINT CK_Products_AvailQty     CHECK (AvailableQuantity >= 0),
    CONSTRAINT CK_Products_PricePerDay  CHECK (PricePerDay  IS NULL OR PricePerDay  > 0),
    CONSTRAINT CK_Products_PricePerHour CHECK (PricePerHour IS NULL OR PricePerHour > 0),
    CONSTRAINT CK_Products_SalePrice    CHECK (SalePrice    IS NULL OR SalePrice    > 0),
    CONSTRAINT FK_Products_Users        FOREIGN KEY (OwnerId)
        REFERENCES dbo.Users      (UserId)     ON DELETE NO ACTION,
    CONSTRAINT FK_Products_Categories   FOREIGN KEY (CategoryId)
        REFERENCES dbo.Categories (CategoryId) ON DELETE NO ACTION
);
GO

-- ------------------------------------------------------------
-- 2.7 ProductImages
-- ------------------------------------------------------------
CREATE TABLE dbo.ProductImages (
    ImageId   INT           NOT NULL IDENTITY(1,1),
    ProductId INT           NOT NULL,
    ImageUrl  NVARCHAR(500) NOT NULL,
    AltText   NVARCHAR(200) NULL,
    SortOrder INT           NOT NULL DEFAULT 0,
    IsPrimary BIT           NOT NULL DEFAULT 0,
    CreatedAt DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_ProductImages          PRIMARY KEY (ImageId),
    CONSTRAINT FK_ProductImages_Products FOREIGN KEY (ProductId)
        REFERENCES dbo.Products (ProductId) ON DELETE CASCADE
);
GO

-- ------------------------------------------------------------
-- 2.8 ProductAvailability
-- ------------------------------------------------------------
CREATE TABLE dbo.ProductAvailability (
    AvailabilityId  INT           NOT NULL IDENTITY(1,1),
    ProductId       INT           NOT NULL,
    UnavailableFrom DATETIME2     NOT NULL,
    UnavailableTo   DATETIME2     NOT NULL,
    Reason          NVARCHAR(200) NULL,
    CreatedAt       DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_ProductAvailability          PRIMARY KEY (AvailabilityId),
    CONSTRAINT CK_ProductAvailability_Dates    CHECK (UnavailableTo > UnavailableFrom),
    CONSTRAINT FK_ProductAvailability_Products FOREIGN KEY (ProductId)
        REFERENCES dbo.Products (ProductId) ON DELETE CASCADE
);
GO

-- ------------------------------------------------------------
-- 2.9 Rentals
-- ------------------------------------------------------------
CREATE TABLE dbo.Rentals (
    RentalId         INT           NOT NULL IDENTITY(1,1),
    ProductId        INT           NOT NULL,
    RenterId         INT           NOT NULL,
    Quantity         INT           NOT NULL DEFAULT 1,
    StartDate        DATETIME2     NOT NULL,
    EndDate          DATETIME2     NOT NULL,
    ActualReturnDate DATETIME2     NULL,
    RentalUnit       NVARCHAR(10)  NOT NULL DEFAULT 'Day',  -- Hour | Day | Week | Month
    TotalPrice       DECIMAL(18,2) NOT NULL,
    DepositAmount    DECIMAL(18,2) NOT NULL DEFAULT 0,
    DepositRefunded  BIT           NOT NULL DEFAULT 0,
    LateFee          DECIMAL(18,2) NOT NULL DEFAULT 0,
    DamageFee        DECIMAL(18,2) NOT NULL DEFAULT 0,
    Status           NVARCHAR(20)  NOT NULL DEFAULT 'Pending',
    CancelReason     NVARCHAR(500) NULL,
    Notes            NVARCHAR(500) NULL,
    OwnerNotes       NVARCHAR(500) NULL,
    CreatedAt        DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt        DATETIME2     NULL,

    CONSTRAINT PK_Rentals            PRIMARY KEY (RentalId),
    CONSTRAINT CK_Rentals_Dates      CHECK (EndDate > StartDate),
    CONSTRAINT CK_Rentals_Quantity   CHECK (Quantity >= 1),
    CONSTRAINT CK_Rentals_TotalPrice CHECK (TotalPrice >= 0),
    CONSTRAINT CK_Rentals_RentalUnit CHECK (RentalUnit IN ('Hour','Day','Week','Month')),
    CONSTRAINT CK_Rentals_Status     CHECK (Status IN ('Pending','Confirmed','InProgress','Active','Completed','Cancelled','Disputed')),
    CONSTRAINT FK_Rentals_Products   FOREIGN KEY (ProductId)
        REFERENCES dbo.Products (ProductId) ON DELETE NO ACTION,
    CONSTRAINT FK_Rentals_Renters    FOREIGN KEY (RenterId)
        REFERENCES dbo.Users    (UserId)    ON DELETE NO ACTION
);
GO

-- ------------------------------------------------------------
-- 2.10 SaleOrders
-- ------------------------------------------------------------
CREATE TABLE dbo.SaleOrders (
    SaleOrderId     INT           NOT NULL IDENTITY(1,1),
    ProductId       INT           NOT NULL,
    BuyerId         INT           NOT NULL,
    SalePrice       DECIMAL(18,2) NOT NULL,
    Status          NVARCHAR(20)  NOT NULL DEFAULT 'Pending',
    PreviousOwnerId INT           NULL,
    Notes           NVARCHAR(500) NULL,
    CreatedAt       DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2     NULL,

    CONSTRAINT PK_SaleOrders          PRIMARY KEY (SaleOrderId),
    CONSTRAINT CK_SaleOrders_Price    CHECK (SalePrice > 0),
    CONSTRAINT CK_SaleOrders_Status   CHECK (Status IN ('Pending','Confirmed','Completed','Cancelled')),
    CONSTRAINT FK_SaleOrders_Products FOREIGN KEY (ProductId)
        REFERENCES dbo.Products (ProductId) ON DELETE NO ACTION,
    CONSTRAINT FK_SaleOrders_Buyers   FOREIGN KEY (BuyerId)
        REFERENCES dbo.Users    (UserId)    ON DELETE NO ACTION
);
GO

-- ------------------------------------------------------------
-- 2.11 Payments
-- ------------------------------------------------------------
CREATE TABLE dbo.Payments (
    PaymentId     INT           NOT NULL IDENTITY(1,1),
    RentalId      INT           NULL,
    SaleOrderId   INT           NULL,
    PayerId       INT           NOT NULL,
    Amount        DECIMAL(18,2) NOT NULL,
    PaymentType   NVARCHAR(20)  NOT NULL DEFAULT 'RentalFee',
    PaymentMethod NVARCHAR(30)  NOT NULL,
    PaymentStatus NVARCHAR(20)  NOT NULL DEFAULT 'Pending',
    TransactionId NVARCHAR(100) NULL,
    PaymentDate   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    Notes         NVARCHAR(500) NULL,
    CreatedAt     DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt     DATETIME2     NULL,

    CONSTRAINT PK_Payments            PRIMARY KEY (PaymentId),
    CONSTRAINT CK_Payments_Amount     CHECK (Amount > 0),
    CONSTRAINT CK_Payments_OneRef     CHECK (
        (RentalId IS NOT NULL AND SaleOrderId IS NULL) OR
        (RentalId IS NULL     AND SaleOrderId IS NOT NULL)
    ),
    CONSTRAINT CK_Payments_Type       CHECK (PaymentType   IN ('RentalFee','Deposit','LateFee','DamageFee','DepositRefund','SalePayment')),
    CONSTRAINT CK_Payments_Method     CHECK (PaymentMethod IN ('Cash','BankTransfer','Momo','ZaloPay','VNPay')),
    CONSTRAINT CK_Payments_Status     CHECK (PaymentStatus IN ('Pending','Completed','Failed','Refunded','Cancelled')),
    CONSTRAINT FK_Payments_Rentals    FOREIGN KEY (RentalId)
        REFERENCES dbo.Rentals    (RentalId)    ON DELETE CASCADE,
    CONSTRAINT FK_Payments_SaleOrders FOREIGN KEY (SaleOrderId)
        REFERENCES dbo.SaleOrders (SaleOrderId) ON DELETE NO ACTION,
    CONSTRAINT FK_Payments_Payers     FOREIGN KEY (PayerId)
        REFERENCES dbo.Users      (UserId)      ON DELETE NO ACTION
);
GO

CREATE TABLE dbo.Transactions (
    TransactionId INT           NOT NULL IDENTITY(1,1),
    UserId        INT           NOT NULL,
    Type          VARCHAR(50)   NOT NULL, -- Deposit, Withdraw, Payment, Refund, Commission
    Amount        DECIMAL(18,2) NOT NULL,
    ReferenceId   INT           NULL,     -- e.g., RentalId
    Description   NVARCHAR(255) NULL,
    CreatedAt     DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Transactions        PRIMARY KEY (TransactionId),
    CONSTRAINT FK_Transactions_Users  FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId) ON DELETE CASCADE
);
GO

-- ------------------------------------------------------------
-- 2.12 Reviews
-- ------------------------------------------------------------
CREATE TABLE dbo.Reviews (
    ReviewId  INT            NOT NULL IDENTITY(1,1),
    ProductId INT            NOT NULL,
    UserId    INT            NOT NULL,
    RentalId  INT            NULL,
    Rating    TINYINT        NOT NULL,
    Comment   NVARCHAR(1000) NULL,
    CreatedAt DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Reviews             PRIMARY KEY (ReviewId),
    CONSTRAINT UQ_Reviews_UserProduct UNIQUE      (UserId, ProductId),
    CONSTRAINT CK_Reviews_Rating      CHECK (Rating BETWEEN 1 AND 5),
    CONSTRAINT FK_Reviews_Products    FOREIGN KEY (ProductId)
        REFERENCES dbo.Products (ProductId) ON DELETE CASCADE,
    CONSTRAINT FK_Reviews_Users       FOREIGN KEY (UserId)
        REFERENCES dbo.Users    (UserId)    ON DELETE NO ACTION,
    CONSTRAINT FK_Reviews_Rentals     FOREIGN KEY (RentalId)
        REFERENCES dbo.Rentals  (RentalId)  ON DELETE SET NULL
);
GO

-- ------------------------------------------------------------
-- 2.13 Disputes
-- ------------------------------------------------------------
CREATE TABLE dbo.Disputes (
    DisputeId  INT            NOT NULL IDENTITY(1,1),
    RentalId   INT            NOT NULL,
    ReporterId INT            NOT NULL,
    Reason     NVARCHAR(1000) NOT NULL,
    Status     NVARCHAR(20)   NOT NULL DEFAULT 'Open',
    Resolution NVARCHAR(1000) NULL,
    ResolvedBy INT            NULL,
    ResolvedAt DATETIME2      NULL,
    CreatedAt  DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt  DATETIME2      NULL,

    CONSTRAINT PK_Disputes           PRIMARY KEY (DisputeId),
    CONSTRAINT CK_Disputes_Status    CHECK (Status IN ('Open','UnderReview','Resolved','Closed')),
    CONSTRAINT FK_Disputes_Rentals   FOREIGN KEY (RentalId)
        REFERENCES dbo.Rentals (RentalId)  ON DELETE NO ACTION,
    CONSTRAINT FK_Disputes_Reporters FOREIGN KEY (ReporterId)
        REFERENCES dbo.Users   (UserId)    ON DELETE NO ACTION,
    CONSTRAINT FK_Disputes_Resolvers FOREIGN KEY (ResolvedBy)
        REFERENCES dbo.Users   (UserId)    ON DELETE NO ACTION
);
GO

-- ------------------------------------------------------------
-- 2.14 Messages
-- ------------------------------------------------------------
CREATE TABLE dbo.Messages (
    MessageId  INT            NOT NULL IDENTITY(1,1),
    SenderId   INT            NOT NULL,
    ReceiverId INT            NOT NULL,
    ProductId  INT            NULL,
    RentalId   INT            NULL,
    Content    NVARCHAR(2000) NOT NULL,
    IsRead     BIT            NOT NULL DEFAULT 0,
    CreatedAt  DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Messages           PRIMARY KEY (MessageId),
    CONSTRAINT CK_Messages_NoSelfMsg CHECK (SenderId <> ReceiverId),
    CONSTRAINT FK_Messages_Senders   FOREIGN KEY (SenderId)
        REFERENCES dbo.Users    (UserId)    ON DELETE NO ACTION,
    CONSTRAINT FK_Messages_Receivers FOREIGN KEY (ReceiverId)
        REFERENCES dbo.Users    (UserId)    ON DELETE NO ACTION,
    CONSTRAINT FK_Messages_Products  FOREIGN KEY (ProductId)
        REFERENCES dbo.Products (ProductId) ON DELETE SET NULL,
    CONSTRAINT FK_Messages_Rentals   FOREIGN KEY (RentalId)
        REFERENCES dbo.Rentals  (RentalId)  ON DELETE SET NULL
);
GO

-- ------------------------------------------------------------
-- 2.15 Notifications
-- ------------------------------------------------------------
CREATE TABLE dbo.Notifications (
    NotificationId INT           NOT NULL IDENTITY(1,1),
    UserId         INT           NOT NULL,
    Title          NVARCHAR(200) NOT NULL,
    Body           NVARCHAR(500) NOT NULL,
    Type           NVARCHAR(30)  NOT NULL DEFAULT 'System',
    ReferenceId    INT           NULL,
    ReferenceType  NVARCHAR(30)  NULL,
    IsRead         BIT           NOT NULL DEFAULT 0,
    CreatedAt      DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Notifications      PRIMARY KEY (NotificationId),
    CONSTRAINT CK_Notifications_Type CHECK (Type IN ('System','RentalRequest','RentalConfirmed','RentalCancelled','RentalCompleted','NewMessage','SaleRequest','SaleCompleted')),
    CONSTRAINT FK_Notifications_Users FOREIGN KEY (UserId)
        REFERENCES dbo.Users (UserId) ON DELETE CASCADE
);
GO

PRINT N'✓ Đã tạo tất cả các bảng';
GO

-- ============================================================
-- PHẦN 3: INDEX
-- ============================================================

-- Dùng macro helper: chỉ tạo index nếu chưa tồn tại
-- Users
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_StudentId'      AND object_id = OBJECT_ID('dbo.Users'))
    CREATE INDEX IX_Users_StudentId      ON dbo.Users    (StudentId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_Role'           AND object_id = OBJECT_ID('dbo.Users'))
    CREATE INDEX IX_Users_Role           ON dbo.Users    (Role);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_UserType'       AND object_id = OBJECT_ID('dbo.Users'))
    CREATE INDEX IX_Users_UserType       ON dbo.Users    (UserType);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_IsActive'       AND object_id = OBJECT_ID('dbo.Users'))
    CREATE INDEX IX_Users_IsActive       ON dbo.Users    (IsActive);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_IsVerified'     AND object_id = OBJECT_ID('dbo.Users'))
    CREATE INDEX IX_Users_IsVerified     ON dbo.Users    (IsVerified);

-- OTPVerifications
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OTP_UserId'           AND object_id = OBJECT_ID('dbo.OTPVerifications'))
    CREATE INDEX IX_OTP_UserId           ON dbo.OTPVerifications (UserId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OTP_ExpiresAt'        AND object_id = OBJECT_ID('dbo.OTPVerifications'))
    CREATE INDEX IX_OTP_ExpiresAt        ON dbo.OTPVerifications (ExpiresAt);

-- Products
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_OwnerId'     AND object_id = OBJECT_ID('dbo.Products'))
    CREATE INDEX IX_Products_OwnerId     ON dbo.Products (OwnerId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_CategoryId'  AND object_id = OBJECT_ID('dbo.Products'))
    CREATE INDEX IX_Products_CategoryId  ON dbo.Products (CategoryId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_IsAvailable' AND object_id = OBJECT_ID('dbo.Products'))
    CREATE INDEX IX_Products_IsAvailable ON dbo.Products (IsAvailable);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_ProductType' AND object_id = OBJECT_ID('dbo.Products'))
    CREATE INDEX IX_Products_ProductType ON dbo.Products (ProductType);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_CreatedAt'   AND object_id = OBJECT_ID('dbo.Products'))
    CREATE INDEX IX_Products_CreatedAt   ON dbo.Products (CreatedAt DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_Location'    AND object_id = OBJECT_ID('dbo.Products'))
    CREATE INDEX IX_Products_Location    ON dbo.Products (Location);

-- Rentals
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Rentals_ProductId'    AND object_id = OBJECT_ID('dbo.Rentals'))
    CREATE INDEX IX_Rentals_ProductId    ON dbo.Rentals  (ProductId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Rentals_RenterId'     AND object_id = OBJECT_ID('dbo.Rentals'))
    CREATE INDEX IX_Rentals_RenterId     ON dbo.Rentals  (RenterId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Rentals_Status'       AND object_id = OBJECT_ID('dbo.Rentals'))
    CREATE INDEX IX_Rentals_Status       ON dbo.Rentals  (Status);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Rentals_StartDate'    AND object_id = OBJECT_ID('dbo.Rentals'))
    CREATE INDEX IX_Rentals_StartDate    ON dbo.Rentals  (StartDate, EndDate);

-- SaleOrders
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SaleOrders_ProductId' AND object_id = OBJECT_ID('dbo.SaleOrders'))
    CREATE INDEX IX_SaleOrders_ProductId ON dbo.SaleOrders (ProductId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SaleOrders_BuyerId'   AND object_id = OBJECT_ID('dbo.SaleOrders'))
    CREATE INDEX IX_SaleOrders_BuyerId   ON dbo.SaleOrders (BuyerId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SaleOrders_Status'    AND object_id = OBJECT_ID('dbo.SaleOrders'))
    CREATE INDEX IX_SaleOrders_Status    ON dbo.SaleOrders (Status);

-- Payments
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Payments_RentalId'    AND object_id = OBJECT_ID('dbo.Payments'))
    CREATE INDEX IX_Payments_RentalId    ON dbo.Payments (RentalId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Payments_SaleOrderId' AND object_id = OBJECT_ID('dbo.Payments'))
    CREATE INDEX IX_Payments_SaleOrderId ON dbo.Payments (SaleOrderId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Payments_PayerId'     AND object_id = OBJECT_ID('dbo.Payments'))
    CREATE INDEX IX_Payments_PayerId     ON dbo.Payments (PayerId);

-- Reviews
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Reviews_ProductId'    AND object_id = OBJECT_ID('dbo.Reviews'))
    CREATE INDEX IX_Reviews_ProductId    ON dbo.Reviews  (ProductId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Reviews_UserId'       AND object_id = OBJECT_ID('dbo.Reviews'))
    CREATE INDEX IX_Reviews_UserId       ON dbo.Reviews  (UserId);

-- Messages
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Messages_SenderId'    AND object_id = OBJECT_ID('dbo.Messages'))
    CREATE INDEX IX_Messages_SenderId    ON dbo.Messages (SenderId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Messages_ReceiverId'  AND object_id = OBJECT_ID('dbo.Messages'))
    CREATE INDEX IX_Messages_ReceiverId  ON dbo.Messages (ReceiverId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Messages_CreatedAt'   AND object_id = OBJECT_ID('dbo.Messages'))
    CREATE INDEX IX_Messages_CreatedAt   ON dbo.Messages (CreatedAt DESC);

-- Notifications
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Notifications_UserId' AND object_id = OBJECT_ID('dbo.Notifications'))
    CREATE INDEX IX_Notifications_UserId ON dbo.Notifications (UserId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Notifications_IsRead' AND object_id = OBJECT_ID('dbo.Notifications'))
    CREATE INDEX IX_Notifications_IsRead ON dbo.Notifications (IsRead);

-- ProductAvailability
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProdAvail_ProductId'  AND object_id = OBJECT_ID('dbo.ProductAvailability'))
    CREATE INDEX IX_ProdAvail_ProductId  ON dbo.ProductAvailability (ProductId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProdAvail_Dates'      AND object_id = OBJECT_ID('dbo.ProductAvailability'))
    CREATE INDEX IX_ProdAvail_Dates      ON dbo.ProductAvailability (UnavailableFrom, UnavailableTo);

GO
PRINT N'✓ Đã tạo tất cả index';
GO

-- ============================================================
-- PHẦN 4: TRIGGER
-- ============================================================

-- ------------------------------------------------------------
-- TR 4.1 [v4 MỚI] Xác thực: email User phải khớp email sinh viên trong Students
-- ------------------------------------------------------------
CREATE OR ALTER TRIGGER TR_Users_ValidateStudent
ON dbo.Users
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Kiểm tra: nếu có StudentId thì email phải khớp với Students.Email
    IF EXISTS (
        SELECT 1
        FROM inserted i
        INNER JOIN dbo.Students s ON s.StudentId = i.StudentId
        WHERE i.Email <> s.Email
    )
    BEGIN
        RAISERROR(N'Email đăng ký phải khớp chính xác với email sinh viên trong hệ thống trường.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    -- Kiểm tra: User thường (Role = User) bắt buộc phải có StudentId
    IF EXISTS (
        SELECT 1
        FROM inserted i
        WHERE i.Role = 'User' AND i.StudentId IS NULL
    )
    BEGIN
        RAISERROR(N'Tài khoản sinh viên bắt buộc phải liên kết với mã sinh viên hợp lệ của trường.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

-- ------------------------------------------------------------
-- TR 4.2 [v4 MỚI] Chặn đặt thuê khi tài khoản chưa được xác thực email trường
-- ------------------------------------------------------------
CREATE OR ALTER TRIGGER TR_Rentals_RequireVerified
ON dbo.Rentals
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        INNER JOIN dbo.Users u ON u.UserId = i.RenterId
        WHERE u.IsVerified = 0
    )
    BEGIN
        RAISERROR(N'Tài khoản chưa xác thực email trường. Vui lòng xác thực OTP trước khi đặt thuê.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

-- ------------------------------------------------------------
-- TR 4.3 [v4 MỚI] Chặn đặt mua khi tài khoản chưa được xác thực email trường
-- ------------------------------------------------------------
CREATE OR ALTER TRIGGER TR_SaleOrders_RequireVerified
ON dbo.SaleOrders
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        INNER JOIN dbo.Users u ON u.UserId = i.BuyerId
        WHERE u.IsVerified = 0
    )
    BEGIN
        RAISERROR(N'Tài khoản chưa xác thực email trường. Vui lòng xác thực OTP trước khi đặt mua.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

-- ------------------------------------------------------------
-- TR 4.4 Ngăn chủ sản phẩm tự thuê
-- ------------------------------------------------------------
CREATE OR ALTER TRIGGER TR_Rentals_NoSelfRent
ON dbo.Rentals
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        INNER JOIN dbo.Products p ON p.ProductId = i.ProductId
        WHERE i.RenterId = p.OwnerId
    )
    BEGIN
        RAISERROR(N'Chủ sản phẩm không thể tự thuê sản phẩm của mình.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

-- ------------------------------------------------------------
-- TR 4.5 Ngăn chủ sản phẩm tự mua
-- ------------------------------------------------------------
CREATE OR ALTER TRIGGER TR_SaleOrders_NoSelfBuy
ON dbo.SaleOrders
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        INNER JOIN dbo.Products p ON p.ProductId = i.ProductId
        WHERE i.BuyerId = p.OwnerId
    )
    BEGIN
        RAISERROR(N'Chủ sản phẩm không thể tự mua sản phẩm của mình.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

-- ------------------------------------------------------------
-- TR 4.6 Tự cập nhật AverageRating & ReviewCount
-- ------------------------------------------------------------
CREATE OR ALTER TRIGGER TR_Reviews_UpdateRating
ON dbo.Reviews
AFTER INSERT, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    WITH affected AS (
        SELECT ProductId FROM inserted
        UNION
        SELECT ProductId FROM deleted
    )
    UPDATE p
    SET
        p.AverageRating = ISNULL((
            SELECT CAST(AVG(CAST(r.Rating AS FLOAT)) AS DECIMAL(3,2))
            FROM dbo.Reviews r WHERE r.ProductId = p.ProductId
        ), 0),
        p.ReviewCount = (
            SELECT COUNT(*) FROM dbo.Reviews r WHERE r.ProductId = p.ProductId
        ),
        p.UpdatedAt = SYSUTCDATETIME()
    FROM dbo.Products p
    INNER JOIN affected a ON a.ProductId = p.ProductId;
END;
GO

-- ------------------------------------------------------------
-- TR 4.7 Khi Rental Confirmed → trừ AvailableQuantity
--         Khi Rental Completed/Cancelled → hoàn lại
-- ------------------------------------------------------------
CREATE OR ALTER TRIGGER TR_Rentals_UpdateAvailableQty
ON dbo.Rentals
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Trừ khi chuyển sang Confirmed
    UPDATE p
    SET p.AvailableQuantity = p.AvailableQuantity - i.Quantity,
        p.UpdatedAt = SYSUTCDATETIME()
    FROM dbo.Products p
    INNER JOIN inserted i ON i.ProductId = p.ProductId
    INNER JOIN deleted  d ON d.RentalId  = i.RentalId
    WHERE d.Status <> 'Confirmed' AND i.Status = 'Confirmed';

    -- Hoàn lại khi Completed hoặc Cancelled
    UPDATE p
    SET p.AvailableQuantity = p.AvailableQuantity + i.Quantity,
        p.UpdatedAt = SYSUTCDATETIME()
    FROM dbo.Products p
    INNER JOIN inserted i ON i.ProductId = p.ProductId
    INNER JOIN deleted  d ON d.RentalId  = i.RentalId
    WHERE d.Status IN ('Confirmed', 'InProgress', 'Active')
      AND i.Status IN ('Completed', 'Cancelled');
END;
GO

-- ------------------------------------------------------------
-- TR 4.8 Khi Rental Completed → tăng RentCount
-- ------------------------------------------------------------
CREATE OR ALTER TRIGGER TR_Rentals_UpdateRentCount
ON dbo.Rentals
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE p
    SET p.RentCount = p.RentCount + 1,
        p.UpdatedAt = SYSUTCDATETIME()
    FROM dbo.Products p
    INNER JOIN inserted i ON i.ProductId = p.ProductId
    INNER JOIN deleted  d ON d.RentalId  = i.RentalId
    WHERE d.Status <> 'Completed' AND i.Status = 'Completed';
END;
GO

-- ------------------------------------------------------------
-- TR 4.9 Khi SaleOrder Completed → chuyển OwnerId sản phẩm sang BuyerId
-- ------------------------------------------------------------
CREATE OR ALTER TRIGGER TR_SaleOrders_TransferOwnership
ON dbo.SaleOrders
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Lưu PreviousOwnerId
    UPDATE so
    SET so.PreviousOwnerId = p.OwnerId
    FROM dbo.SaleOrders so
    INNER JOIN inserted   i ON i.SaleOrderId  = so.SaleOrderId
    INNER JOIN dbo.Products p ON p.ProductId  = i.ProductId
    INNER JOIN deleted    d ON d.SaleOrderId  = i.SaleOrderId
    WHERE d.Status <> 'Completed'
      AND i.Status  = 'Completed'
      AND so.PreviousOwnerId IS NULL;

    -- Chuyển quyền sở hữu
    UPDATE p
    SET p.OwnerId   = i.BuyerId,
        p.IsForSale = 0,
        p.SalePrice = NULL,
        p.SaleCount = p.SaleCount + 1,
        p.UpdatedAt = SYSUTCDATETIME()
    FROM dbo.Products p
    INNER JOIN inserted i ON i.ProductId   = p.ProductId
    INNER JOIN deleted  d ON d.SaleOrderId = i.SaleOrderId
    WHERE d.Status <> 'Completed' AND i.Status = 'Completed';
END;
GO

PRINT N'✓ Đã tạo tất cả trigger';
GO

-- ============================================================
-- PHẦN 5: VIEW
-- ============================================================

CREATE OR ALTER VIEW vw_ProductsDetail AS
SELECT
    p.ProductId,
    p.ProductName,
    p.Description,
    p.Condition,
    p.PricePerHour,
    p.PricePerDay,
    p.PricePerWeek,
    p.PricePerMonth,
    p.IsForSale,
    p.SalePrice,
    p.ProductType,
    p.Deposit,
    p.Quantity,
    p.AvailableQuantity,
    p.ImageUrl,
    p.Location,
    p.IsAvailable,
    p.AverageRating,
    p.ReviewCount,
    p.RentCount,
    p.SaleCount,
    p.CreatedAt,
    u.UserId      AS OwnerId,
    u.FullName    AS OwnerName,
    u.Email       AS OwnerEmail,
    u.PhoneNumber AS OwnerPhone,
    c.CategoryId,
    c.CategoryName
FROM dbo.Products  p
INNER JOIN dbo.Users      u ON u.UserId     = p.OwnerId
INNER JOIN dbo.Categories c ON c.CategoryId = p.CategoryId;
GO

CREATE OR ALTER VIEW vw_RentalsDetail AS
SELECT
    r.RentalId,
    r.StartDate,
    r.EndDate,
    r.ActualReturnDate,
    r.RentalUnit,
    r.Quantity,
    r.TotalPrice,
    r.DepositAmount,
    r.DepositRefunded,
    r.LateFee,
    r.DamageFee,
    r.Status,
    r.CreatedAt,
    u.UserId      AS RenterId,
    u.FullName    AS RenterName,
    u.Email       AS RenterEmail,
    u.PhoneNumber AS RenterPhone,
    p.ProductId,
    p.ProductName,
    p.ImageUrl    AS ProductImage,
    p.Location    AS ProductLocation,
    owner.UserId      AS OwnerId,
    owner.FullName    AS OwnerName,
    owner.Email       AS OwnerEmail,
    owner.PhoneNumber AS OwnerPhone
FROM dbo.Rentals   r
INNER JOIN dbo.Users    u     ON u.UserId     = r.RenterId
INNER JOIN dbo.Products p     ON p.ProductId  = r.ProductId
INNER JOIN dbo.Users    owner ON owner.UserId = p.OwnerId;
GO

-- [v4 MỚI] View thông tin đầy đủ User kèm sinh viên
CREATE OR ALTER VIEW vw_UsersDetail AS
SELECT
    u.UserId,
    u.FullName,
    u.Email,
    u.PhoneNumber,
    u.AvatarUrl,
    u.Address,
    u.Role,
    u.UserType,
    u.IsActive,
    u.IsVerified,
    u.LastLoginAt,
    u.CreatedAt,
    -- Thông tin sinh viên
    s.StudentId,
    s.StudentCode,
    s.Major,
    s.Faculty,
    s.ClassName,
    s.AcademicYear,
    s.Status AS StudentStatus
FROM dbo.Users    u
LEFT JOIN dbo.Students s ON s.StudentId = u.StudentId;
GO

PRINT N'✓ Đã tạo View';
GO

-- ============================================================
-- PHẦN 6: STORED PROCEDURES
-- ============================================================

-- SP: Kiểm tra lịch thuê trùng
CREATE OR ALTER PROCEDURE sp_CheckRentalConflict
    @ProductId       INT,
    @StartDate       DATETIME2,
    @EndDate         DATETIME2,
    @ExcludeRentalId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @RentalsConflict INT;
    DECLARE @BlockedConflict INT;

    SELECT @RentalsConflict = COUNT(*)
    FROM dbo.Rentals
    WHERE ProductId = @ProductId
      AND Status NOT IN ('Cancelled','Completed')
      AND (@ExcludeRentalId IS NULL OR RentalId <> @ExcludeRentalId)
      AND StartDate < @EndDate
      AND EndDate   > @StartDate;

    SELECT @BlockedConflict = COUNT(*)
    FROM dbo.ProductAvailability
    WHERE ProductId = @ProductId
      AND UnavailableFrom < @EndDate
      AND UnavailableTo > @StartDate;

    SELECT (@RentalsConflict + @BlockedConflict) AS ConflictCount;
END;
GO

-- SP: Lấy sản phẩm còn trống trong khoảng thời gian
CREATE OR ALTER PROCEDURE sp_GetAvailableProducts
    @StartDate  DATETIME2,
    @EndDate    DATETIME2,
    @CategoryId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.*
    FROM dbo.Products p
    WHERE p.IsAvailable = 1
      AND p.AvailableQuantity > 0
      AND (p.ProductType = 'Rent' OR p.ProductType = 'Both')
      AND (@CategoryId IS NULL OR p.CategoryId = @CategoryId)
      AND p.ProductId NOT IN (
          SELECT DISTINCT ProductId
          FROM dbo.Rentals
          WHERE Status NOT IN ('Cancelled','Completed')
            AND StartDate < @EndDate
            AND EndDate   > @StartDate
          UNION
          SELECT DISTINCT ProductId
          FROM dbo.ProductAvailability
          WHERE UnavailableFrom < @EndDate
            AND UnavailableTo > @StartDate
      );
END;
GO

-- [v4 MỚI] SP: Đăng ký tài khoản (kiểm tra MSSV hợp lệ trước)
-- Bước 1: Backend gọi SP này trước khi tạo User
CREATE OR ALTER PROCEDURE sp_ValidateStudentForRegister
    @StudentCode NVARCHAR(20),
    @Email       NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StudentId INT;
    DECLARE @StudentStatus NVARCHAR(20);
    DECLARE @AlreadyRegistered BIT = 0;

    -- Tìm sinh viên theo MSSV và email
    SELECT @StudentId     = StudentId,
           @StudentStatus = Status
    FROM dbo.Students
    WHERE StudentCode = @StudentCode
      AND Email       = @Email;

    IF @StudentId IS NULL
    BEGIN
        SELECT
            0         AS IsValid,
            N'MSSV hoặc email sinh viên không tồn tại trong hệ thống trường.' AS Message,
            NULL      AS StudentId;
        RETURN;
    END

    IF @StudentStatus <> 'Active'
    BEGIN
        SELECT
            0         AS IsValid,
            N'Tài khoản sinh viên không còn hoạt động (đã tốt nghiệp/đình chỉ).' AS Message,
            NULL      AS StudentId;
        RETURN;
    END

    -- Kiểm tra MSSV đã được đăng ký chưa
    IF EXISTS (SELECT 1 FROM dbo.Users WHERE StudentId = @StudentId)
    BEGIN
        SELECT
            0         AS IsValid,
            N'MSSV này đã được dùng để đăng ký tài khoản.' AS Message,
            NULL      AS StudentId;
        RETURN;
    END

    SELECT
        1           AS IsValid,
        N'Hợp lệ. Tiến hành gửi OTP xác thực.' AS Message,
        @StudentId  AS StudentId;
END;
GO

-- [v4 MỚI] SP: Xác thực OTP
CREATE OR ALTER PROCEDURE sp_VerifyOTP
    @UserId  INT,
    @OTPCode NVARCHAR(10),
    @OTPType NVARCHAR(30) = 'EmailVerification'
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @OTPId        INT;
    DECLARE @ExpiresAt    DATETIME2;
    DECLARE @IsUsed       BIT;
    DECLARE @AttemptCount INT;

    SELECT TOP 1
        @OTPId        = OTPId,
        @ExpiresAt    = ExpiresAt,
        @IsUsed       = IsUsed,
        @AttemptCount = AttemptCount
    FROM dbo.OTPVerifications
    WHERE UserId  = @UserId
      AND OTPType = @OTPType
      AND IsUsed  = 0
    ORDER BY CreatedAt DESC;

    IF @OTPId IS NULL
    BEGIN
        SELECT 0 AS IsValid, N'Không tìm thấy OTP hợp lệ.' AS Message;
        RETURN;
    END

    IF @AttemptCount >= 5
    BEGIN
        SELECT 0 AS IsValid, N'OTP đã bị khóa do nhập sai quá 5 lần.' AS Message;
        RETURN;
    END

    IF SYSUTCDATETIME() > @ExpiresAt
    BEGIN
        SELECT 0 AS IsValid, N'OTP đã hết hạn.' AS Message;
        RETURN;
    END

    -- Tăng attempt count
    UPDATE dbo.OTPVerifications
    SET AttemptCount = AttemptCount + 1
    WHERE OTPId = @OTPId;

    -- Lấy OTP code thực
    IF NOT EXISTS (
        SELECT 1 FROM dbo.OTPVerifications
        WHERE OTPId = @OTPId AND OTPCode = @OTPCode
    )
    BEGIN
        SELECT 0 AS IsValid, N'Mã OTP không đúng.' AS Message;
        RETURN;
    END

    -- Đánh dấu OTP đã dùng + kích hoạt User
    UPDATE dbo.OTPVerifications SET IsUsed = 1    WHERE OTPId  = @OTPId;
    UPDATE dbo.Users SET IsVerified = 1, UpdatedAt = SYSUTCDATETIME() WHERE UserId = @UserId;

    SELECT 1 AS IsValid, N'Xác thực thành công.' AS Message;
END;
GO

PRINT N'✓ Đã tạo Stored Procedures';
GO

-- ============================================================
-- PHẦN 7: DỮ LIỆU MẪU
-- ============================================================

-- ------------------------------------------------------------
-- 7.1 Categories
-- ------------------------------------------------------------
SET IDENTITY_INSERT dbo.Categories ON;
INSERT INTO dbo.Categories (CategoryId, CategoryName, Description, SortOrder, IsActive, CreatedAt)
VALUES
    (1,  N'Thiết bị điện tử',        N'Laptop, máy tính bảng, điện thoại, phụ kiện công nghệ', 1,  1, '2025-01-01'),
    (2,  N'Máy ảnh & Quay phim',     N'Máy ảnh, máy quay, lens, gimbal, tripod',                2,  1, '2025-01-01'),
    (3,  N'Âm thanh',                N'Tai nghe, loa bluetooth, micro, mixer, soundcard',        3,  1, '2025-01-01'),
    (4,  N'Thời trang & Lễ phục',    N'Áo dài, lễ phục tốt nghiệp, vest, đồ dự tiệc',          4,  1, '2025-01-01'),
    (5,  N'Sách & Giáo trình',       N'Sách giáo khoa, sách tham khảo, tài liệu học tập',       5,  1, '2025-01-01'),
    (6,  N'Thể thao & Gym',          N'Dụng cụ tập gym, xe đạp, vợt cầu lông, bóng đá',        6,  1, '2025-01-01'),
    (7,  N'Nhạc cụ',                 N'Guitar, piano, trống, violin, ukulele, nhạc cụ dân tộc', 7,  1, '2025-01-01'),
    (8,  N'Văn phòng & Trình chiếu', N'Máy chiếu, màn chiếu, bảng flipchart, máy in',          8,  1, '2025-01-01'),
    (9,  N'Đồ gia dụng',             N'Nồi cơm điện, lò vi sóng, bàn ủi, quạt điện',           9,  1, '2025-01-01'),
    (10, N'Xe cộ',                   N'Xe máy, xe đạp, xe đạp điện',                            10, 1, '2025-01-01');
SET IDENTITY_INSERT dbo.Categories OFF;
GO

-- ------------------------------------------------------------
-- 7.2 Students
-- [v4] StudentCode và Email KHÔNG có số 0 ở đầu
--      Format: 23115053122326 (bỏ chữ số 0 đầu tiên của 023115053122326)
-- ------------------------------------------------------------
SET IDENTITY_INSERT dbo.Students ON;
INSERT INTO dbo.Students (StudentId, StudentCode, FullName, Email, PhoneNumber, Major, Faculty, ClassName, AcademicYear, Status, CreatedAt)
VALUES
    (1,  '23115053122326', N'Nguyễn Văn An',   '23115053122326@sv.ute.udn.vn', '0901234561', N'Công nghệ Thông tin',  N'Khoa CNTT',   N'23T1', 2023, 'Active', '2025-01-01'),
    (2,  '23115053122327', N'Trần Thị Bình',   '23115053122327@sv.ute.udn.vn', '0901234562', N'Công nghệ Thông tin',  N'Khoa CNTT',   N'23T1', 2023, 'Active', '2025-01-01'),
    (3,  '23115053122328', N'Lê Văn Cường',    '23115053122328@sv.ute.udn.vn', '0901234563', N'Công nghệ Thông tin',  N'Khoa CNTT',   N'23T1', 2023, 'Active', '2025-01-01'),
    (4,  '23115053122329', N'Phạm Thị Dung',   '23115053122329@sv.ute.udn.vn', '0901234564', N'Kỹ thuật Điện tử',     N'Khoa Điện',   N'23T2', 2023, 'Active', '2025-01-01'),
    (5,  '23115053122330', N'Hoàng Văn Em',    '23115053122330@sv.ute.udn.vn', '0901234565', N'Kỹ thuật Điện tử',     N'Khoa Điện',   N'23T2', 2023, 'Active', '2025-01-01'),
    (6,  '22115053122201', N'Võ Thị Phương',   '22115053122201@sv.ute.udn.vn', '0901234566', N'Kỹ thuật Cơ khí',      N'Khoa Cơ khí', N'22T3', 2022, 'Active', '2025-01-01'),
    (7,  '22115053122202', N'Đặng Văn Giang',  '22115053122202@sv.ute.udn.vn', '0901234567', N'Kỹ thuật Cơ khí',      N'Khoa Cơ khí', N'22T3', 2022, 'Active', '2025-01-01'),
    (8,  '22115053122203', N'Bùi Thị Hoa',     '22115053122203@sv.ute.udn.vn', '0901234568', N'Quản trị Kinh doanh',  N'Khoa KT',     N'22T4', 2022, 'Active', '2025-01-01'),
    (9,  '21115053122101', N'Ngô Văn Khoa',    '21115053122101@sv.ute.udn.vn', '0901234569', N'Công nghệ Thông tin',  N'Khoa CNTT',   N'21T1', 2021, 'Active', '2025-01-01'),
    (10, '21115053122102', N'Phan Thị Lan',    '21115053122102@sv.ute.udn.vn', '0901234570', N'Kỹ thuật Phần mềm',    N'Khoa CNTT',   N'21T1', 2021, 'Active', '2025-01-01'),
    (11, '21115053122103', N'Trương Minh Mẫn', '21115053122103@sv.ute.udn.vn', '0901234571', N'Công nghệ Thông tin',  N'Khoa CNTT',   N'21T2', 2021, 'Active', '2025-01-01'),
    (12, '20115053122001', N'Đinh Thị Ngân',   '20115053122001@sv.ute.udn.vn', '0901234572', N'Kỹ thuật Điện',        N'Khoa Điện',   N'20T1', 2020, 'Active', '2025-01-01');
SET IDENTITY_INSERT dbo.Students OFF;
GO

-- ------------------------------------------------------------
-- 7.3 Users
-- [v4] Admin: StudentId NULL (Role = Admin được phép)
--      Email sinh viên khớp chính xác với Students.Email (không số 0 đầu)
-- Password: Admin@123    → BCrypt $2a$11$sU26lp/POAUyb.ezvdeR/.dXlTt2G3pXqWT7OL9i5S7.Du4zO.QIO
-- Password: Student@123  → BCrypt $2a$11$IZGpvObw6hU4f9dRe.FZTuGRfuVVVrxAstkErvkwLxCYpEONdqj7G
-- ------------------------------------------------------------
-- Tạm tắt trigger để seed dữ liệu (trigger yêu cầu IsVerified = 1 cho giao dịch)
DISABLE TRIGGER TR_Users_ValidateStudent ON dbo.Users;
GO

SET IDENTITY_INSERT dbo.Users ON;
INSERT INTO dbo.Users (UserId, FullName, Email, PhoneNumber, PasswordHash, Role, UserType, IsActive, IsVerified, StudentId, CreatedAt)
VALUES
    -- Admin: không có StudentId, email domain @ute.udn.vn
    (1, N'Admin UTE',
     'admin@ute.udn.vn', '0236123456',
     '$2a$11$sU26lp/POAUyb.ezvdeR/.dXlTt2G3pXqWT7OL9i5S7.Du4zO.QIO',
     'Admin', 'Both', 1, 1, NULL, '2025-01-01'),

    -- [v4] Email khớp chính xác Students.Email (không số 0 đầu)
    (2, N'Nguyễn Văn An',
     '23115053122326@sv.ute.udn.vn', '0901234561',
     '$2a$11$IZGpvObw6hU4f9dRe.FZTuGRfuVVVrxAstkErvkwLxCYpEONdqj7G',
     'User', 'Both', 1, 1, 1, '2025-01-01'),

    (3, N'Trần Thị Bình',
     '23115053122327@sv.ute.udn.vn', '0901234562',
     '$2a$11$IZGpvObw6hU4f9dRe.FZTuGRfuVVVrxAstkErvkwLxCYpEONdqj7G',
     'User', 'Both', 1, 1, 2, '2025-01-01'),

    (4, N'Lê Văn Cường',
     '23115053122328@sv.ute.udn.vn', '0901234563',
     '$2a$11$IZGpvObw6hU4f9dRe.FZTuGRfuVVVrxAstkErvkwLxCYpEONdqj7G',
     'User', 'Both', 1, 1, 3, '2025-01-01'),

    (5, N'Phạm Thị Dung',
     '23115053122329@sv.ute.udn.vn', '0901234564',
     '$2a$11$IZGpvObw6hU4f9dRe.FZTuGRfuVVVrxAstkErvkwLxCYpEONdqj7G',
     'User', 'Renter', 1, 1, 4, '2025-01-01'),

    (6, N'Hoàng Văn Em',
     '23115053122330@sv.ute.udn.vn', '0901234565',
     '$2a$11$IZGpvObw6hU4f9dRe.FZTuGRfuVVVrxAstkErvkwLxCYpEONdqj7G',
     'User', 'Owner', 1, 1, 5, '2025-01-01'),

    (7, N'Võ Thị Phương',
     '22115053122201@sv.ute.udn.vn', '0901234566',
     '$2a$11$IZGpvObw6hU4f9dRe.FZTuGRfuVVVrxAstkErvkwLxCYpEONdqj7G',
     'User', 'Both', 1, 1, 6, '2025-01-01');
SET IDENTITY_INSERT dbo.Users OFF;
GO

ENABLE TRIGGER TR_Users_ValidateStudent ON dbo.Users;
GO

-- ------------------------------------------------------------
-- 7.4 Products
-- ------------------------------------------------------------
SET IDENTITY_INSERT dbo.Products ON;
INSERT INTO dbo.Products
    (ProductId, ProductName, Description, Condition,
     PricePerHour, PricePerDay, PricePerWeek, PricePerMonth,
     IsForSale, SalePrice, ProductType,
     Deposit, Quantity, AvailableQuantity,
     ImageUrl, Location, IsAvailable,
     OwnerId, CategoryId, CreatedAt)
VALUES
    (1, N'MacBook Pro M3 14 inch',
     N'MacBook Pro 14 inch chip M3, RAM 16GB, SSD 512GB. Máy 99%, đầy đủ sạc và túi đựng.',
     'LikeNew', 18000, 120000, 700000, 2500000,
     0, NULL, 'Rent', 5000000, 1, 1,
     'https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=800',
     N'Ký túc xá A, UTE Đà Nẵng', 1, 2, 1, '2025-02-01'),

    (2, N'iPad Pro 11 inch M2 + Apple Pencil',
     N'iPad Pro 11 inch M2, 256GB WiFi, kèm Apple Pencil Gen 2 và Magic Keyboard.',
     'Good', 10000, 80000, 500000, 1800000,
     0, NULL, 'Rent', 3000000, 1, 1,
     'https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?w=800',
     N'Ký túc xá B, UTE Đà Nẵng', 1, 3, 1, '2025-02-05'),

    (3, N'Canon EOS R6 + Lens RF 24-105mm',
     N'Mirrorless Canon EOS R6 Full Frame, kèm lens RF 24-105mm f/4L IS USM, 2 pin, túi máy.',
     'Good', 25000, 200000, 1200000, 4000000,
     0, NULL, 'Rent', 10000000, 1, 1,
     'https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=800',
     N'Đà Nẵng', 1, 4, 2, '2025-02-10'),

    (4, N'Bộ sách Lập trình C/C++ (3 cuốn)',
     N'Gồm: Lập trình C cơ bản, C++ nâng cao, Cấu trúc dữ liệu & Giải thuật. Sách 85%.',
     'Good', NULL, 10000, 60000, 200000,
     1, 150000, 'Both', 100000, 2, 2,
     'https://images.unsplash.com/photo-1532012197267-da84d127e765?w=800',
     N'Thư viện UTE, Đà Nẵng', 1, 2, 5, '2025-02-12'),

    (5, N'Giáo trình Giải tích 1 & 2',
     N'Giải tích 1 và Giải tích 2 dành cho sinh viên kỹ thuật UTE. Sách 90%.',
     'Good', NULL, 5000, 30000, 100000,
     1, 60000, 'Both', 50000, 3, 3,
     'https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=800',
     N'Ký túc xá A, UTE', 1, 3, 5, '2025-02-15'),

    (6, N'Lễ phục tốt nghiệp UTE (Size M)',
     N'Bộ lễ phục tốt nghiệp chính thức của UTE, áo + mũ + cổ, Size M. Giặt sạch sau mỗi lần thuê.',
     'Good', NULL, 50000, NULL, NULL,
     0, NULL, 'Rent', 200000, 2, 2,
     'https://images.unsplash.com/photo-1541339907198-e08756dedf3f?w=800',
     N'Gần cổng chính UTE', 1, 6, 4, '2025-03-01'),

    (7, N'Vest nam xanh navy (Size L)',
     N'Vest nam công sở cao cấp, kèm cà vạt và sơ mi trắng. Phù hợp phỏng vấn, sự kiện.',
     'LikeNew', NULL, 80000, 480000, NULL,
     1, 800000, 'Both', 500000, 1, 1,
     'https://images.unsplash.com/photo-1507679799987-c73779587ccf?w=800',
     N'Đà Nẵng', 1, 7, 4, '2025-03-05'),

    (8, N'Guitar Acoustic Yamaha F310',
     N'Guitar acoustic Yamaha F310, âm thanh ấm, tình trạng tốt. Kèm bao đàn và capo.',
     'Good', NULL, 40000, 250000, 800000,
     0, NULL, 'Rent', 1000000, 1, 1,
     'https://images.unsplash.com/photo-1510915361894-db8b60106cb1?w=800',
     N'Ký túc xá B, UTE', 1, 4, 7, '2025-03-08'),

    (9, N'Xe đạp Giant ATX 810 27.5"',
     N'Xe đạp địa hình Giant ATX 810, 27.5 inch, phanh đĩa thủy lực, 21 tốc độ.',
     'Good', 5000, 30000, 180000, 600000,
     0, NULL, 'Rent', 800000, 1, 1,
     'https://images.unsplash.com/photo-1485965120184-e220f721d03e?w=800',
     N'Đà Nẵng', 1, 6, 6, '2025-03-10'),

    (10, N'Máy chiếu Epson EB-X05',
     N'Epson EB-X05, 3300 lumens, độ phân giải XGA, kèm remote và dây HDMI.',
     'Good', 20000, 150000, 900000, 3000000,
     1, 3500000, 'Both', 2000000, 1, 1,
     'https://images.unsplash.com/photo-1593784991095-a205069470b6?w=800',
     N'Gần phòng A101, UTE', 1, 6, 8, '2025-03-12'),

    (11, N'Sony WH-1000XM4 Chống ồn',
     N'Tai nghe chống ồn Sony WH-1000XM4, pin 30 giờ, Bluetooth 5.0. Hộp và đầy đủ phụ kiện.',
     'LikeNew', 5000, 30000, 180000, 600000,
     0, NULL, 'Rent', 1500000, 1, 1,
     'https://images.unsplash.com/photo-1546435770-a3e426bf472b?w=800',
     N'Ký túc xá C, UTE', 1, 7, 3, '2025-03-15'),

    (12, N'Xe máy Honda Wave RSX 110cc',
     N'Honda Wave RSX 2022, màu đỏ đen, máy êm, xe sạch đẹp. Có gương chiếu hậu, khóa chống trộm.',
     'Good', NULL, 80000, 500000, 1800000,
     0, NULL, 'Rent', 2000000, 1, 1,
     'https://images.unsplash.com/photo-1558981806-ec527fa84c39?w=800',
     N'Đà Nẵng', 1, 6, 10, '2025-03-20'),

    (15, N'máy ảnh', N'còn mới', 'Good', 20000.00, 49000.00, 300000.00, 990000.00, 0, NULL, 'Rent', 500000.00, 1, 1, 'https://images.unsplash.com/photo-1516035069371-29a1b244cc32?w=800', N'ký túc xá', 1, 6, 1, SYSUTCDATETIME()),
    (16, N'áo dài', N'còn mới', 'Good', 10000.00, 29000.00, 60000.00, 100000.00, 0, NULL, 'Rent', 100000.00, 1, 1, 'https://images.unsplash.com/photo-1600566753376-12c8ab7fb75b?w=800', N'ký túc xá', 1, 6, 4, SYSUTCDATETIME());
SET IDENTITY_INSERT dbo.Products OFF;
UPDATE dbo.Products SET IsApproved = 1;
GO

-- ------------------------------------------------------------
-- 7.5 Rentals mẫu (User IsVerified = 1 nên trigger không chặn)
-- ------------------------------------------------------------
SET IDENTITY_INSERT dbo.Rentals ON;
INSERT INTO dbo.Rentals
    (RentalId, ProductId, RenterId, Quantity, StartDate, EndDate, ActualReturnDate,
     RentalUnit, TotalPrice, DepositAmount, DepositRefunded, Status, CreatedAt)
VALUES
    (1, 1, 5, 1, '2025-04-01', '2025-04-03', '2025-04-03', 'Day', 240000, 5000000, 1, 'Completed', '2025-03-28'),
    (2, 8, 5, 1, '2025-04-05', '2025-04-07', '2025-04-07', 'Day',  80000, 1000000, 1, 'Completed', '2025-04-03'),
    (3, 9, 3, 1, '2025-04-10', '2025-04-12', '2025-04-12', 'Day',  60000,  800000, 1, 'Completed', '2025-04-08'),
    (4, 4, 5, 1, '2025-05-01', '2025-05-03', NULL,         'Day',  20000,  100000, 0, 'Confirmed', '2025-04-28'),
    (5, 6, 3, 1, '2025-05-10', '2025-05-11', NULL,         'Day',  50000,  200000, 0, 'Pending',   '2025-05-08');
SET IDENTITY_INSERT dbo.Rentals OFF;
GO

-- ------------------------------------------------------------
-- 7.6 Reviews mẫu
-- ------------------------------------------------------------
SET IDENTITY_INSERT dbo.Reviews ON;
INSERT INTO dbo.Reviews (ReviewId, ProductId, UserId, RentalId, Rating, Comment, CreatedAt)
VALUES
    (1, 1, 5, 1, 5, N'Máy rất nhanh, pin tốt, giao nhận thuận tiện. Sẽ thuê lại!',        '2025-04-04'),
    (2, 8, 5, 2, 4, N'Đàn âm thanh ổn, bao đàn sạch. Trừ 1 sao vì dây đàn hơi cũ.',      '2025-04-08'),
    (3, 9, 3, 3, 5, N'Xe đạp tốt, đi êm. Rất phù hợp để đạp quanh UTE và ven sông Hàn.', '2025-04-13');
SET IDENTITY_INSERT dbo.Reviews OFF;
GO

UPDATE dbo.Products SET AverageRating = 5.00, ReviewCount = 1 WHERE ProductId = 1;
UPDATE dbo.Products SET AverageRating = 4.00, ReviewCount = 1 WHERE ProductId = 8;
UPDATE dbo.Products SET AverageRating = 5.00, ReviewCount = 1 WHERE ProductId = 9;
GO

-- ------------------------------------------------------------
-- 7.7 Payments mẫu
-- ------------------------------------------------------------
SET IDENTITY_INSERT dbo.Payments ON;
INSERT INTO dbo.Payments (PaymentId, RentalId, SaleOrderId, PayerId, Amount, PaymentType, PaymentMethod, PaymentStatus, CreatedAt)
VALUES
    (1, 1, NULL, 5, 240000,  'RentalFee', 'Cash',          'Completed', '2025-04-01'),
    (2, 1, NULL, 5, 5000000, 'Deposit',   'BankTransfer',   'Completed', '2025-04-01'),
    (3, 2, NULL, 5, 80000,   'RentalFee', 'Momo',           'Completed', '2025-04-05'),
    (4, 3, NULL, 3, 60000,   'RentalFee', 'Cash',           'Completed', '2025-04-10'),
    (5, 4, NULL, 5, 20000,   'RentalFee', 'ZaloPay',        'Pending',   '2025-04-28');
SET IDENTITY_INSERT dbo.Payments OFF;
GO

-- ------------------------------------------------------------
-- 7.8 Notifications mẫu
-- ------------------------------------------------------------
SET IDENTITY_INSERT dbo.Notifications ON;
INSERT INTO dbo.Notifications (NotificationId, UserId, Title, Body, Type, ReferenceId, ReferenceType, IsRead, CreatedAt)
VALUES
    (1, 2, N'Đơn thuê mới',           N'Phạm Thị Dung vừa đặt thuê MacBook Pro của bạn.',       'RentalRequest',   1, 'Rental', 1, '2025-03-28'),
    (2, 5, N'Đơn thuê được xác nhận', N'Đơn thuê MacBook Pro đã được chủ xác nhận.',             'RentalConfirmed', 1, 'Rental', 1, '2025-03-29'),
    (3, 2, N'Đơn thuê hoàn thành',    N'Đơn thuê MacBook Pro đã hoàn thành.',                    'RentalCompleted', 1, 'Rental', 0, '2025-04-03'),
    (4, 3, N'Đơn thuê mới',           N'Phạm Thị Dung muốn thuê Sách Lập trình C/C++ của bạn.', 'RentalRequest',   4, 'Rental', 0, '2025-04-28');
SET IDENTITY_INSERT dbo.Notifications OFF;
GO

PRINT N'✓ Đã insert tất cả dữ liệu mẫu';
GO

-- ============================================================
-- PHẦN 8: KIỂM TRA KẾT QUẢ
-- ============================================================
PRINT N'';
PRINT N'======== THỐNG KÊ DATABASE ========';

SELECT TableName = 'Categories',       RecordCount = COUNT(*) FROM dbo.Categories       UNION ALL
SELECT 'Students',                                               COUNT(*) FROM dbo.Students         UNION ALL
SELECT 'Users',                                                  COUNT(*) FROM dbo.Users             UNION ALL
SELECT 'Products',                                               COUNT(*) FROM dbo.Products          UNION ALL
SELECT 'Rentals',                                                COUNT(*) FROM dbo.Rentals           UNION ALL
SELECT 'SaleOrders',                                             COUNT(*) FROM dbo.SaleOrders        UNION ALL
SELECT 'Payments',                                               COUNT(*) FROM dbo.Payments          UNION ALL
SELECT 'Reviews',                                                COUNT(*) FROM dbo.Reviews           UNION ALL
SELECT 'Notifications',                                          COUNT(*) FROM dbo.Notifications     UNION ALL
SELECT 'OTPVerifications',                                       COUNT(*) FROM dbo.OTPVerifications;

PRINT N'';
PRINT N'======== TÀI KHOẢN TEST ========';
PRINT N'Admin    : admin@ute.udn.vn                | Mật khẩu: Admin@123';
PRINT N'SV Both  : 23115053122326@sv.ute.udn.vn    | Mật khẩu: Student@123 (Nguyễn Văn An)';
PRINT N'SV Both  : 23115053122327@sv.ute.udn.vn    | Mật khẩu: Student@123 (Trần Thị Bình)';
PRINT N'SV Renter: 23115053122329@sv.ute.udn.vn    | Mật khẩu: Student@123 (Phạm Thị Dung)';
PRINT N'SV Owner : 23115053122330@sv.ute.udn.vn    | Mật khẩu: Student@123 (Hoàng Văn Em)';
PRINT N'';
PRINT N'======== LUỒNG ĐĂNG KÝ (v4) ========';
PRINT N'1. Người dùng nhập MSSV (vd: 23115053122326) + email trường';
PRINT N'2. Gọi sp_ValidateStudentForRegister để kiểm tra MSSV hợp lệ';
PRINT N'3. Tạo User với IsVerified = 0';
PRINT N'4. Tạo OTP 6 số, lưu vào OTPVerifications, gửi về email trường';
PRINT N'5. Người dùng nhập OTP → gọi sp_VerifyOTP → IsVerified = 1';
PRINT N'6. Tài khoản được phép thuê/mua/bán';
PRINT N'';
PRINT N'======== ĐÃ HOÀN TẤT THIẾT LẬP DATABASE v4.0 ========';
GO
USE [THUEDONGANHAN_DB]
GO






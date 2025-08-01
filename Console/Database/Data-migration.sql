
-- Create FinanceSystemDb
-- GO
-- USE FinanceSystemDb

IF OBJECT_ID('dbo.Transactions', 'U') IS NOT NULL
    DROP TABLE dbo.Transactions;
IF OBJECT_ID('dbo.Accounts', 'U') IS NOT NULL
    DROP TABLE dbo.Accounts;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL
    DROP TABLE dbo.Users;
GO

 
CREATE TABLE dbo.Users (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(100) NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

 
CREATE TABLE dbo.Accounts (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    AccountNumber NVARCHAR(50) NOT NULL,
    Balance DECIMAL(18, 2) NOT NULL DEFAULT 0.00,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UserId UNIQUEIDENTIFIER NOT NULL,

    
    CONSTRAINT FK_Accounts_Users FOREIGN KEY (UserId)
        REFERENCES dbo.Users(Id)
        ON DELETE CASCADE,  
    CONSTRAINT UQ_Accounts_AccountNumber UNIQUE (AccountNumber)  
);
GO

 
CREATE NONCLUSTERED INDEX IX_Accounts_UserId ON dbo.Accounts(UserId);
GO


CREATE TABLE dbo.Transactions (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    Amount DECIMAL(18, 2) NOT NULL,
    Type INT NOT NULL, -- 0 for Deposit, 1 for Withdrawal
    Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    Description NVARCHAR(200) NULL,
    AccountId UNIQUEIDENTIFIER NOT NULL,

   
    CONSTRAINT FK_Transactions_Accounts FOREIGN KEY (AccountId)
        REFERENCES dbo.Accounts(Id)
        ON DELETE CASCADE, 

    CONSTRAINT CHK_Transactions_Amount CHECK (Amount > 0) -- The transaction amount should always be positive.
);
GO


CREATE NONCLUSTERED INDEX IX_Transactions_AccountId ON dbo.Transactions(AccountId);
GO

PRINT 'Database schema created successfully.';
GO
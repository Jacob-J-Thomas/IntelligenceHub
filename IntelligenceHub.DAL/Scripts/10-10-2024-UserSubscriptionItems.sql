CREATE TABLE UserSubscriptionItems (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(255) NOT NULL,
    UsageType NVARCHAR(50) NOT NULL,
    SubscriptionItemId NVARCHAR(255) NOT NULL
);

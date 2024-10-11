CREATE TABLE MessageHistory (
    Id INT PRIMARY KEY,
    ConversationId UNIQUEIDENTIFIER,
    [Role] NVARCHAR(255) NOT NULL,
    Base64Image NVARCHAR(MAX),
    [TimeStamp] DATETIME NOT NULL,
    Content NVARCHAR(MAX) NOT NULL
);

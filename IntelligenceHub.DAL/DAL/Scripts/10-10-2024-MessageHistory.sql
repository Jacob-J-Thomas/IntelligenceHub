﻿CREATE TABLE MessageHistory (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ConversationId UNIQUEIDENTIFIER,
    [Role] NVARCHAR(255) NOT NULL,
    Base64Image NVARCHAR(MAX),
    [TimeStamp] DATETIME NOT NULL,
    Content NVARCHAR(MAX) NOT NULL
);

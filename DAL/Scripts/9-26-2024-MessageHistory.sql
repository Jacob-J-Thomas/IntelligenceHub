﻿CREATE TABLE MessageHistory (
    Id INT PRIMARY KEY IDENTITY(1,1), 
    ConversationId UNIQUEIDENTIFIER,
    [Role] NVARCHAR(32) NOT NULL,
    [TimeStamp] DATETIME NOT NULL,
    Content NVARCHAR(MAX) NULL,
    ToolsCalled NVARCHAR(512) NULL
);

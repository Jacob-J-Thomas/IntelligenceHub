﻿CREATE TABLE ProfileTools (
    ProfileID INT NOT NULL,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    ToolID INT NOT NULL,
    PRIMARY KEY (ProfileID, ToolID)
);
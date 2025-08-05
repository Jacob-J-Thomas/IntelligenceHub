CREATE TABLE ProfileToolsManaged (
    ProfileID INT NOT NULL,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    ToolID INT NOT NULL,
    PRIMARY KEY (ProfileID, ToolID)
);
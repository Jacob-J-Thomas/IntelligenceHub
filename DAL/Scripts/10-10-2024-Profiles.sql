CREATE TABLE Profiles (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    [Name] NVARCHAR(255) UNIQUE NOT NULL,
    Model NVARCHAR(255),
    FrequencyPenalty FLOAT,
    PresencePenalty FLOAT,
    Temperature FLOAT,
    TopP FLOAT,
    TopLogprobs INT,
    MaxTokens INT,
    MaxMessageHistory INT,
    ResponseFormat NVARCHAR(255),
    [User] NVARCHAR(255),
    SystemMessage NVARCHAR(255),
    [Stop] NVARCHAR(255),
    ReferenceProfiles NVARCHAR(255),
    ReferenceDescription NVARCHAR(255),
    ReturnRecursion BIT
);

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Users] (
    [UserId] nvarchar(50) NOT NULL,
    [Username] nvarchar(100) NOT NULL,
    [Email] nvarchar(255) NOT NULL,
    [PasswordHash] nvarchar(255) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [LastLogin] datetime2 NULL,
    [IsActive] bit NOT NULL,
    [FirstName] nvarchar(100) NULL,
    [LastName] nvarchar(100) NULL,
    [PhoneNumber] nvarchar(20) NULL,
    [LastPasswordChange] datetime2 NULL,
    [EmailVerified] bit NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([UserId])
);
GO

CREATE TABLE [Devices] (
    [DeviceId] nvarchar(50) NOT NULL,
    [DeviceName] nvarchar(200) NOT NULL,
    [OwnerId] nvarchar(50) NOT NULL,
    [CurrentIP] nvarchar(45) NOT NULL,
    [Port] int NOT NULL,
    [LastSeen] datetime2 NOT NULL,
    [IsOnline] bit NOT NULL,
    [ComputerName] nvarchar(100) NOT NULL,
    [UserName] nvarchar(100) NOT NULL,
    [OperatingSystem] nvarchar(100) NOT NULL,
    [ProcessorArchitecture] nvarchar(50) NOT NULL,
    [ScreenWidth] int NOT NULL,
    [ScreenHeight] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [LastConnectionAt] datetime2 NULL,
    [Description] nvarchar(500) NULL,
    [SupportsH264] bit NOT NULL,
    [MaxFrameRate] int NOT NULL,
    [SupportsAudio] bit NOT NULL,
    [SupportsFileTransfer] bit NOT NULL,
    [RequireApproval] bit NOT NULL,
    [TrustedViewers] nvarchar(1000) NULL,
    CONSTRAINT [PK_Devices] PRIMARY KEY ([DeviceId]),
    CONSTRAINT [FK_Devices_Users_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
);
GO

CREATE TABLE [ConnectionPermissions] (
    [Id] int NOT NULL IDENTITY,
    [GranterId] nvarchar(50) NOT NULL,
    [GranteeId] nvarchar(50) NOT NULL,
    [DeviceId] nvarchar(50) NOT NULL,
    [PermissionType] nvarchar(20) NOT NULL,
    [IsActive] bit NOT NULL,
    [GrantedAt] datetime2 NOT NULL,
    [ExpiresAt] datetime2 NULL,
    [RevokedAt] datetime2 NULL,
    [Reason] nvarchar(500) NULL,
    [AllowedStartTime] time NULL,
    [AllowedEndTime] time NULL,
    [AllowedDays] nvarchar(50) NULL,
    [MaxSessionDuration] int NULL,
    [MaxConcurrentSessions] int NULL,
    [RequireApproval] bit NOT NULL,
    [AllowFileTransfer] bit NOT NULL,
    [AllowAudioStreaming] bit NOT NULL,
    [AllowInputControl] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [CreatedBy] nvarchar(50) NULL,
    [UpdatedBy] nvarchar(50) NULL,
    CONSTRAINT [PK_ConnectionPermissions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ConnectionPermissions_Devices_DeviceId] FOREIGN KEY ([DeviceId]) REFERENCES [Devices] ([DeviceId]) ON DELETE CASCADE,
    CONSTRAINT [FK_ConnectionPermissions_Users_GranteeId] FOREIGN KEY ([GranteeId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE,
    CONSTRAINT [FK_ConnectionPermissions_Users_GranterId] FOREIGN KEY ([GranterId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [SessionLogs] (
    [SessionId] nvarchar(50) NOT NULL,
    [ViewerId] nvarchar(50) NOT NULL,
    [AgentId] nvarchar(50) NOT NULL,
    [DeviceId] nvarchar(50) NOT NULL,
    [StartTime] datetime2 NOT NULL,
    [EndTime] datetime2 NULL,
    [Status] nvarchar(20) NOT NULL,
    [EndReason] nvarchar(500) NULL,
    [ViewerIP] nvarchar(45) NOT NULL,
    [AgentIP] nvarchar(45) NOT NULL,
    [ViewerLocation] nvarchar(100) NULL,
    [Duration] time NULL,
    [BytesTransferred] bigint NULL,
    [AverageFrameRate] int NULL,
    [AverageLatency] int NULL,
    [PacketsLost] int NULL,
    [VideoCodec] nvarchar(20) NULL,
    [CompressionLevel] nvarchar(20) NULL,
    [RequiredApproval] bit NOT NULL,
    [ApprovalTime] datetime2 NULL,
    [ApprovalReason] nvarchar(500) NULL,
    [Metadata] nvarchar(2000) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_SessionLogs] PRIMARY KEY ([SessionId]),
    CONSTRAINT [FK_SessionLogs_Devices_DeviceId] FOREIGN KEY ([DeviceId]) REFERENCES [Devices] ([DeviceId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SessionLogs_Users_AgentId] FOREIGN KEY ([AgentId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SessionLogs_Users_ViewerId] FOREIGN KEY ([ViewerId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [AuditLogs] (
    [Id] bigint NOT NULL IDENTITY,
    [UserId] nvarchar(50) NULL,
    [Action] nvarchar(100) NOT NULL,
    [EntityType] nvarchar(50) NOT NULL,
    [EntityId] nvarchar(50) NULL,
    [Timestamp] datetime2 NOT NULL,
    [IPAddress] nvarchar(45) NULL,
    [UserAgent] nvarchar(500) NULL,
    [Location] nvarchar(100) NULL,
    [Severity] nvarchar(20) NOT NULL,
    [Summary] nvarchar(200) NULL,
    [Details] nvarchar(2000) NULL,
    [OldValues] nvarchar(4000) NULL,
    [NewValues] nvarchar(4000) NULL,
    [SessionId] nvarchar(50) NULL,
    [DeviceId] nvarchar(50) NULL,
    [IsSecurityEvent] bit NOT NULL,
    [IsSuccessful] bit NOT NULL,
    [ErrorMessage] nvarchar(500) NULL,
    [Metadata] nvarchar(2000) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AuditLogs_Devices_DeviceId] FOREIGN KEY ([DeviceId]) REFERENCES [Devices] ([DeviceId]) ON DELETE SET NULL,
    CONSTRAINT [FK_AuditLogs_SessionLogs_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [SessionLogs] ([SessionId]) ON DELETE SET NULL,
    CONSTRAINT [FK_AuditLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE SET NULL
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'UserId', N'CreatedAt', N'Email', N'EmailVerified', N'FirstName', N'IsActive', N'LastLogin', N'LastName', N'LastPasswordChange', N'PasswordHash', N'PhoneNumber', N'Username') AND [object_id] = OBJECT_ID(N'[Users]'))
    SET IDENTITY_INSERT [Users] ON;
INSERT INTO [Users] ([UserId], [CreatedAt], [Email], [EmailVerified], [FirstName], [IsActive], [LastLogin], [LastName], [LastPasswordChange], [PasswordHash], [PhoneNumber], [Username])
VALUES (N'admin-001', '2025-07-25T18:18:36.2914678Z', N'admin@remotecontrol.local', CAST(1 AS bit), N'System', CAST(1 AS bit), NULL, N'Administrator', NULL, N'$2a$11$example.hash.for.admin.password', NULL, N'admin'),
(N'demo-001', '2025-07-25T18:18:36.2914727Z', N'demo@remotecontrol.local', CAST(1 AS bit), N'Demo', CAST(1 AS bit), NULL, N'User', NULL, N'$2a$11$example.hash.for.demo.password', NULL, N'demo');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'UserId', N'CreatedAt', N'Email', N'EmailVerified', N'FirstName', N'IsActive', N'LastLogin', N'LastName', N'LastPasswordChange', N'PasswordHash', N'PhoneNumber', N'Username') AND [object_id] = OBJECT_ID(N'[Users]'))
    SET IDENTITY_INSERT [Users] OFF;
GO

CREATE INDEX [IX_AuditLogs_Action] ON [AuditLogs] ([Action]);
GO

CREATE INDEX [IX_AuditLogs_DeviceId] ON [AuditLogs] ([DeviceId]);
GO

CREATE INDEX [IX_AuditLogs_EntityType] ON [AuditLogs] ([EntityType]);
GO

CREATE INDEX [IX_AuditLogs_SessionId] ON [AuditLogs] ([SessionId]);
GO

CREATE INDEX [IX_AuditLogs_Timestamp] ON [AuditLogs] ([Timestamp]);
GO

CREATE INDEX [IX_AuditLogs_UserId] ON [AuditLogs] ([UserId]);
GO

CREATE INDEX [IX_ConnectionPermissions_DeviceId] ON [ConnectionPermissions] ([DeviceId]);
GO

CREATE INDEX [IX_ConnectionPermissions_GranteeId] ON [ConnectionPermissions] ([GranteeId]);
GO

CREATE INDEX [IX_ConnectionPermissions_GranterId] ON [ConnectionPermissions] ([GranterId]);
GO

CREATE UNIQUE INDEX [IX_ConnectionPermissions_GranterId_GranteeId_DeviceId] ON [ConnectionPermissions] ([GranterId], [GranteeId], [DeviceId]);
GO

CREATE UNIQUE INDEX [IX_Devices_DeviceId] ON [Devices] ([DeviceId]);
GO

CREATE INDEX [IX_Devices_OwnerId] ON [Devices] ([OwnerId]);
GO

CREATE INDEX [IX_SessionLogs_AgentId] ON [SessionLogs] ([AgentId]);
GO

CREATE INDEX [IX_SessionLogs_DeviceId] ON [SessionLogs] ([DeviceId]);
GO

CREATE UNIQUE INDEX [IX_SessionLogs_SessionId] ON [SessionLogs] ([SessionId]);
GO

CREATE INDEX [IX_SessionLogs_StartTime] ON [SessionLogs] ([StartTime]);
GO

CREATE INDEX [IX_SessionLogs_ViewerId] ON [SessionLogs] ([ViewerId]);
GO

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
GO

CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250725181836_InitialCreate', N'8.0.7');
GO

COMMIT;
GO


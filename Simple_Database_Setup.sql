-- Simple SQL Server Database Setup for Remote Control System
-- This creates a working database that you can see in SSMS

-- Create database
CREATE DATABASE RemoteControlSystem;
GO

-- Use the database
USE RemoteControlSystem;
GO

-- Create Users table
CREATE TABLE Users (
    UserId NVARCHAR(50) PRIMARY KEY,
    Username NVARCHAR(100) NOT NULL UNIQUE,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastLogin DATETIME2 NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    FirstName NVARCHAR(100) NULL,
    LastName NVARCHAR(100) NULL,
    PhoneNumber NVARCHAR(20) NULL,
    LastPasswordChange DATETIME2 NULL,
    EmailVerified BIT NOT NULL DEFAULT 0
);
GO

-- Create Devices table
CREATE TABLE Devices (
    DeviceId NVARCHAR(50) PRIMARY KEY,
    DeviceName NVARCHAR(200) NOT NULL,
    OwnerId NVARCHAR(50) NOT NULL,
    CurrentIP NVARCHAR(45) NOT NULL,
    Port INT NOT NULL DEFAULT 7777,
    LastSeen DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsOnline BIT NOT NULL DEFAULT 0,
    ComputerName NVARCHAR(100) NOT NULL,
    UserName NVARCHAR(100) NOT NULL,
    OperatingSystem NVARCHAR(100) NOT NULL,
    ProcessorArchitecture NVARCHAR(50) NOT NULL,
    ScreenWidth INT NOT NULL DEFAULT 1920,
    ScreenHeight INT NOT NULL DEFAULT 1080,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastConnectionAt DATETIME2 NULL,
    Description NVARCHAR(500) NULL,
    SupportsH264 BIT NOT NULL DEFAULT 1,
    MaxFrameRate INT NOT NULL DEFAULT 30,
    SupportsAudio BIT NOT NULL DEFAULT 1,
    SupportsFileTransfer BIT NOT NULL DEFAULT 1,
    RequireApproval BIT NOT NULL DEFAULT 0,
    TrustedViewers NVARCHAR(1000) NULL
);
GO

-- Create SessionLogs table 
CREATE TABLE SessionLogs (
    SessionId NVARCHAR(50) PRIMARY KEY,
    ViewerId NVARCHAR(50) NOT NULL,
    AgentId NVARCHAR(50) NOT NULL,
    DeviceId NVARCHAR(50) NOT NULL,
    StartTime DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    EndTime DATETIME2 NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Active',
    EndReason NVARCHAR(500) NULL,
    ViewerIP NVARCHAR(45) NOT NULL,
    AgentIP NVARCHAR(45) NOT NULL,
    ViewerLocation NVARCHAR(100) NULL,
    Duration TIME NULL,
    BytesTransferred BIGINT NULL,
    AverageFrameRate INT NULL,
    AverageLatency INT NULL,
    PacketsLost INT NULL,
    VideoCodec NVARCHAR(20) NULL,
    CompressionLevel NVARCHAR(20) NULL,
    RequiredApproval BIT NOT NULL DEFAULT 0,
    ApprovalTime DATETIME2 NULL,
    ApprovalReason NVARCHAR(500) NULL,
    Metadata NVARCHAR(2000) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL
);
GO

-- Insert sample data
INSERT INTO Users (UserId, Username, Email, PasswordHash, FirstName, LastName, IsActive, EmailVerified) VALUES
('admin-001', 'admin', 'admin@remotecontrol.local', '$2a$11$example.hash.for.admin.password', 'System', 'Administrator', 1, 1),
('demo-001', 'demo', 'demo@remotecontrol.local', '$2a$11$example.hash.for.demo.password', 'Demo', 'User', 1, 1);
GO

-- Insert sample device
INSERT INTO Devices (DeviceId, DeviceName, OwnerId, CurrentIP, ComputerName, UserName, OperatingSystem, ProcessorArchitecture) VALUES
('RC-001', 'Demo Computer', 'admin-001', '192.168.1.100', 'DESKTOP-DEMO', 'DemoUser', 'Windows 11', 'x64');
GO

-- Create indexes for performance
CREATE INDEX IX_Devices_OwnerId ON Devices(OwnerId);
CREATE INDEX IX_SessionLogs_ViewerId ON SessionLogs(ViewerId);
CREATE INDEX IX_SessionLogs_AgentId ON SessionLogs(AgentId);
CREATE INDEX IX_SessionLogs_DeviceId ON SessionLogs(DeviceId);
CREATE INDEX IX_SessionLogs_StartTime ON SessionLogs(StartTime);
GO

PRINT 'Database RemoteControlSystem created successfully!';
PRINT 'You can now see it in SQL Server Management Studio (SSMS)';
PRINT 'Connection String: Server=(localdb)\MSSQLLocalDB;Database=RemoteControlSystem;Integrated Security=true;';
GO

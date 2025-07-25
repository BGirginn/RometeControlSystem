# SQL Server Database Setup for Remote Control System

This document provides instructions for setting up the SQL Server database for the Remote Control System.

## Prerequisites

1. **SQL Server LocalDB** (recommended for development) or **SQL Server Express/Full**
2. **.NET 8.0 SDK**
3. **Entity Framework Core Tools** (install with: `dotnet tool install --global dotnet-ef`)

## Connection Strings

The system uses the following connection strings by default:

### Development (LocalDB)
```
Server=(localdb)\MSSQLLocalDB;Database=RemoteControlSystem;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true;
```

### Production Example
```
Server=localhost;Database=RemoteControlSystem;User Id=remotecontrol;Password=YourSecurePassword;MultipleActiveResultSets=true;TrustServerCertificate=true;
```

## Database Schema

The system includes the following main entities:

- **Users**: User accounts and authentication
- **Devices**: Registered remote devices/agents
- **SessionLogs**: Remote control session history
- **ConnectionPermissions**: User access permissions
- **AuditLogs**: System audit trail

## Setup Instructions

### Option 1: Automatic Migration (Recommended)

The applications are configured to automatically create and migrate the database on startup when `AutoMigrate` is set to `true` in appsettings.json.

1. Ensure SQL Server LocalDB is installed
2. Run the ControlServer or Admin application
3. The database will be created automatically

### Option 2: Manual Migration

If you prefer manual control:

1. Update connection strings in appsettings.json files
2. Run the migration command:
   ```bash
   dotnet ef database update --project src\RemoteControl.Core --startup-project src\RemoteControl.ControlServer
   ```

### Option 3: SQL Script

Generate and run SQL scripts manually:

```bash
# Generate SQL script
dotnet ef migrations script --project src\RemoteControl.Core --startup-project src\RemoteControl.ControlServer --output database_script.sql

# Run the script in SQL Server Management Studio or sqlcmd
sqlcmd -S (localdb)\MSSQLLocalDB -i database_script.sql
```

## Configuration Files

### ControlServer (src\RemoteControl.ControlServer\appsettings.json)
```json
{
  "Database": {
    "Provider": "SqlServer",
    "ConnectionString": "Server=(localdb)\\MSSQLLocalDB;Database=RemoteControlSystem;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true;",
    "AutoMigrate": true,
    "SeedData": true
  }
}
```

### Admin (src\RemoteControl.Admin\appsettings.json)
```json
{
  "Database": {
    "Provider": "SqlServer",
    "ConnectionString": "Server=(localdb)\\MSSQLLocalDB;Database=RemoteControlSystem;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true;",
    "AutoMigrate": true,
    "SeedData": true
  }
}
```

## Security Considerations

For production environments:

1. **Use SQL Server Authentication** instead of Trusted_Connection
2. **Create a dedicated database user** with minimal required permissions
3. **Enable SSL/TLS encryption** (`Encrypt=true`)
4. **Use strong passwords** and consider using Azure Key Vault or similar for secret management
5. **Regular backups** and disaster recovery planning

## Troubleshooting

### Common Issues

1. **LocalDB not installed**: Install SQL Server LocalDB from Microsoft
2. **Connection timeout**: Increase CommandTimeout in configuration
3. **Permission denied**: Ensure the application has database create/modify permissions
4. **Migration conflicts**: Use `dotnet ef migrations remove` to remove the last migration if needed

### Verification

To verify the database was created successfully:

```sql
-- Connect to the database
USE RemoteControlSystem;

-- Check tables were created
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';

-- Check for seed data
SELECT COUNT(*) as UserCount FROM Users;
SELECT COUNT(*) as DeviceCount FROM Devices;
```

## Development Notes

- The system supports both **SQL Server** and **PostgreSQL** through the Provider setting
- Entity Framework migrations are stored in `src\RemoteControl.Core\Migrations\`
- Database context is defined in `src\RemoteControl.Core\Data\RemoteControlDbContext.cs`
- Connection configuration is in `src\RemoteControl.Core\Data\Configuration\DatabaseOptions.cs`

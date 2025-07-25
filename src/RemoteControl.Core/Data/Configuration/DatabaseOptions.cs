namespace RemoteControl.Core.Data.Configuration;

/// <summary>
/// Database configuration options
/// </summary>
public class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// Database provider: SqlServer, PostgreSQL
    /// </summary>
    public string Provider { get; set; } = "SqlServer";

    /// <summary>
    /// Database connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Enable sensitive data logging (development only)
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; } = false;

    /// <summary>
    /// Enable detailed errors (development only)
    /// </summary>
    public bool EnableDetailedErrors { get; set; } = false;

    /// <summary>
    /// Command timeout in seconds
    /// </summary>
    public int CommandTimeout { get; set; } = 30;

    /// <summary>
    /// Auto-migrate database on startup
    /// </summary>
    public bool AutoMigrate { get; set; } = false;

    /// <summary>
    /// Seed initial data
    /// </summary>
    public bool SeedData { get; set; } = true;

    /// <summary>
    /// Connection pool size
    /// </summary>
    public int MaxPoolSize { get; set; } = 100;

    /// <summary>
    /// Connection retry count
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Connection retry delay in seconds
    /// </summary>
    public int RetryDelay { get; set; } = 5;
}

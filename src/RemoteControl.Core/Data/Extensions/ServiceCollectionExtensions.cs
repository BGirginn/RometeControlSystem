using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RemoteControl.Core.Data.Configuration;
using RemoteControl.Core.Data.Repositories;

namespace RemoteControl.Core.Data.Extensions;

/// <summary>
/// Service collection extensions for database configuration
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add database services to the service collection
    /// </summary>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure database options
        var databaseSection = configuration.GetSection(DatabaseOptions.SectionName);
        services.Configure<DatabaseOptions>(databaseSection);
        var dbOptions = databaseSection.Get<DatabaseOptions>() ?? new DatabaseOptions();

        // Configure DbContext
        services.AddDbContext<RemoteControlDbContext>(options =>
        {
            ConfigureDbContext(options, dbOptions);
        });

        // Add repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<ISessionLogRepository, SessionLogRepository>();
        services.AddScoped<IConnectionPermissionRepository, ConnectionPermissionRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        return services;
    }

    /// <summary>
    /// Configure DbContext based on provider
    /// </summary>
    private static void ConfigureDbContext(DbContextOptionsBuilder options, DatabaseOptions dbOptions)
    {
        switch (dbOptions.Provider.ToLowerInvariant())
        {
            case "sqlserver":
                options.UseSqlServer(dbOptions.ConnectionString, sqlOptions =>
                {
                    sqlOptions.CommandTimeout(dbOptions.CommandTimeout);
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: dbOptions.RetryCount,
                        maxRetryDelay: TimeSpan.FromSeconds(dbOptions.RetryDelay),
                        errorNumbersToAdd: null);
                });
                break;

            case "postgresql":
                options.UseNpgsql(dbOptions.ConnectionString, npgsqlOptions =>
                {
                    npgsqlOptions.CommandTimeout(dbOptions.CommandTimeout);
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: dbOptions.RetryCount,
                        maxRetryDelay: TimeSpan.FromSeconds(dbOptions.RetryDelay),
                        errorCodesToAdd: null);
                });
                break;

            default:
                throw new ArgumentException($"Unsupported database provider: {dbOptions.Provider}");
        }

        if (dbOptions.EnableSensitiveDataLogging)
        {
            options.EnableSensitiveDataLogging();
        }

        if (dbOptions.EnableDetailedErrors)
        {
            options.EnableDetailedErrors();
        }

        options.LogTo(Console.WriteLine, LogLevel.Information);
    }

    /// <summary>
    /// Migrate and seed database
    /// </summary>
    public static async Task<IServiceProvider> MigrateDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RemoteControlDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<RemoteControlDbContext>>();

        var dbOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();

        try
        {
            if (dbOptions.AutoMigrate)
            {
                logger.LogInformation("Applying database migrations...");
                await context.Database.MigrateAsync();
                logger.LogInformation("Database migrations completed successfully");
            }

            if (dbOptions.SeedData)
            {
                logger.LogInformation("Seeding database...");
                await SeedDatabaseAsync(context, logger);
                logger.LogInformation("Database seeding completed successfully");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during database migration or seeding");
            throw;
        }

        return serviceProvider;
    }

    /// <summary>
    /// Seed additional database data
    /// </summary>
    private static async Task SeedDatabaseAsync(RemoteControlDbContext context, ILogger logger)
    {
        // Add any additional seeding logic here
        // The initial seed data is already handled in OnModelCreating

        await context.SaveChangesAsync();
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RemoteControl.Transport.Authentication;
using RemoteControl.Transport.Configuration;
using RemoteControl.Transport.Implementations;
using RemoteControl.Transport.Interfaces;

namespace RemoteControl.Transport.Extensions;

/// <summary>
/// Extension methods for configuring transport services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add transport services to the service collection
    /// </summary>
    public static IServiceCollection AddTransportServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options
        services.Configure<TransportOptions>(configuration.GetSection(TransportOptions.SectionName));

        // Register services
        services.AddSingleton<IJwtService, JwtService>();
        services.AddScoped<ISignalRClient, SignalRClient>();

        return services;
    }

    /// <summary>
    /// Add transport services with custom options
    /// </summary>
    public static IServiceCollection AddTransportServices(this IServiceCollection services, Action<TransportOptions> configureOptions)
    {
        services.Configure(configureOptions);

        // Register services
        services.AddSingleton<IJwtService, JwtService>();
        services.AddScoped<ISignalRClient, SignalRClient>();

        return services;
    }
} 
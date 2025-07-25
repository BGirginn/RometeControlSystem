using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using RemoteControl.Agent.Services;
using RemoteControl.Transport.Interfaces;
using RemoteControl.Transport.Implementations;
using RemoteControl.Transport.Configuration;
using System.Windows;

namespace RemoteControl.Agent;

/// <summary>
/// Enhanced Agent Demo Application
/// Demonstrates the complete remote control system with:
/// - Real-time screen streaming
/// - H.264 encoding capabilities  
/// - Sub-200ms latency target
/// - User approval workflow
/// - Performance monitoring
/// </summary>
public partial class EnhancedAgentApp : Application
{
    private IHost? _host;
    private ILogger<EnhancedAgentApp>? _logger;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        try
        {
            // Build the host with enhanced services
            _host = CreateHostBuilder(e.Args).Build();
            
            // Get logger
            _logger = _host.Services.GetService<ILogger<EnhancedAgentApp>>();
            _logger?.LogInformation("🚀 Enhanced Remote Control Agent starting...");
            
            // Start the enhanced agent service
            await _host.StartAsync();
            
            _logger?.LogInformation("✅ Enhanced Agent running - Ready for connections");
            
            // Show minimal system tray presence (existing functionality)
            ShowSystemTrayIcon();
        }
        catch (Exception ex)
        {
            var logger = _host?.Services.GetService<ILogger<EnhancedAgentApp>>();
            logger?.LogError(ex, "❌ Failed to start Enhanced Agent");
            
            MessageBox.Show($"Failed to start Enhanced Remote Control Agent:\n\n{ex.Message}", 
                "Agent Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            
            Shutdown(1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            _logger?.LogInformation("🛑 Enhanced Agent shutting down...");
            
            if (_host != null)
            {
                await _host.StopAsync(TimeSpan.FromSeconds(5));
                _host.Dispose();
            }
            
            _logger?.LogInformation("✅ Enhanced Agent stopped cleanly");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during shutdown");
        }
        
        base.OnExit(e);
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                // Load configuration
                config.AddJsonFile("appsettings.json", optional: false)
                      .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
                      .AddEnvironmentVariables()
                      .AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                // Configure logging with enhanced formatting
                services.AddLogging(builder =>
                {
                    builder.AddSimpleConsole(options =>
                    {
                        options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss.fff] ";
                        options.UseUtcTimestamp = false;
                    });
                    builder.AddDebug();
                    builder.SetMinimumLevel(LogLevel.Information);
                });

                // Configure enhanced agent options
                services.Configure<AgentOptions>(context.Configuration.GetSection("Agent"));
                services.Configure<ControlServerOptions>(context.Configuration.GetSection("ControlServer"));
                services.Configure<TransportOptions>(context.Configuration.GetSection("Transport"));

                // Register transport services
                services.AddSingleton<ISignalRClient, SignalRClient>();
                
                // Register the enhanced agent service
                services.AddHostedService<EnhancedAgentService>();
                
                // Register additional services that would be needed in production
                // services.AddScoped<IScreenCaptureService, DesktopDuplicationService>();
                // services.AddScoped<IVideoEncoderService, H264EncoderService>();
                // services.AddScoped<IInputSimulationService, Win32InputSimulationService>();
            });

    private void ShowSystemTrayIcon()
    {
        try
        {
            // This would integrate with the existing system tray functionality
            // For this demo, we'll just ensure the main window doesn't show
            MainWindow = null;
            
            _logger?.LogInformation("💻 Agent running in background - Check system tray");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to setup system tray");
        }
    }
}

/// <summary>
/// Enhanced Agent Demo Launcher (alternative entry point)
/// Use this to demonstrate the enhanced remote control capabilities
/// To run: dotnet run --project RemoteControl.Agent.csproj -- --enhanced
/// </summary>
public static class EnhancedAgentLauncher
{
    [STAThread]
    public static void MainEnhanced(string[] args)
    {
        try
        {
            Console.WriteLine("🚀 Enhanced Remote Control Agent v2.0");
            Console.WriteLine("=====================================");
            Console.WriteLine();
            Console.WriteLine("Features:");
            Console.WriteLine("  ✅ Real-time screen streaming");
            Console.WriteLine("  ✅ H.264 encoding support");
            Console.WriteLine("  ✅ Sub-200ms latency target");
            Console.WriteLine("  ✅ User approval workflow");
            Console.WriteLine("  ✅ Performance monitoring");
            Console.WriteLine("  ✅ Auto-reconnection");
            Console.WriteLine();
            Console.WriteLine("Starting enhanced agent...");
            
            var app = new EnhancedAgentApp();
            app.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Fatal error: {ex.Message}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            Environment.Exit(1);
        }
    }
}

/// <summary>
/// Configuration extensions for better demo setup
/// </summary>
public static class DemoConfiguration
{
    public static string GetDemoConfigJson()
    {
        return """
        {
          "Logging": {
            "LogLevel": {
              "Default": "Information",
              "Microsoft": "Warning",
              "Microsoft.Hosting.Lifetime": "Information",
              "RemoteControl": "Debug"
            }
          },
          "Agent": {
            "SupportedCodecs": ["h264", "jpeg", "png"],
            "MaxFps": 30,
            "AutoRegister": true,
            "DefaultQuality": 80
          },
          "ControlServer": {
            "Url": "https://localhost:5001/hub",
            "TimeoutMs": 30000
          },
          "Transport": {
            "EnableCompression": true,
            "ReconnectDelayMs": 5000,
            "MaxReconnectAttempts": 10,
            "HeartbeatIntervalMs": 30000
          }
        }
        """;
    }
}

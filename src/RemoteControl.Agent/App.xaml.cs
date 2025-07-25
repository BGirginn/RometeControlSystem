using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RemoteControl.Agent.Services;
using RemoteControl.Services.Implementations;
using RemoteControl.Services.Interfaces;

namespace RemoteControl.Agent;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            // Build the host
            _host = Host.CreateDefaultBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                    logging.AddDebug();
                })
                .ConfigureServices((context, services) =>
                {
                    ConfigureServices(services);
                })
                .Build();

            await _host.StartAsync();

            // Show the main window
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Agent application failed to start: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Register Core Services
        services.AddSingleton<ITransportService, TcpTransportService>();
        services.AddSingleton<IAgentService, LocalAgentService>();
        services.AddSingleton<ISessionService, SessionService>();
        services.AddSingleton<IScreenCaptureService, WindowsScreenCaptureService>();
        services.AddSingleton<IInputSimulationService, WindowsInputSimulationService>();
        services.AddSingleton<IUserSettingsService, FileUserSettingsService>();

        // Register Agent Services
        services.AddSingleton<AgentBackgroundService>();
        
        // Register Main Window
        services.AddSingleton<MainWindow>();
    }
}


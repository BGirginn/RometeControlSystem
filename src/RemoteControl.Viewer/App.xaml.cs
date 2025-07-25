using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RemoteControl.Viewer.ViewModels;
using RemoteControl.Viewer.Views;
using RemoteControl.Services.Interfaces;
using RemoteControl.Services.Implementations;
using CommunityToolkit.Mvvm.Messaging;
using Serilog;

namespace RemoteControl.Viewer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/viewer-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Log.Information("Application starting...");

        try
        {
            // Build the host
            _host = Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices((context, services) =>
                {
                    ConfigureServices(services);
                })
                .Build();

            Log.Information("Host built, starting services...");
            await _host.StartAsync();

            Log.Information("Services started, creating main window...");
            // Show the main window
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            Log.Information("Main window created, showing...");
            mainWindow.Show();

            Log.Information("Application startup completed successfully");
            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application failed to start");
            MessageBox.Show($"Application failed to start: {ex.Message}\n\nInner Exception: {ex.InnerException?.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Register MVVM Messenger
        services.AddSingleton<IMessenger, WeakReferenceMessenger>();

        // Register Services
        services.AddSingleton<ITransportService, MockTransportService>();
        services.AddSingleton<IUserSettingsService, FileUserSettingsService>();
        services.AddSingleton<IVideoRenderer, WpfVideoRenderer>();
        services.AddSingleton<IThemeService, WpfThemeService>();

        // Register ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<ConnectViewModel>();
        services.AddTransient<StreamingViewModel>();

        // Register Views
        services.AddTransient<ConnectView>();
        services.AddTransient<StreamingView>();

        // Register Main Window as Singleton
        services.AddSingleton<MainWindow>();
    }
}


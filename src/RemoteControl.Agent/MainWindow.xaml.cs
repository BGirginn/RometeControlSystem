using System;
using System.ComponentModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RemoteControl.Agent.Services;
using RemoteControl.Core.Models;
using RemoteControl.Services.Interfaces;

namespace RemoteControl.Agent;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly IHost _host;
    private readonly ILogger<MainWindow> _logger;
    private readonly IAgentService _agentService;
    private readonly AgentBackgroundService _backgroundService;
    private NotifyIcon? _notifyIcon;
    private AgentInfo? _agentInfo;
    private bool _isClosing = false;

    public MainWindow(IHost host)
    {
        InitializeComponent();
        _host = host;
        _logger = _host.Services.GetRequiredService<ILogger<MainWindow>>();
        _agentService = _host.Services.GetRequiredService<IAgentService>();
        _backgroundService = _host.Services.GetRequiredService<AgentBackgroundService>();
        
        InitializeAsync();
        SetupSystemTray();
    }

    private async void InitializeAsync()
    {
        try
        {
            StatusText.Text = "Initializing...";
            StatusText.Foreground = System.Windows.Media.Brushes.Orange;
            
            // Get agent information
            _agentInfo = await _agentService.GetLocalAgentInfoAsync();
            
            // Update UI with agent info
            AgentIdText.Text = _agentInfo.AgentId;
            ComputerNameText.Text = _agentInfo.ComputerName;
            UserNameText.Text = _agentInfo.UserName;
            IPAddressText.Text = _agentInfo.IPAddress;
            ScreenResolutionText.Text = $"{_agentInfo.ScreenWidth} x {_agentInfo.ScreenHeight}";
            
            // Subscribe to events
            _backgroundService.ActivityLogged += OnActivityLogged;
            
            StatusText.Text = "Ready - Service stopped";
            StatusText.Foreground = System.Windows.Media.Brushes.Red;
            
            AddActivityLog("Agent initialized successfully");
            _logger.LogInformation("Agent MainWindow initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize agent");
            StatusText.Text = "Initialization failed";
            StatusText.Foreground = System.Windows.Media.Brushes.Red;
            AddActivityLog($"ERROR: {ex.Message}");
        }
    }

    private void SetupSystemTray()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "Remote Control Agent",
            Visible = false
        };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Show", null, (s, e) => ShowWindow());
        contextMenu.Items.Add("Start Service", null, (s, e) => StartService());
        contextMenu.Items.Add("Stop Service", null, (s, e) => StopService());
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("Exit", null, (s, e) => ExitApplication());

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (s, e) => ShowWindow();
    }

    private async void StartServiceButton_Click(object sender, RoutedEventArgs e)
    {
        await StartService();
    }

    private async void StopServiceButton_Click(object sender, RoutedEventArgs e)
    {
        await StopService();
    }

    private async Task StartService()
    {
        try
        {
            StatusText.Text = "Starting service...";
            StatusText.Foreground = System.Windows.Media.Brushes.Orange;
            
            StartServiceButton.IsEnabled = false;
            
            var port = int.Parse(ListenPortText.Text);
            var maxConnections = int.Parse(MaxConnectionsText.Text);
            var frameRate = (int)FrameRateSlider.Value;
            var imageQuality = (int)ImageQualitySlider.Value;
            
            await _backgroundService.StartAsync(CancellationToken.None);
            
            AddActivityLog($"Service started on port {port}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start service");
            StatusText.Text = "Failed to start service";
            StatusText.Foreground = System.Windows.Media.Brushes.Red;
            AddActivityLog($"ERROR: Failed to start service - {ex.Message}");
            StartServiceButton.IsEnabled = true;
        }
    }

    private async Task StopService()
    {
        try
        {
            StatusText.Text = "Stopping service...";
            StatusText.Foreground = System.Windows.Media.Brushes.Orange;
            
            StopServiceButton.IsEnabled = false;
            
            await _backgroundService.StopAsync(CancellationToken.None);
            
            AddActivityLog("Service stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop service");
            AddActivityLog($"ERROR: Failed to stop service - {ex.Message}");
        }
    }

    private void CopyAgentIdButton_Click(object sender, RoutedEventArgs e)
    {
        if (_agentInfo != null)
        {
            System.Windows.Clipboard.SetText(_agentInfo.AgentId);
            AddActivityLog("Agent ID copied to clipboard");
        }
    }

    private void MinimizeToTrayButton_Click(object sender, RoutedEventArgs e)
    {
        HideWindow();
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        ExitApplication();
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        if (!_isClosing)
        {
            e.Cancel = true;
            HideWindow();
        }
    }

    private void ShowWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        _notifyIcon!.Visible = false;
    }

    private void HideWindow()
    {
        Hide();
        _notifyIcon!.Visible = true;
        _notifyIcon.ShowBalloonTip(3000, "Remote Control Agent", "Agent minimized to system tray", ToolTipIcon.Info);
    }

    private void ExitApplication()
    {
        _isClosing = true;
        _notifyIcon?.Dispose();
        
        Task.Run(async () =>
        {
            try
            {
                await _backgroundService.StopAsync(CancellationToken.None);
                await _host.StopAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during shutdown");
            }
            finally
            {
                Dispatcher.Invoke(() => System.Windows.Application.Current.Shutdown());
            }
        });
    }

    // Commented out - events no longer exist in AgentBackgroundService
    /*
    private void OnServiceStatusChanged(object? sender, string status)
    {
        Dispatcher.Invoke(() =>
        {
            StatusText.Text = status;
            
            var isRunning = status.Contains("running", StringComparison.OrdinalIgnoreCase);
            StatusText.Foreground = isRunning ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
            
            StartServiceButton.IsEnabled = !isRunning;
            StopServiceButton.IsEnabled = isRunning;
            
            ConnectionStatusText.Text = isRunning ? "Listening for connections" : "Not listening";
        });
    }

    private void OnConnectionReceived(object? sender, string connectionInfo)
    {
        Dispatcher.Invoke(() =>
        {
            if (ShowConnectionDialogCheck.IsChecked == true)
            {
                var result = System.Windows.MessageBox.Show(
                    $"Incoming connection request:\n{connectionInfo}\n\nAllow this connection?",
                    "Connection Request",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                // For now, we'll just log the result. In a real implementation,
                // this would communicate back to the background service
                AddActivityLog($"Connection request {(result == MessageBoxResult.Yes ? "accepted" : "denied")}: {connectionInfo}");
            }
            
            LastConnectionText.Text = $"Last connection: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        });
    }
    */

    private void OnActivityLogged(object? sender, string activity)
    {
        Dispatcher.Invoke(() =>
        {
            AddActivityLog(activity);
        });
    }

    private void AddActivityLog(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var logEntry = $"[{timestamp}] {message}\n";
        
        ActivityLogText.Text += logEntry;
        
        // Keep only last 100 lines
        var lines = ActivityLogText.Text.Split('\n');
        if (lines.Length > 100)
        {
            ActivityLogText.Text = string.Join('\n', lines.Skip(lines.Length - 100));
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _notifyIcon?.Dispose();
        base.OnClosed(e);
    }
}
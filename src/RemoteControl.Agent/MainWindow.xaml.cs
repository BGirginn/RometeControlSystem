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
using Microsoft.Extensions.Options;
using RemoteControl.Agent.Services;
using RemoteControl.Agent.Views;
using RemoteControl.Core.Models;
using RemoteControl.Protocol.Messages;

namespace RemoteControl.Agent;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly IHost _host;
    private readonly ILogger<MainWindow> _logger;
    private readonly ModernAgentService _modernAgentService;
    private readonly IOptions<AgentOptions> _agentOptions;
    private NotifyIcon? _notifyIcon;
    private bool _isClosing = false;
    private bool _serviceRunning = false;

    public MainWindow(IHost host)
    {
        InitializeComponent();
        _host = host;
        _logger = _host.Services.GetRequiredService<ILogger<MainWindow>>();
        _modernAgentService = _host.Services.GetRequiredService<ModernAgentService>();
        _agentOptions = _host.Services.GetRequiredService<IOptions<AgentOptions>>();
        
        InitializeAsync();
        SetupSystemTray();
    }

    private async void InitializeAsync()
    {
        try
        {
            StatusText.Text = "Initializing...";
            StatusText.Foreground = System.Windows.Media.Brushes.Orange;
            
            // Update UI with agent info
            AgentIdText.Text = "Connecting...";
            ComputerNameText.Text = Environment.MachineName;
            UserNameText.Text = Environment.UserName;
            IPAddressText.Text = GetLocalIPAddress();
            ScreenResolutionText.Text = $"{(int)SystemParameters.PrimaryScreenWidth} x {(int)SystemParameters.PrimaryScreenHeight}";
            
            // Subscribe to events
            _modernAgentService.ActivityLogged += OnActivityLogged;
            _modernAgentService.SessionRequested += OnSessionRequested;
            _modernAgentService.ConnectionStateChanged += OnConnectionStateChanged;
            
            StatusText.Text = "Ready - Service stopped";
            StatusText.Foreground = System.Windows.Media.Brushes.Red;
            
            AddActivityLog("Agent initialized successfully");
            _logger.LogInformation("Agent MainWindow initialized");
            
            // Auto-start service if configured
            if (_agentOptions.Value.AutoStartService)
            {
                await StartService();
            }
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
        contextMenu.Items.Add("Start Service", null, async (s, e) => await StartService());
        contextMenu.Items.Add("Stop Service", null, async (s, e) => await StopService());
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
            if (_serviceRunning) return;
            
            StatusText.Text = "Starting service...";
            StatusText.Foreground = System.Windows.Media.Brushes.Orange;
            
            StartServiceButton.IsEnabled = false;
            
            await _modernAgentService.StartAsync(CancellationToken.None);
            _serviceRunning = true;
            
            AddActivityLog("Modern Agent service started");
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
            if (!_serviceRunning) return;
            
            StatusText.Text = "Stopping service...";
            StatusText.Foreground = System.Windows.Media.Brushes.Orange;
            
            StopServiceButton.IsEnabled = false;
            
            await _modernAgentService.StopAsync(CancellationToken.None);
            _serviceRunning = false;
            
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
        if (!string.IsNullOrEmpty(_modernAgentService.AgentId))
        {
            System.Windows.Clipboard.SetText(_modernAgentService.AgentId);
            AddActivityLog("Agent ID copied to clipboard");
        }
        else
        {
            AddActivityLog("Agent ID not available - service not connected");
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
                if (_serviceRunning)
                {
                    await _modernAgentService.StopAsync(CancellationToken.None);
                }
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

    private async void OnSessionRequested(object? sender, RequestSessionMessage sessionRequest)
    {
        try
        {
            // Show approval dialog on UI thread
            bool? approved = null;
            string? reason = null;
            
            await Dispatcher.InvokeAsync(() =>
            {
                var dialog = new SessionApprovalDialog(sessionRequest);
                var result = dialog.ShowDialog();
                
                approved = dialog.UserResponse;
                reason = dialog.DenyReason;
            });
            
            // Respond to the session request
            if (approved.HasValue)
            {
                await _modernAgentService.RespondToSessionRequestAsync(
                    sessionRequest.MessageId ?? Guid.NewGuid().ToString(), 
                    approved.Value, 
                    reason);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling session request");
            AddActivityLog($"Error handling session request: {ex.Message}");
        }
    }

    private void OnConnectionStateChanged(object? sender, bool isConnected)
    {
        Dispatcher.Invoke(() =>
        {
            if (isConnected)
            {
                StatusText.Text = "Connected to ControlServer";
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
                
                AgentIdText.Text = _modernAgentService.AgentId ?? "Connected (ID pending)";
                
                StartServiceButton.IsEnabled = false;
                StopServiceButton.IsEnabled = true;
                
                ConnectionStatusText.Text = "Connected and ready for sessions";
            }
            else
            {
                StatusText.Text = _serviceRunning ? "Connecting..." : "Disconnected";
                StatusText.Foreground = _serviceRunning ? System.Windows.Media.Brushes.Orange : System.Windows.Media.Brushes.Red;
                
                if (!_serviceRunning)
                {
                    AgentIdText.Text = "Not connected";
                    StartServiceButton.IsEnabled = true;
                    StopServiceButton.IsEnabled = false;
                    ConnectionStatusText.Text = "Not connected";
                }
            }
        });
    }

    private string GetLocalIPAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "127.0.0.1";
        }
        catch
        {
            return "Unknown";
        }
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
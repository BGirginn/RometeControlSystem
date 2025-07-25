using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RemoteControl.Core.Models;
using RemoteControl.Services.Interfaces;

namespace RemoteControl.Viewer.Views
{
    public partial class LoginWindow : Window
    {
        private readonly ILogger<LoginWindow> _logger;
        private readonly IRegistryClientService _registryClient;
        private bool _isConnectedToServer;
        private bool _isLoggedIn;
        private string? _currentUserId;

        public bool IsLoggedIn => _isLoggedIn;
        public string? UserId => _currentUserId;
        public bool CanConnect => !_isConnectedToServer && !string.IsNullOrWhiteSpace(ServerUrlTextBox.Text);
        public bool CanLogin => _isConnectedToServer && !_isLoggedIn;
        public string ComputerName => Environment.MachineName;
        public string DeviceId { get; private set; } = string.Empty;

        public LoginWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            
            _logger = serviceProvider.GetRequiredService<ILogger<LoginWindow>>();
            _registryClient = serviceProvider.GetRequiredService<IRegistryClientService>();
            
            DataContext = this;
            
            // Subscribe to registry client events
            _registryClient.ConnectionStatusChanged += OnConnectionStatusChanged;
            
            DeviceNameTextBox.Text = ComputerName;
            UpdateUI();
        }

        private async void ConnectToServerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ConnectToServerButton.IsEnabled = false;
                ServerStatusText.Text = "Connecting...";
                ServerStatusText.Foreground = System.Windows.Media.Brushes.Orange;
                
                var serverUrl = ServerUrlTextBox.Text.Trim();
                if (string.IsNullOrEmpty(serverUrl))
                {
                    MessageBox.Show("Please enter a server URL.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                var connected = await _registryClient.ConnectAsync(serverUrl);
                if (connected)
                {
                    _isConnectedToServer = true;
                    ServerStatusText.Text = "Connected to server";
                    ServerStatusText.Foreground = System.Windows.Media.Brushes.Green;
                    StatusText.Text = "Connected to registry server. Please login or create an account.";
                }
                else
                {
                    ServerStatusText.Text = "Failed to connect";
                    ServerStatusText.Foreground = System.Windows.Media.Brushes.Red;
                    StatusText.Text = "Failed to connect to registry server. Please check the URL and try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to server");
                MessageBox.Show($"Connection error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ServerStatusText.Text = "Connection failed";
                ServerStatusText.Foreground = System.Windows.Media.Brushes.Red;
            }
            finally
            {
                UpdateUI();
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoginButton.IsEnabled = false;
                StatusText.Text = "Logging in...";
                
                var username = UsernameTextBox.Text.Trim();
                var password = PasswordBox.Password;
                
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Please enter both username and password.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                var loginRequest = new LoginRequest
                {
                    Username = username,
                    Password = password
                };
                
                var response = await _registryClient.LoginAsync(loginRequest);
                if (response.Success)
                {
                    _isLoggedIn = true;
                    _currentUserId = response.UserId;
                    StatusText.Text = $"Logged in successfully as {username}";
                    
                    // Register this device if it's a new user
                    await RegisterCurrentDevice();
                }
                else
                {
                    MessageBox.Show(response.Message ?? "Login failed", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusText.Text = "Login failed. Please check your credentials.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                MessageBox.Show($"Login error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Login error occurred.";
            }
            finally
            {
                UpdateUI();
            }
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var registerDialog = new RegisterDialog();
                if (registerDialog.ShowDialog() == true)
                {
                    RegisterButton.IsEnabled = false;
                    StatusText.Text = "Creating account...";
                    
                    var registerRequest = new RegisterRequest
                    {
                        Username = registerDialog.Username,
                        Email = registerDialog.Email,
                        Password = registerDialog.Password,
                        DeviceName = DeviceNameTextBox.Text.Trim()
                    };
                    
                    var response = await _registryClient.RegisterAsync(registerRequest);
                    if (response.Success)
                    {
                        _isLoggedIn = true;
                        _currentUserId = response.UserId;
                        StatusText.Text = $"Account created successfully! Logged in as {registerRequest.Username}";
                        
                        // Register this device
                        await RegisterCurrentDevice();
                        
                        DeviceRegistrationGroup.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        MessageBox.Show(response.Message ?? "Registration failed", "Registration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        StatusText.Text = "Registration failed.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                MessageBox.Show($"Registration error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Registration error occurred.";
            }
            finally
            {
                UpdateUI();
            }
        }

        private async System.Threading.Tasks.Task RegisterCurrentDevice()
        {
            try
            {
                var deviceRequest = new DeviceRegistrationRequest
                {
                    UserId = _currentUserId ?? string.Empty,
                    DeviceName = DeviceNameTextBox.Text.Trim(),
                    ComputerName = Environment.MachineName,
                    UserName = Environment.UserName,
                    OperatingSystem = Environment.OSVersion.ToString()
                };
                
                DeviceId = await _registryClient.RegisterDeviceAsync(deviceRequest);
                DeviceIdTextBox.Text = DeviceId;
                
                if (!string.IsNullOrEmpty(DeviceId))
                {
                    DeviceRegistrationGroup.Visibility = Visibility.Visible;
                    _logger.LogInformation("Device registered with ID: {DeviceId}", DeviceId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering device");
            }
        }

        private void OnConnectionStatusChanged(object? sender, string status)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = $"Registry connection: {status}";
            });
        }

        private void UpdateUI()
        {
            ConnectToServerButton.IsEnabled = CanConnect;
            LoginButton.IsEnabled = CanLogin;
            RegisterButton.IsEnabled = CanLogin;
            OkButton.IsEnabled = IsLoggedIn;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isLoggedIn)
            {
                DialogResult = true;
                Close();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _registryClient.ConnectionStatusChanged -= OnConnectionStatusChanged;
            base.OnClosed(e);
        }
    }

    // Simple registration dialog
    public partial class RegisterDialog : Window
    {
        public string Username { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public string Password { get; private set; } = string.Empty;

        public RegisterDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Title = "Create Account";
            Width = 400;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;

            var grid = new Grid { Margin = new Thickness(20) };
            Content = grid;

            // Simple form for demo - in production you'd create proper XAML
            var textBlock = new TextBlock { Text = "Registration form placeholder" };
            grid.Children.Add(textBlock);
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RemoteControl.Core.Events;
using RemoteControl.Core.Models;
using RemoteControl.Services.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RemoteControl.Viewer.ViewModels
{
    public partial class ConnectViewModel : ObservableObject
    {
        private readonly ITransportService _transportService;
        private readonly IUserSettingsService _userSettingsService;
        private readonly IRegistryClientService? _registryClient;
        private readonly IMessenger _messenger;

        [ObservableProperty]
        private string _targetId = string.Empty;

        [ObservableProperty]
        private string _userToken = string.Empty;

        [ObservableProperty]
        private bool _isConnecting = false;

        [ObservableProperty]
        private string _statusMessage = "Ready to connect";

        [ObservableProperty]
        private string _connectionError = string.Empty;

        [ObservableProperty]
        private bool _hasError = false;

        [ObservableProperty]
        private bool _isLoggedIn = false;

        [ObservableProperty]
        private string _currentUser = string.Empty;

        public ObservableCollection<RecentConnection> RecentConnections { get; } = new();
        public ObservableCollection<Device> AvailableDevices { get; } = new();

        public ConnectViewModel(
            ITransportService transportService, 
            IUserSettingsService userSettingsService,
            IRegistryClientService? registryClient,
            IMessenger messenger)
        {
            _transportService = transportService;
            _userSettingsService = userSettingsService;
            _registryClient = registryClient;
            _messenger = messenger;

            // Subscribe to connection state changes
            _transportService.ConnectionStateChanged += OnConnectionStateChanged;
            
            LoadUserSettings();
            LoadRecentConnections();
        }

        [RelayCommand(CanExecute = nameof(CanConnect))]
        private async Task ConnectAsync()
        {
            try
            {
                IsConnecting = true;
                HasError = false;
                ConnectionError = string.Empty;
                StatusMessage = "Connecting...";

                var request = new ConnectionRequest
                {
                    TargetId = TargetId,
                    UserToken = UserToken,
                    UserIdentity = Environment.UserName,
                    UserLocation = Environment.MachineName,
                    RequestTime = DateTime.UtcNow
                };

                await _transportService.ConnectAsync(request);
                
                // Save token and add to recent connections
                await _userSettingsService.SaveUserTokenAsync(UserToken);
                await _userSettingsService.AddRecentConnectionAsync(TargetId);
                
                // Navigate to streaming view
                _messenger.Send(new NavigationMessage("Streaming"));
            }
            catch (Exception ex)
            {
                HasError = true;
                ConnectionError = ex.Message;
                StatusMessage = "Connection failed";
            }
            finally
            {
                IsConnecting = false;
            }
        }

        [RelayCommand]
        private void CancelConnection()
        {
            _transportService.DisconnectAsync();
            IsConnecting = false;
            StatusMessage = "Ready to connect";
        }

        [RelayCommand]
        private void ConnectToRecent(RecentConnection connection)
        {
            TargetId = connection.TargetId;
            _ = ConnectAsync();
        }

        [RelayCommand]
        private async Task RemoveRecentConnection(RecentConnection connection)
        {
            RecentConnections.Remove(connection);
            // Remove from settings service
            await _userSettingsService.RemoveRecentConnectionAsync(connection.TargetId);
        }

        private bool CanConnect()
        {
            return !IsConnecting && 
                   !string.IsNullOrWhiteSpace(TargetId) && 
                   !string.IsNullOrWhiteSpace(UserToken);
        }

        private async void LoadUserSettings()
        {
            try
            {
                UserToken = await _userSettingsService.GetUserTokenAsync();
            }
            catch (Exception ex)
            {
                // Log error but don't block UI
                System.Diagnostics.Debug.WriteLine($"Failed to load user token: {ex.Message}");
            }
        }

        private async void LoadRecentConnections()
        {
            try
            {
                var recent = await _userSettingsService.GetRecentConnectionsAsync();
                RecentConnections.Clear();
                foreach (var connection in recent)
                {
                    RecentConnections.Add(connection);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load recent connections: {ex.Message}");
            }
        }

        private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
        {
            StatusMessage = e.NewState switch
            {
                Core.Enums.ConnectionState.Resolving => "Resolving target...",
                Core.Enums.ConnectionState.Connecting => "Establishing connection...",
                Core.Enums.ConnectionState.Authenticating => "Authenticating...",
                Core.Enums.ConnectionState.Connected => "Connected successfully",
                Core.Enums.ConnectionState.Error => $"Error: {e.Message}",
                Core.Enums.ConnectionState.Disconnected => "Disconnected",
                _ => StatusMessage
            };

            if (e.NewState == Core.Enums.ConnectionState.Error)
            {
                HasError = true;
                ConnectionError = e.Message ?? "Unknown error occurred";
                IsConnecting = false;
            }
        }

        partial void OnTargetIdChanged(string value)
        {
            ConnectCommand.NotifyCanExecuteChanged();
        }

        partial void OnUserTokenChanged(string value)
        {
            ConnectCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private async Task LoadAvailableDevices()
        {
            if (_registryClient == null || !_registryClient.IsConnected)
            {
                StatusMessage = "Not connected to registry server";
                return;
            }

            try
            {
                StatusMessage = "Loading available devices...";
                var response = await _registryClient.GetAvailableDevicesAsync();
                
                if (response.Success)
                {
                    AvailableDevices.Clear();
                    foreach (var device in response.Devices.Where(d => d.IsOnline))
                    {
                        AvailableDevices.Add(device);
                    }
                    StatusMessage = $"Found {AvailableDevices.Count} available devices";
                }
                else
                {
                    StatusMessage = response.Message ?? "Failed to load devices";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading devices: {ex.Message}";
            }
        }

        [RelayCommand]
        private void SelectDevice(Device device)
        {
            if (device != null)
            {
                TargetId = device.DeviceId;
                StatusMessage = $"Selected device: {device.DeviceName} ({device.DeviceId})";
            }
        }

        public void SetLoggedInUser(string userId, string username)
        {
            CurrentUser = username;
            IsLoggedIn = true;
            StatusMessage = $"Logged in as {username}";
            
            // Load available devices after login
            _ = LoadAvailableDevices();
        }

        public void Logout()
        {
            IsLoggedIn = false;
            CurrentUser = string.Empty;
            AvailableDevices.Clear();
            StatusMessage = "Logged out";
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            
            if (e.PropertyName == nameof(IsConnecting))
            {
                ConnectCommand.NotifyCanExecuteChanged();
            }
        }
    }

    // Navigation message for MVVM messaging
    public record NavigationMessage(string ViewName);
}

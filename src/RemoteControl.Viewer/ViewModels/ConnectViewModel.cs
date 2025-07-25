using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RemoteControl.Core.Events;
using RemoteControl.Core.Models;
using RemoteControl.Services.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RemoteControl.Viewer.ViewModels
{
    public partial class ConnectViewModel : ObservableObject
    {
        private readonly ITransportService _transportService;
        private readonly IUserSettingsService _userSettingsService;
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

        public ObservableCollection<RecentConnection> RecentConnections { get; } = new();

        public ConnectViewModel(
            ITransportService transportService, 
            IUserSettingsService userSettingsService,
            IMessenger messenger)
        {
            _transportService = transportService;
            _userSettingsService = userSettingsService;
            _messenger = messenger;

            // Subscribe to connection state changes
            _transportService.ConnectionStateChanged += OnConnectionStateChanged;
            
            LoadUserSettings();
            LoadRecentConnections();
        }

        [RelayCommand(CanExecute = nameof(CanConnect))]
        private void Connect()
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

                _transportService.ConnectAsync(request);
                
                // Save token and add to recent connections
                _userSettingsService.SaveUserTokenAsync(UserToken);
                _userSettingsService.AddRecentConnectionAsync(TargetId);
                
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
            Connect();
        }

        [RelayCommand]
        private void RemoveRecentConnection(RecentConnection connection)
        {
            RecentConnections.Remove(connection);
            // TODO: Implement removal from settings service
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

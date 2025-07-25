using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RemoteControl.Core.Events;
using RemoteControl.Core.Models;
using System.Collections.ObjectModel;
using System.Windows;

namespace RemoteControl.Agent.ViewModels
{
    public partial class AgentTrayViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isOnline = true;

        [ObservableProperty]
        private string _statusText = "Ready";

        [ObservableProperty]
        private int _activeConnections = 0;

        [ObservableProperty]
        private ConnectionRequest? _pendingRequest;

        [ObservableProperty]
        private bool _hasPendingRequest = false;

        [ObservableProperty]
        private bool _autoAcceptTrustedUsers = false;

        public ObservableCollection<string> TrustedUsers { get; } = new();
        public ObservableCollection<ConnectionRequest> ActiveSessions { get; } = new();

        public AgentTrayViewModel()
        {
            StatusText = IsOnline ? "Ready - Accepting connections" : "Offline";
        }

        [RelayCommand]
        private void ToggleOnlineStatus()
        {
            IsOnline = !IsOnline;
            StatusText = IsOnline ? "Ready - Accepting connections" : "Offline";
        }

        [RelayCommand]
        private void AcceptConnection()
        {
            if (PendingRequest != null)
            {
                // TODO: Implement connection acceptance
                ActiveSessions.Add(PendingRequest);
                ActiveConnections = ActiveSessions.Count;
                
                PendingRequest = null;
                HasPendingRequest = false;
                StatusText = $"Active session - {ActiveConnections} connection(s)";
            }
        }

        [RelayCommand]
        private void RejectConnection()
        {
            if (PendingRequest != null)
            {
                // TODO: Implement connection rejection
                PendingRequest = null;
                HasPendingRequest = false;
                StatusText = IsOnline ? "Ready - Accepting connections" : "Offline";
            }
        }

        [RelayCommand]
        private void ShowSettings()
        {
            // TODO: Show settings window
        }

        [RelayCommand]
        private void ExitApplication()
        {
            Application.Current.Shutdown();
        }

        public void HandleIncomingConnection(ConnectionRequest request)
        {
            // Check if user is trusted for auto-accept
            if (AutoAcceptTrustedUsers && TrustedUsers.Contains(request.UserIdentity))
            {
                AcceptConnection();
                return;
            }

            // Show connection request dialog
            PendingRequest = request;
            HasPendingRequest = true;
            StatusText = "Incoming connection request";
        }

        public void HandleConnectionClosed(string sessionId)
        {
            var session = ActiveSessions.FirstOrDefault(s => s.UserToken == sessionId);
            if (session != null)
            {
                ActiveSessions.Remove(session);
                ActiveConnections = ActiveSessions.Count;
                StatusText = ActiveConnections > 0 
                    ? $"Active session - {ActiveConnections} connection(s)"
                    : (IsOnline ? "Ready - Accepting connections" : "Offline");
            }
        }
    }
}

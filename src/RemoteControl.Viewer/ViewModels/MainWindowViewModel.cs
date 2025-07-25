using System;
using System.Windows;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using RemoteControl.Services.Interfaces;

namespace RemoteControl.Viewer.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject, IRecipient<NavigationMessage>
    {
        private readonly ILogger<MainWindowViewModel> _logger;
        private readonly IThemeService _themeService;
        private readonly IMessenger _messenger;

        [ObservableProperty]
        private object? _currentView;

        [ObservableProperty]
        private string _currentTheme = "Light";

        [ObservableProperty]
        private string _windowTitle = "Remote Control Viewer";

        [ObservableProperty]
        private bool _isFullscreen = false;

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _loadingMessage = "Loading...";

        public ObservableCollection<string> AvailableThemes { get; } = new() { "Light", "Dark", "Auto" };

        // Child ViewModels
        public ConnectViewModel ConnectViewModel { get; }
        public StreamingViewModel StreamingViewModel { get; }

        public MainWindowViewModel(
            ConnectViewModel connectViewModel,
            StreamingViewModel streamingViewModel,
            IThemeService themeService,
            IMessenger messenger,
            ILogger<MainWindowViewModel> logger)
        {
            ConnectViewModel = connectViewModel;
            StreamingViewModel = streamingViewModel;
            _themeService = themeService;
            _messenger = messenger;
            _logger = logger;

            // Register for navigation messages
            _messenger.Register<NavigationMessage>(this);

            // Start with Connect view
            CurrentView = ConnectViewModel;

            // Load current theme
            LoadCurrentTheme();
        }

        [RelayCommand]
        private void ChangeTheme(string theme)
        {
            try
            {
                CurrentTheme = theme;
                _themeService.SetThemeAsync(theme);
                _logger.LogInformation("Theme changed to {Theme}", theme);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to change theme to {Theme}", theme);
            }
        }

        [RelayCommand]
        private void ToggleFullscreen()
        {
            IsFullscreen = !IsFullscreen;
        }

        [RelayCommand]
        private void ShowAbout()
        {
            // TODO: Show about dialog
            _logger.LogInformation("About dialog requested");
        }

        [RelayCommand]
        private void ShowSettings()
        {
            // TODO: Navigate to settings view or show settings dialog
            _logger.LogInformation("Settings dialog requested");
        }

        [RelayCommand]
        private void ExitApplication()
        {
            try
            {
                // Cleanup connections if any
                if (CurrentView == StreamingViewModel)
                {
                    StreamingViewModel.DisconnectCommand.Execute(null);
                }

                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during application exit");
                Application.Current.Shutdown();
            }
        }

        public void Receive(NavigationMessage message)
        {
            _logger.LogDebug("Navigation requested to {ViewName}", message.ViewName);

            CurrentView = message.ViewName switch
            {
                "Connect" => ConnectViewModel,
                "Streaming" => StreamingViewModel,
                _ => ConnectViewModel
            };

            // Update window title based on current view
            if (ReferenceEquals(CurrentView, ConnectViewModel))
            {
                WindowTitle = "Remote Control Viewer - Connect";
            }
            else if (ReferenceEquals(CurrentView, StreamingViewModel))
            {
                WindowTitle = "Remote Control Viewer - Session Active";
            }
            else
            {
                WindowTitle = "Remote Control Viewer";
            }
        }

        private async void LoadCurrentTheme()
        {
            try
            {
                CurrentTheme = await _themeService.GetThemeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load current theme, using default");
                CurrentTheme = "Light";
            }
        }

        partial void OnIsFullscreenChanged(bool value)
        {
            // Notify StreamingViewModel about fullscreen change
            if (CurrentView == StreamingViewModel)
            {
                StreamingViewModel.IsFullscreen = value;
            }
        }

        public void Dispose()
        {
            _messenger.Unregister<NavigationMessage>(this);
            StreamingViewModel?.Dispose();
        }
    }
}

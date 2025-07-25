using System.Diagnostics;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using RemoteControl.Viewer.ViewModels;
using RemoteControl.Services.Interfaces;
using CommunityToolkit.Mvvm.Messaging;
using RemoteControl.Core.Models;

namespace RemoteControl.Viewer.Tests
{
    public class ConnectViewModelTests
    {
        private readonly Mock<ITransportService> _mockTransportService;
        private readonly Mock<IUserSettingsService> _mockUserSettingsService;
        private readonly Mock<IMessenger> _mockMessenger;
        private readonly ConnectViewModel _viewModel;

        public ConnectViewModelTests()
        {
            _mockTransportService = new Mock<ITransportService>();
            _mockUserSettingsService = new Mock<IUserSettingsService>();
            _mockMessenger = new Mock<IMessenger>();

            _viewModel = new ConnectViewModel(
                _mockTransportService.Object,
                _mockUserSettingsService.Object,
                _mockMessenger.Object);
        }

        [Fact]
        public void ConnectCommand_CanExecute_ReturnsFalseWhenTargetIdEmpty()
        {
            // Arrange
            _viewModel.TargetId = "";
            _viewModel.UserToken = "valid-token";

            // Act
            var canExecute = _viewModel.ConnectCommand.CanExecute(null);

            // Assert
            Assert.False(canExecute);
        }

        [Fact]
        public void ConnectCommand_CanExecute_ReturnsFalseWhenUserTokenEmpty()
        {
            // Arrange
            _viewModel.TargetId = "target-123";
            _viewModel.UserToken = "";

            // Act
            var canExecute = _viewModel.ConnectCommand.CanExecute(null);

            // Assert
            Assert.False(canExecute);
        }

        [Fact]
        public void ConnectCommand_CanExecute_ReturnsTrueWhenBothFieldsValid()
        {
            // Arrange
            _viewModel.TargetId = "target-123";
            _viewModel.UserToken = "valid-token";

            // Act
            var canExecute = _viewModel.ConnectCommand.CanExecute(null);

            // Assert
            Assert.True(canExecute);
        }

        [Fact]
        public void ConnectCommand_CanExecute_ReturnsFalseWhenConnecting()
        {
            // Arrange
            _viewModel.TargetId = "target-123";
            _viewModel.UserToken = "valid-token";
            _viewModel.IsConnecting = true;

            // Act
            var canExecute = _viewModel.ConnectCommand.CanExecute(null);

            // Assert
            Assert.False(canExecute);
        }

        [Fact]
        public async Task ConnectCommand_Execute_CallsTransportService()
        {
            // Arrange
            _viewModel.TargetId = "target-123";
            _viewModel.UserToken = "valid-token";

            _mockTransportService.Setup(x => x.ConnectAsync(It.IsAny<ConnectionRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _viewModel.ConnectCommand.ExecuteAsync(null);

            // Assert
            _mockTransportService.Verify(x => x.ConnectAsync(
                It.Is<ConnectionRequest>(r => r.TargetId == "target-123" && r.UserToken == "valid-token"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ConnectCommand_Execute_SavesUserToken()
        {
            // Arrange
            _viewModel.TargetId = "target-123";
            _viewModel.UserToken = "valid-token";

            _mockTransportService.Setup(x => x.ConnectAsync(It.IsAny<ConnectionRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _viewModel.ConnectCommand.ExecuteAsync(null);

            // Assert
            _mockUserSettingsService.Verify(x => x.SaveUserTokenAsync("valid-token"), Times.Once);
        }

        [Fact]
        public async Task ConnectCommand_Execute_AddsToRecentConnections()
        {
            // Arrange
            _viewModel.TargetId = "target-123";
            _viewModel.UserToken = "valid-token";

            _mockTransportService.Setup(x => x.ConnectAsync(It.IsAny<ConnectionRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _viewModel.ConnectCommand.ExecuteAsync(null);

            // Assert
            _mockUserSettingsService.Verify(x => x.AddRecentConnectionAsync("target-123"), Times.Once);
        }

        [Fact]
        public async Task ConnectCommand_Execute_SendsNavigationMessage()
        {
            // Arrange
            _viewModel.TargetId = "target-123";
            _viewModel.UserToken = "valid-token";

            _mockTransportService.Setup(x => x.ConnectAsync(It.IsAny<ConnectionRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _viewModel.ConnectCommand.ExecuteAsync(null);

            // Assert
            _mockMessenger.Verify(x => x.Send(It.Is<NavigationMessage>(m => m.ViewName == "Streaming")), Times.Once);
        }

        [Fact]
        public async Task ConnectCommand_Execute_HandlesException()
        {
            // Arrange
            _viewModel.TargetId = "target-123";
            _viewModel.UserToken = "valid-token";

            var expectedException = new InvalidOperationException("Connection failed");
            _mockTransportService.Setup(x => x.ConnectAsync(It.IsAny<ConnectionRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            // Act
            await _viewModel.ConnectCommand.ExecuteAsync(null);

            // Assert
            Assert.True(_viewModel.HasError);
            Assert.Equal("Connection failed", _viewModel.ConnectionError);
            Assert.Equal("Connection failed", _viewModel.StatusMessage);
            Assert.False(_viewModel.IsConnecting);
        }

        [Fact]
        public void TargetId_PropertyChanged_NotifiesCanExecuteChanged()
        {
            // Arrange
            var canExecuteChangedFired = false;
            _viewModel.ConnectCommand.CanExecuteChanged += (s, e) => canExecuteChangedFired = true;

            // Act
            _viewModel.TargetId = "new-target";

            // Assert
            Assert.True(canExecuteChangedFired);
        }

        [Fact]
        public void UserToken_PropertyChanged_NotifiesCanExecuteChanged()
        {
            // Arrange
            var canExecuteChangedFired = false;
            _viewModel.ConnectCommand.CanExecuteChanged += (s, e) => canExecuteChangedFired = true;

            // Act
            _viewModel.UserToken = "new-token";

            // Assert
            Assert.True(canExecuteChangedFired);
        }

        [Fact]
        public async Task LoadRecentConnections_PopulatesCollection()
        {
            // Arrange
            var recentConnections = new List<RecentConnection>
            {
                new() { TargetId = "target-1", DisplayName = "Computer 1", LastConnected = DateTime.UtcNow.AddDays(-1) },
                new() { TargetId = "target-2", DisplayName = "Computer 2", LastConnected = DateTime.UtcNow.AddDays(-2) }
            };

            _mockUserSettingsService.Setup(x => x.GetRecentConnectionsAsync())
                .ReturnsAsync(recentConnections);

            // Act
            var newViewModel = new ConnectViewModel(
                _mockTransportService.Object,
                _mockUserSettingsService.Object,
                _mockMessenger.Object);

            // Allow async loading to complete
            await Task.Delay(100);

            // Assert
            Assert.Equal(2, newViewModel.RecentConnections.Count);
            Assert.Equal("target-1", newViewModel.RecentConnections[0].TargetId);
            Assert.Equal("target-2", newViewModel.RecentConnections[1].TargetId);
        }
    }
}

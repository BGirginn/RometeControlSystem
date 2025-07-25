# Remote Control System - Frontend Architecture

## Executive Summary

This document outlines the complete frontend architecture for a Windows-to-Windows remote control system built with WPF/.NET 8 and MVVM pattern.

### Target Users & Use Cases

**Viewer Operator (Primary User)**
- **Goal**: Connect to and control remote computers with minimal latency
- **Critical Tasks**: 
  - Quick connection setup (Target ID + Token)
  - Real-time screen viewing with quality controls
  - Mouse/keyboard input with visual feedback
  - Session monitoring (FPS, latency, bitrate)
- **Constraints**: Must work on varying network conditions, security-first approach

**Agent Owner (Secondary User)**  
- **Goal**: Securely allow remote access while maintaining control
- **Critical Tasks**:
  - Accept/reject connection requests with caller identification
  - Monitor active sessions
  - Configure trusted users and auto-accept rules
- **Constraints**: Minimal performance impact, clear security indicators

**System Administrator (Tertiary User)**
- **Goal**: Monitor and manage remote access across organization
- **Critical Tasks**:
  - View active sessions and user activity
  - Manage user permissions and device inventory
  - Audit connection logs and security events
- **Constraints**: Web-based access, comprehensive reporting

---

## Information Architecture

### Viewer Application Flow
```
[Start] → [Token Setup] → [Connect Screen] → [Streaming Session] → [Disconnect]
    ↓         ↓              ↓                    ↓               ↓
[Settings] [History]    [Quality Control]  [Metrics Overlay]  [Session End]
```

### Agent Application Flow  
```
[System Tray] → [Incoming Request] → [Accept/Reject] → [Active Session] → [Tray]
      ↓              ↓                    ↓              ↓
  [Settings]    [Caller Info]        [Session Monitor] [Disconnect]
```

### State Machines

**Connection States:**
- `Disconnected` → `Resolving` → `Connecting` → `Authenticating` → `Connected` → `Streaming`
- Error transitions: Any state → `Error` → `Disconnected`
- Reconnection: `Streaming` → `Reconnecting` → `Connected`

---

## Screen Designs & Components

### Viewer Application Screens

#### 1. Connect Screen (`ConnectView.xaml`)
```
┌─────────────────────────────────────┐
│ Connect to Remote Computer          │
├─────────────────────────────────────┤
│ ┌─────────────────────────────────┐ │
│ │ Target ID: [________________]   │ │
│ │ Token:     [________________]   │ │  
│ │                    [Connect]    │ │
│ └─────────────────────────────────┘ │
│                                     │
│ Recent Connections:                 │
│ ┌─────────────────────────────────┐ │
│ │ ○ Computer-1  Last: 2 hrs ago   │ │
│ │ ○ Server-01   Last: 1 day ago   │ │
│ └─────────────────────────────────┘ │
└─────────────────────────────────────┘
```

**Components:**
- `TextBox` for Target ID (AutomationProperties.Name="Target ID")
- `PasswordBox` for User Token
- `Button` Connect (Primary style, IsDefault=true)
- `ListView` Recent connections with connect/remove actions
- Status indicator with connection progress

#### 2. Streaming Session Screen (`StreamingView.xaml`)
```
┌─────────────────────────────────────┐
│ [←] Settings [📊] [🖱] [⛶] Quality│
├─────────────────────────────────────┤
│                                     │
│        Remote Screen Display        │
│     ┌─────────────────────────┐     │
│     │                         │     │
│     │   [Remote Desktop]      │     │
│     │                         │     │
│     └─────────────────────────┘     │
│               ┌─────────────┐       │
│               │📊 Metrics   │       │
│               │FPS: 30      │       │
│               │RTT: 45ms    │       │
│               └─────────────┘       │
├─────────────────────────────────────┤
│ Quality: [████████░░] 80%  FPS: 30  │
└─────────────────────────────────────┘
```

**Components:**
- Top toolbar with disconnect, settings, metrics toggle, input toggle, fullscreen
- `Image` control for remote screen (WriteableBitmap binding)
- Metrics overlay (toggleable) showing FPS, RTT, bitrate, encode/decode times
- Quality/FPS sliders in bottom toolbar
- Fullscreen mode (F11) hides toolbars

### Agent Application Screens

#### 3. System Tray Interface
```
Right-click menu:
┌─────────────────────┐
│ ● Online            │
│ ─────────────────── │
│ 📊 0 Active Sessions│
│ ⚙️  Settings        │
│ ❌ Exit             │
└─────────────────────┘

Tooltip: "Remote Control Agent - Ready"
```

#### 4. Connection Request Dialog (`ConnectionRequestDialog.xaml`)  
```
┌─────────────────────────────────────┐
│ ⚠️  Incoming Connection Request     │
├─────────────────────────────────────┤
│ User:      johnsmith@company.com    │
│ Computer:  LAPTOP-ABC123           │
│ Time:      Jan 15, 2025 14:30      │
│ Target ID: DESK-001                │
│                                     │
│ ⚠️ Only accept from trusted users   │
│                                     │
│           [Reject]    [Accept]      │
│ ESC=Reject, Enter=Accept           │
└─────────────────────────────────────┘
```

**Security Features:**
- Auto-close after 30 seconds (auto-reject)
- Caller identification with computer name
- Visual warning about screen sharing
- Sound notification and window flashing

---

## Design System

### Color Palette & Tokens
```css
/* Light Theme */
--primary: #2D7D32 (Green)
--primary-dark: #1B5E20
--secondary: #FF6F00 (Orange)
--error: #D32F2F
--warning: #F57C00
--success: #388E3C

--surface: #FFFFFF
--background: #FAFAFA  
--card: #FFFFFF

--text-primary: #212121
--text-secondary: #757575
--text-disabled: #BDBDBD

/* Dark Theme */
--surface-dark: #1E1E1E
--background-dark: #121212
--card-dark: #2D2D2D
--text-primary-dark: #FFFFFF
--text-secondary-dark: #B3B3B3
```

### Typography Scale
- **Headline**: 24px, SemiBold (Page titles)
- **Title**: 20px, Medium (Section headers)  
- **Subtitle**: 16px, Medium (Card titles)
- **Body**: 14px, Regular (Main content)
- **Caption**: 12px, Regular (Secondary info)

### Spacing System
- **Small**: 4px (Icon margins)
- **Medium**: 8px (Component spacing)
- **Large**: 16px (Section spacing)
- **XLarge**: 24px (Page margins)

### Component Styles

**Button Styles:**
```xml
<!-- Primary Button -->
<Style x:Key="PrimaryButton" TargetType="Button">
    <Setter Property="Background" Value="{StaticResource PrimaryBrush}"/>
    <Setter Property="Foreground" Value="White"/>
    <Setter Property="Padding" Value="16,8"/>
    <Setter Property="CornerRadius" Value="4"/>
    <Setter Property="MinWidth" Value="88"/>
    <Setter Property="Effect" Value="{StaticResource Elevation1}"/>
</Style>

<!-- Secondary Button -->  
<Style x:Key="SecondaryButton" TargetType="Button" BasedOn="{StaticResource PrimaryButton}">
    <Setter Property="Background" Value="Transparent"/>
    <Setter Property="Foreground" Value="{StaticResource PrimaryBrush}"/>
    <Setter Property="BorderBrush" Value="{StaticResource BorderBrush}"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="Effect" Value="{x:Null}"/>
</Style>
```

### Accessibility Features
- **Keyboard Navigation**: Full tab order, focus indicators
- **Screen Reader**: AutomationProperties.Name/HelpText on all interactive elements
- **High Contrast**: Separate style variants for contrast themes
- **Minimum Touch Targets**: 44x44px minimum for touch interfaces
- **Focus Management**: Logical tab order, escape key handling

---

## MVVM Architecture

### ViewModels Structure

#### MainWindowViewModel
- **Responsibilities**: Navigation, theme management, global commands
- **Properties**: `CurrentView`, `WindowTitle`, `IsFullscreen`, `CurrentTheme`
- **Commands**: `ChangeThemeCommand`, `ToggleFullscreenCommand`, `ExitApplicationCommand`
- **Navigation**: Receives `NavigationMessage` via MVVM Messenger

#### ConnectViewModel  
- **Responsibilities**: Connection setup, recent connections, user token management
- **Properties**: `TargetId`, `UserToken`, `IsConnecting`, `StatusMessage`, `RecentConnections`
- **Commands**: `ConnectCommand`, `CancelConnectionCommand`, `ConnectToRecentCommand`
- **Async Operations**: Connection with CancellationToken support
- **Validation**: Real-time CanExecute for connect button

#### StreamingViewModel
- **Responsibilities**: Video display, input control, metrics, session management
- **Properties**: `FrameBuffer`, `IsConnected`, `Quality`, `FrameRate`, `ShowMetrics`
- **Commands**: `DisconnectCommand`, `ToggleFullscreenCommand`, `ToggleInputEnabledCommand`
- **Events**: Frame updates on UI thread via Dispatcher

### Service Contracts

#### ITransportService
```csharp
public interface ITransportService
{
    Task ConnectAsync(ConnectionRequest request, CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task<bool> IsConnectedAsync();
    
    event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
    event EventHandler<byte[]>? FrameDataReceived;
}
```

#### IVideoRenderer
```csharp
public interface IVideoRenderer
{
    Task<WriteableBitmap> RenderFrameAsync(byte[] frameData, CancellationToken cancellationToken = default);
    Task SetQualityAsync(int quality);
    Task SetFrameRateAsync(int fps);
    
    event EventHandler<MetricsEventArgs>? MetricsUpdated;
}
```

#### IUserSettingsService
```csharp
public interface IUserSettingsService
{
    Task<string> GetUserTokenAsync();
    Task SaveUserTokenAsync(string token);
    Task AddRecentConnectionAsync(string targetId);
    Task<IEnumerable<RecentConnection>> GetRecentConnectionsAsync();
    Task<string> GetThemeAsync();
    Task SetThemeAsync(string theme);
}
```

### Dependency Injection Setup
```csharp
// App.xaml.cs - ConfigureServices
services.AddSingleton<IMessenger, WeakReferenceMessenger>();
services.AddSingleton<ITransportService, TcpTransportService>();
services.AddSingleton<IUserSettingsService, FileUserSettingsService>();
services.AddSingleton<IVideoRenderer, WpfVideoRenderer>();
services.AddSingleton<IThemeService, WpfThemeService>();

services.AddTransient<MainWindowViewModel>();
services.AddTransient<ConnectViewModel>();
services.AddTransient<StreamingViewModel>();
```

---

## Performance & Rendering

### Frame Rendering Pipeline
```
Network Thread → Frame Buffer → UI Thread → WriteableBitmap → Image Control
     ↓              ↓              ↓             ↓             ↓
  Receive        Decode        Dispatch      Render        Display
```

**Optimization Strategies:**
- **Background Decoding**: Frame processing off UI thread
- **Frame Dropping**: Skip frames when UI thread busy
- **Adaptive Quality**: Auto-adjust based on network conditions
- **Memory Pooling**: Reuse bitmap buffers to reduce GC pressure

### Metrics Collection
```csharp
public class MetricsEventArgs : EventArgs
{
    public int CurrentFps { get; set; }           // Actual FPS achieved
    public int RoundTripTime { get; set; }        // Network latency (ms)
    public double BitrateMbps { get; set; }       // Current bitrate
    public int QueueLength { get; set; }          // Frame queue depth
    public double EncodeTimeMs { get; set; }      // Remote encode time
    public double DecodeTimeMs { get; set; }      // Local decode time
    public DateTime Timestamp { get; set; }
}
```

**Metrics Display:**
- Real-time overlay with 1-second updates
- Historical graphs (optional)
- Performance warnings when thresholds exceeded
- Export capability for troubleshooting

---

## Security & User Experience

### Security UI Elements
- **Caller Identification**: Show requesting user's identity and computer name
- **Permission Indicators**: Clear visual state for when input control is enabled
- **Session Monitoring**: Active session count and duration
- **Auto-Disconnect**: Configurable idle timeout with warning
- **Access Logging**: Local audit trail of connections

### Error Handling & Status Management
```csharp
public enum ConnectionState
{
    Disconnected,    // Ready to connect
    Resolving,       // Looking up target
    Connecting,      // TCP connection
    Authenticating,  // User verification  
    Connected,       // Authenticated
    Streaming,       // Active session
    Error,           // Connection failed
    Reconnecting,    // Auto-retry
    Disconnecting    // Closing session
}
```

**Error Dictionary:**
- Network errors → User-friendly messages
- Authentication failures → Clear next steps
- Timeout scenarios → Retry options
- Service unavailable → Alternative suggestions

---

## Testing Strategy

### Unit Tests (xUnit + Moq)
```csharp
[Fact]
public async Task ConnectCommand_Execute_CallsTransportService()
{
    // Arrange
    var mockTransport = new Mock<ITransportService>();
    var viewModel = new ConnectViewModel(mockTransport.Object, ...);
    viewModel.TargetId = "test-target";
    viewModel.UserToken = "test-token";

    // Act  
    await viewModel.ConnectCommand.ExecuteAsync(null);

    // Assert
    mockTransport.Verify(x => x.ConnectAsync(
        It.Is<ConnectionRequest>(r => r.TargetId == "test-target"),
        It.IsAny<CancellationToken>()), Times.Once);
}
```

### UI Automation Tests (Playwright for .NET)
```csharp
[Test]
public async Task CanConnectToRemoteComputer()
{
    // Launch application
    await using var app = await Page.LaunchAppAsync("RemoteControl.Viewer.exe");
    
    // Fill connection form
    await app.Locator("[data-testid=target-id]").FillAsync("test-computer");
    await app.Locator("[data-testid=user-token]").FillAsync("test-token");
    
    // Click connect
    await app.Locator("[data-testid=connect-button]").ClickAsync();
    
    // Verify streaming view appears
    await app.Locator("[data-testid=streaming-view]").WaitForAsync();
    
    // Verify video display
    var videoElement = app.Locator("[data-testid=remote-screen]");
    await Expect(videoElement).ToBeVisibleAsync();
}
```

### Accessibility Testing
- **Keyboard-only navigation**: Tab through all interactive elements
- **Screen reader compatibility**: Test with NVDA/JAWS
- **High contrast themes**: Verify visibility in contrast modes
- **Focus indicators**: Ensure visible focus rings
- **Minimum target sizes**: 44x44px touch targets

---

## Internationalization (i18n)

### Resource Structure
```
Resources/
├── Strings.resx              # English (default)
├── Strings.tr.resx           # Turkish
├── Strings.es.resx           # Spanish
└── Strings.fr.resx           # French
```

### XAML Binding Example
```xml
<TextBlock Text="{x:Static properties:Strings.Connect_TargetId_Label}"/>
<Button Content="{x:Static properties:Strings.Common_Connect}" 
        Command="{Binding ConnectCommand}"/>
```

### Resource Keys Convention
```
// Format: [Screen]_[Element]_[Type]
Connect_TargetId_Label = "Target ID:"
Connect_UserToken_Label = "User Token:"
Common_Connect = "Connect"
Common_Cancel = "Cancel"
Streaming_Quality_Label = "Quality:"
Error_ConnectionFailed = "Connection failed: {0}"
```

### Runtime Language Switching
```csharp
public async Task ChangeLanguageAsync(string cultureCode)
{
    var culture = new CultureInfo(cultureCode);
    Thread.CurrentThread.CurrentCulture = culture;
    Thread.CurrentThread.CurrentUICulture = culture;
    
    // Reload resource dictionaries
    Application.Current.Resources.MergedDictionaries.Clear();
    // ... reload with new culture
    
    await _userSettingsService.SetLanguageAsync(cultureCode);
}
```

---

## Deployment & Packaging

### Viewer Application Packaging
**Recommended: MSIX Package**
- **Advantages**: Auto-updates, Store distribution, sandboxing, clean uninstall
- **Dependencies**: .NET 8 Runtime included
- **Update Strategy**: Background updates with user notification

**Alternative: Squirrel.Windows**
- **Advantages**: More control over updates, easier CI/CD integration
- **Update Process**: Check for updates on startup, download in background

### Agent Application Packaging  
**Windows Service + Tray Application**
- **Service**: Runs as LocalSystem for screen capture access
- **Tray UI**: Runs in user session for interaction
- **Communication**: Named pipes between service and tray app
- **Installation**: MSI installer with administrator privileges

### Configuration Management
```
%APPDATA%/RemoteControlViewer/
├── settings.json          # User preferences
├── recent-connections.json # Connection history  
├── logs/                  # Application logs
│   ├── viewer-20250125.txt
│   └── errors.txt
└── cache/                 # Temporary files
    └── thumbnails/
```

---

## Development Roadmap

### Phase 1 (30 days) - MVP
- ✅ Basic connection UI with mock transport
- ✅ MVVM architecture with dependency injection
- ✅ Theme system (Light/Dark)
- ✅ Basic metrics display
- ✅ Unit tests for ViewModels
- 🔄 Agent tray UI with connection dialog
- 🔄 Real TCP transport implementation
- 🔄 Basic internationalization (EN/TR)

### Phase 2 (60 days) - Enhanced Features
- 🔲 H.264 video codec integration
- 🔲 GPU-accelerated rendering (DirectX)
- 🔲 Multi-monitor support
- 🔲 File transfer capability
- 🔲 Clipboard synchronization
- 🔲 Audio streaming (optional)
- 🔲 UI automation tests with Playwright
- 🔲 Performance profiling and optimization

### Phase 3 (90 days) - Enterprise Features
- 🔲 Web admin panel (ASP.NET Core + Blazor)
- 🔲 Active Directory integration
- 🔲 Group Policy support
- 🔲 Detailed audit logging
- 🔲 Session recording/playback
- 🔲 Advanced security (certificate-based auth)
- 🔲 Accessibility certification (WCAG 2.1 AA)
- 🔲 Complete documentation and training materials

---

## Acceptance Criteria & Definition of Done

### Connect Screen
**AC1**: User can enter Target ID and Token to initiate connection
**AC2**: Recent connections list shows last 10 connections with quick connect
**AC3**: Connection status shows progress through all states with appropriate messaging
**AC4**: Form validation prevents empty fields and shows clear error messages

### Streaming Screen  
**AC1**: Remote screen displays in real-time with < 100ms latency on local network
**AC2**: Quality and FPS controls adjust stream parameters within 2 seconds
**AC3**: Metrics overlay shows current performance data updated every second
**AC4**: Fullscreen mode hides all UI chrome and supports F11/ESC toggle
**AC5**: Input control toggle works immediately with visual feedback

### Agent Tray
**AC1**: Connection requests show caller identification with 30-second timeout
**AC2**: System tray shows online status and active session count
**AC3**: Settings allow configuration of auto-accept for trusted users
**AC4**: All security dialogs are modal and require explicit user action

### General DoD
- ✅ All code compiles without warnings
- ✅ Unit tests achieve >80% code coverage
- ✅ All UI elements have AutomationProperties for accessibility
- ✅ Application starts in <3 seconds on target hardware
- ✅ Memory usage remains <200MB during normal operation
- ✅ Themes apply correctly without restart
- ✅ All text is externalized for internationalization
- ✅ Error handling covers network disconnection scenarios
- ✅ Application logs include sufficient detail for troubleshooting
- ✅ Installation/uninstallation works cleanly on Windows 10/11

---

## Technical Implementation Notes

### Key Design Decisions

1. **MVVM with CommunityToolkit**: Chosen for robust property change notifications, async command support, and source generation features

2. **WeakReferenceMessenger**: Selected over EventAggregator for memory-safe cross-ViewModel communication

3. **WriteableBitmap**: Direct pixel manipulation for optimal frame rendering performance over WPF Image controls

4. **Dependency Injection**: Microsoft.Extensions.DependencyInjection for testability and service lifetime management

5. **Serilog**: Structured logging with file rotation for production debugging

6. **TCP Sockets**: Custom protocol over TCP for reliable frame delivery with minimal overhead

### Performance Considerations

- **Frame Buffer Pool**: Reuse bitmap objects to minimize GC pressure
- **Background Threads**: All network I/O and frame processing off UI thread
- **Adaptive Quality**: Automatically reduce quality on high latency/low bandwidth
- **Memory Management**: Explicit disposal of large objects and event unsubscription

### Security Architecture

- **Token-based Authentication**: User tokens with expiration for session management
- **Local Settings Encryption**: Sensitive data encrypted using Windows DPAPI
- **Network Encryption**: TLS 1.3 for all network communication
- **Audit Trail**: Comprehensive logging of all connection attempts and activities

This architecture provides a solid foundation for a production-ready remote control system with excellent user experience, security, and maintainability.

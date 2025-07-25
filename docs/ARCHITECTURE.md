# Remote Control System - Architecture Documentation

## Overview

This is a modern remote control system built with .NET 8, featuring WebSocket-based real-time communication, JWT authentication, and H.264 video encoding. The system enables secure remote desktop access between Windows computers with explicit user consent and <200ms latency.

## System Architecture

### High-Level Architecture

```
┌─────────────┐    WebSocket/SignalR    ┌──────────────────┐    WebSocket/SignalR    ┌─────────────┐
│   Viewer    │◄─────────────────────────►│  ControlServer   │◄─────────────────────────►│    Agent    │
│ (WPF App)   │                          │ (ASP.NET Core)   │                          │ (WPF App)   │
└─────────────┘                          └──────────────────┘                          └─────────────┘
                                                   │
                                                   ▼
                                          ┌──────────────────┐
                                          │     Admin        │
                                          │ (Razor Pages)    │
                                          └──────────────────┘
```

### Core Components

#### 1. **RemoteControl.Protocol** 📨
- **Purpose**: Defines the communication protocol between components
- **Key Features**:
  - JSON-based message schema with polymorphic serialization
  - Type-safe message definitions for all communication
  - Efficient binary serialization for frame data
- **Message Types**:
  - `RegisterAgent` / `AgentRegistered`
  - `RequestSession` / `SessionDecision`
  - `Frame` / `InputEvent` / `KeepAlive` / `Error`

#### 2. **RemoteControl.Transport** 🚀
- **Purpose**: WebSocket communication layer with JWT authentication
- **Key Features**:
  - SignalR client with automatic reconnection
  - JWT token generation and validation
  - Configurable connection parameters
  - Error handling and retry logic

#### 3. **RemoteControl.ControlServer** 🏢
- **Purpose**: Central coordination server for session management
- **Key Features**:
  - SignalR hub for real-time communication
  - Agent registration and discovery
  - Session request routing and approval
  - JWT-based authentication
  - Health monitoring and metrics

#### 4. **RemoteControl.Agent** 🖥️
- **Purpose**: Service running on computers to be controlled
- **Key Features**:
  - Desktop Duplication API for screen capture
  - H.264 encoding using MediaFoundation
  - Windows input simulation
  - User consent dialogs for security
  - Automatic registration with server

#### 5. **RemoteControl.Viewer** 👁️
- **Purpose**: Client application for controlling remote computers
- **Key Features**:
  - Modern WPF UI with MVVM pattern
  - H.264 decoding and rendering
  - Real-time input capture and transmission
  - Session management and metrics display

#### 6. **RemoteControl.Admin** 🔧
- **Purpose**: Web-based administration interface
- **Key Features**:
  - Active session monitoring
  - User and device management
  - System metrics and health status

## Technical Implementation

### Communication Protocol

#### 1. **Agent Registration**
```json
{
  "type": "RegisterAgent",
  "payload": {
    "username": "john",
    "machineName": "DESKTOP-123",
    "capabilities": { "h264": true, "maxFps": 60 }
  }
}
```

#### 2. **Session Request Flow**
```
Viewer → RequestSession → ControlServer → Agent
                         ↓
Agent → SessionDecision → ControlServer → Viewer
                         ↓
        SessionStarted (if accepted)
```

#### 3. **Real-time Communication**
```
Agent: Frame → ControlServer → Viewer
Viewer: InputEvent → ControlServer → Agent
Both: KeepAlive ↔ ControlServer
```

### Authentication & Security

#### JWT Token Flow
```
1. User Login → ControlServer generates JWT
2. JWT included in WebSocket connection
3. Token validated on every SignalR hub call
4. Automatic token refresh before expiration
```

#### User Consent Model
```
1. Viewer requests session with Agent
2. Agent shows popup: "User X wants to control your screen"
3. Agent user clicks Accept/Reject
4. Session starts only after explicit approval
```

### Performance Optimizations

#### Screen Capture & Encoding
- **Desktop Duplication API**: Efficient screen capture (Windows 8+)
- **H.264 Hardware Encoding**: GPU acceleration when available
- **Adaptive Quality**: Automatic adjustment based on network conditions
- **Frame Skipping**: Drop frames when network is congested

#### Network Optimizations
- **WebSocket Compression**: Automatic message compression
- **Binary Frame Data**: Efficient transfer of video frames
- **Connection Pooling**: Reuse connections for multiple sessions
- **Automatic Reconnection**: Seamless recovery from network issues

## Configuration

### Transport Settings
```json
{
  "Transport": {
    "ConnectionTimeoutMs": 30000,
    "KeepAliveIntervalMs": 30000,
    "MaxMessageSize": 10485760,
    "Jwt": {
      "SigningKey": "YourSecretKey",
      "ExpirationMinutes": 480
    }
  }
}
```

### Agent Configuration
```json
{
  "Agent": {
    "ServerUrl": "https://controlserver:5001/hub",
    "ScreenCapture": {
      "DefaultFps": 30,
      "MaxFps": 60,
      "Quality": 80
    },
    "AutoRegister": true
  }
}
```

## Deployment Architecture

### Development Environment
```
Local Machine:
├── ControlServer:5001 (HTTPS)
├── Admin:5002 (HTTP)
├── Agent (background service)
└── Viewer (desktop app)
```

### Production Environment
```
Internet
    │
    ▼
┌─────────────┐
│ Load Balancer│
│   (nginx)   │
└─────────────┘
    │
    ▼
┌─────────────┐    ┌─────────────┐
│ControlServer│    │ControlServer│
│  Instance 1 │    │  Instance 2 │
└─────────────┘    └─────────────┘
    │                      │
    └──────────┬───────────┘
               ▼
    ┌─────────────────┐
    │   Redis Cache   │
    │ (Session Store) │
    └─────────────────┘
```

## API Endpoints

### Authentication API
```
POST /api/auth/token
POST /api/auth/register
```

### Management API
```
GET /api/agents           # List online agents
GET /api/sessions         # List active sessions
GET /health              # Health check
GET /metrics             # System metrics
```

### SignalR Hub Methods
```
RegisterAgent(message)    # Agent registration
RequestSession(message)   # Session request
SessionDecision(message)  # Accept/reject session
SendMessage(message)      # Generic message
```

## Monitoring & Observability

### Health Checks
- **Application Health**: `/health/live`
- **Readiness Check**: `/health/ready`
- **Dependencies**: Database, Redis, External APIs

### Metrics Collection
- **Connection Metrics**: Active connections, reconnections
- **Session Metrics**: Duration, frame rates, quality
- **Performance Metrics**: CPU usage, memory usage, latency

### Logging Strategy
- **Structured Logging**: JSON format with correlation IDs
- **Log Levels**: Trace → Debug → Info → Warning → Error → Critical
- **Log Aggregation**: Centralized logging with ELK stack

## Security Considerations

### Authentication
- **JWT Tokens**: Short-lived tokens with refresh mechanism
- **Strong Passwords**: Minimum 6 characters (configurable)
- **Rate Limiting**: Prevent brute force attacks

### Authorization
- **User Consent**: Explicit approval required for all sessions
- **Session Isolation**: Each session uses unique identifiers
- **Connection Validation**: Continuous token validation

### Data Protection
- **TLS Encryption**: All communication over HTTPS/WSS
- **Frame Encryption**: Additional encryption for screen data
- **No Data Persistence**: No screen data stored on server

### Network Security
- **CORS Policy**: Restricted cross-origin requests
- **Firewall Rules**: Limited port access
- **VPN Support**: Works through corporate VPNs

## Performance Targets

### Latency Requirements
- **End-to-End Latency**: <200ms (720p @ 30fps)
- **Input Response**: <50ms (local network)
- **Frame Delivery**: <100ms (encoding + network + decoding)

### Throughput Targets
- **Video Bitrate**: 1-5 Mbps (adaptive)
- **Concurrent Sessions**: 100+ per server instance
- **Frame Rate**: 15-60 fps (adaptive)

### Resource Usage
- **Agent CPU**: <10% (idle), <25% (active session)
- **Agent Memory**: <100MB (idle), <200MB (active)
- **Network Bandwidth**: 1-5 Mbps per session

## Development Guidelines

### Code Organization
```
src/
├── RemoteControl.Core/         # Shared models
├── RemoteControl.Protocol/     # Message definitions
├── RemoteControl.Transport/    # Communication layer
├── RemoteControl.ControlServer/# Central server
├── RemoteControl.Agent/        # Target computer app
├── RemoteControl.Viewer/       # Controller app
└── RemoteControl.Admin/        # Web admin interface
```

### Testing Strategy
- **Unit Tests**: >80% coverage for business logic
- **Integration Tests**: End-to-end session scenarios
- **Performance Tests**: Latency and throughput validation
- **Security Tests**: Authentication and authorization

### CI/CD Pipeline
```
GitHub Actions:
├── Build & Test (on PR)
├── Security Scan (SAST)
├── Package & Sign (on main)
├── Deploy to Staging
└── Deploy to Production (manual approval)
```

This architecture provides a solid foundation for a production-ready remote control system with excellent performance, security, and maintainability. 
# Remote Control System - Protocol Documentation

## Overview

This document describes the communication protocol for the Remote Control System, built on top of SignalR WebSockets with JSON messaging and binary frame data transmission.

## Protocol Stack

```
Application Layer    │ Remote Control Messages (JSON)
                    │ Frame Data (Binary H.264/JPEG)
                    ├─────────────────────────────
Transport Layer     │ SignalR over WebSockets
                    │ JWT Authentication
                    ├─────────────────────────────
Security Layer      │ TLS 1.3 Encryption
                    │ Token-based Authorization
                    ├─────────────────────────────
Network Layer       │ TCP/IP
```

## Message Format

All messages follow a standard JSON schema with polymorphic type discrimination:

```json
{
  "type": "MessageType",
  "timestamp": "2025-01-25T10:30:00.000Z",
  "messageId": "optional-correlation-id",
  "payload": {
    // Message-specific data
  }
}
```

## Connection Flow

### 1. Authentication & Registration

```
Client → Server: JWT Token (via query parameter or header)
Server → Client: Connection acknowledged
```

### 2. Agent Registration

```json
{
  "type": "RegisterAgent",
  "payload": {
    "username": "john#4A2B", 
    "machineName": "DESKTOP-WORK01",
    "operatingSystem": "Windows 11 Pro",
    "screenWidth": 1920,
    "screenHeight": 1080,
    "capabilities": {
      "h264": true,
      "maxFps": 60,
      "supportedEncodings": ["h264", "jpeg"],
      "hasAudio": false
    }
  }
}
```

**Response:**
```json
{
  "type": "AgentRegistered", 
  "payload": {
    "agentId": "AGENT_UUID",
    "assignedId": "john#4A2B-WORK01",
    "serverSettings": {
      "maxFrameSize": 10485760,
      "keepAliveInterval": 30000
    }
  }
}
```

### 3. Session Request Flow

#### Viewer initiates connection:
```json
{
  "type": "RequestSession",
  "payload": {
    "targetAgentId": "john#4A2B-WORK01",
    "viewerUsername": "jane#5C3D",
    "requestedCapabilities": {
      "videoQuality": "high",
      "frameRate": 30,
      "encoding": "h264"
    }
  }
}
```

#### Server forwards to Agent:
```json
{
  "type": "SessionRequestReceived",
  "payload": {
    "sessionId": "SESSION_UUID",
    "viewerUsername": "jane#5C3D", 
    "viewerDisplayName": "Jane Smith",
    "requestTime": "2025-01-25T10:30:00.000Z",
    "autoAcceptTimeout": 30000
  }
}
```

#### Agent responds with decision:
```json
{
  "type": "SessionDecision",
  "payload": {
    "sessionId": "SESSION_UUID",
    "accepted": true,
    "reason": null,
    "settings": {
      "frameRate": 30,
      "quality": 80,
      "encoding": "h264"
    }
  }
}
```

#### Server notifies Viewer:
```json
{
  "type": "SessionStarted",
  "payload": {
    "sessionId": "SESSION_UUID", 
    "agentCapabilities": {
      "screenWidth": 1920,
      "screenHeight": 1080,
      "encoding": "h264",
      "frameRate": 30
    }
  }
}
```

## Real-time Communication

### Frame Streaming

Video frames are sent as binary data through SignalR with metadata:

```json
{
  "type": "Frame",
  "payload": {
    "sessionId": "SESSION_UUID",
    "frameNumber": 12345,
    "timestamp": 1674654600000,
    "encoding": "h264",
    "width": 1920,
    "height": 1080,
    "dataSize": 87432,
    "isKeyFrame": false
  }
}
```

The actual frame data is sent immediately after as binary data.

### Input Events

Input events are batched and sent as JSON:

```json
{
  "type": "InputEvent",
  "payload": {
    "sessionId": "SESSION_UUID",
    "events": [
      {
        "type": "MouseMove",
        "x": 1234,
        "y": 567,
        "timestamp": 1674654600001
      },
      {
        "type": "MouseDown", 
        "button": "Left",
        "x": 1234,
        "y": 567,
        "timestamp": 1674654600002
      },
      {
        "type": "KeyDown",
        "virtualKey": 65,
        "scanCode": 30,
        "flags": 0,
        "timestamp": 1674654600003
      }
    ]
  }
}
```

### Keep Alive & Health

```json
{
  "type": "KeepAlive",
  "payload": {
    "sessionId": "SESSION_UUID",
    "metrics": {
      "framesSent": 12345,
      "framesDropped": 23,
      "averageLatency": 45,
      "cpuUsage": 15.5,
      "memoryUsage": 234567890
    }
  }
}
```

## Error Handling

```json
{
  "type": "Error",
  "payload": {
    "errorCode": "SCREEN_CAPTURE_FAILED",
    "message": "Failed to initialize Desktop Duplication API",
    "details": "DXGI_ERROR_UNSUPPORTED",
    "sessionId": "SESSION_UUID",
    "isRecoverable": false
  }
}
```

Common error codes:
- `AUTHENTICATION_FAILED` - Invalid JWT token
- `AGENT_NOT_FOUND` - Target agent is offline
- `SESSION_REJECTED` - User declined the session
- `SCREEN_CAPTURE_FAILED` - Desktop capture initialization failed
- `ENCODING_ERROR` - H.264 encoder error
- `NETWORK_ERROR` - Connection issues
- `PERMISSION_DENIED` - Insufficient privileges

## Session Management

### Session Termination

```json
{
  "type": "SessionEnded",
  "payload": {
    "sessionId": "SESSION_UUID",
    "reason": "USER_DISCONNECTED",
    "duration": 300000,
    "metrics": {
      "totalFrames": 9000,
      "averageFps": 30,
      "averageLatency": 42,
      "dataTransferred": 45678901
    }
  }
}
```

## Performance Requirements

- **Frame Latency**: <200ms end-to-end (capture → encode → transmit → decode → render)
- **Frame Rate**: 15-60 FPS (configurable)
- **Resolution**: Up to 4K (3840x2160) 
- **Encoding**: H.264 preferred, JPEG fallback
- **Max Message Size**: 10MB per SignalR message
- **Keep Alive**: 30 second intervals

## Security Considerations

1. **Authentication**: JWT tokens with configurable expiration
2. **Authorization**: Per-session permission validation
3. **Encryption**: TLS 1.3 for all communication
4. **User Consent**: Explicit approval required for all sessions
5. **Rate Limiting**: Configurable limits on connection attempts
6. **Audit Trail**: All connection attempts and sessions logged

## Protocol Extensions

### Multi-Monitor Support
```json
{
  "type": "SelectMonitor",
  "payload": {
    "sessionId": "SESSION_UUID",
    "monitorIndex": 1,
    "bounds": {
      "left": 1920,
      "top": 0, 
      "width": 1920,
      "height": 1080
    }
  }
}
```

### Quality Control
```json
{
  "type": "UpdateQuality",
  "payload": {
    "sessionId": "SESSION_UUID", 
    "frameRate": 15,
    "quality": 60,
    "adaptive": true
  }
}
```

This protocol provides a robust foundation for high-performance, secure remote desktop control with explicit user consent and sub-200ms latency.

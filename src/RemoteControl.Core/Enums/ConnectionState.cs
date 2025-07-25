namespace RemoteControl.Core.Enums
{
    public enum ConnectionState
    {
        Disconnected,
        Resolving,
        Connecting,
        Authenticating,
        Connected,
        Streaming,
        Error,
        Reconnecting,
        Disconnecting
    }

    public enum ConnectionEvent
    {
        Connect,
        Resolved,
        Connected,
        Authenticated,
        StreamStarted,
        Failed,
        Disconnect,
        Disconnected,
        Retry
    }
}
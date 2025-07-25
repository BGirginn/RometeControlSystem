using System;

namespace RemoteControl.Core.Models
{
    public class SessionInfo
    {
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        public string ViewerId { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
        public SessionStatus Status { get; set; } = SessionStatus.Initializing;
        public string ViewerEndpoint { get; set; } = string.Empty;
        public string AgentEndpoint { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public long BytesTransferred { get; set; } = 0;
        public int FramesSent { get; set; } = 0;
        public TimeSpan Duration => EndTime?.Subtract(StartTime) ?? DateTime.UtcNow.Subtract(StartTime);
    }

    public enum SessionStatus
    {
        Initializing,
        Connecting,
        Connected,
        Active,
        Disconnecting,
        Disconnected,
        Error
    }
}

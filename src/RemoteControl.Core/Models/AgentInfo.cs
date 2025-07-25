using System;

namespace RemoteControl.Core.Models
{
    public class AgentInfo
    {
        public string AgentId { get; set; } = string.Empty;
        public string ComputerName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string IPAddress { get; set; } = string.Empty;
        public string OperatingSystem { get; set; } = string.Empty;
        public int ScreenWidth { get; set; }
        public int ScreenHeight { get; set; }
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
        public bool IsOnline { get; set; } = true;
        public string Version { get; set; } = "1.0.0";
    }
}

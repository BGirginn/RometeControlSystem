using System;

namespace RemoteControl.Core.Models
{
    public class RecentConnection
    {
        public string TargetId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public DateTime LastConnected { get; set; }
        public TimeSpan Duration { get; set; }
        public bool IsFavorite { get; set; }
    }
}
namespace RemoteControl.Core.Models
{
    public class ConnectionRequest
    {
        public string TargetId { get; set; } = string.Empty;
        public string UserToken { get; set; } = string.Empty;
        public string UserIdentity { get; set; } = string.Empty;
        public string UserLocation { get; set; } = string.Empty;
        public DateTime RequestTime { get; set; } = DateTime.UtcNow;
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
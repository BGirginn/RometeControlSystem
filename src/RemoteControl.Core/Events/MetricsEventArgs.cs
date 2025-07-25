namespace RemoteControl.Core.Events
{
    public class MetricsEventArgs : EventArgs
    {
        public int CurrentFps { get; set; }
        public int RoundTripTime { get; set; }
        public double BitrateMbps { get; set; }
        public int QueueLength { get; set; }
        public double EncodeTimeMs { get; set; }
        public double DecodeTimeMs { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
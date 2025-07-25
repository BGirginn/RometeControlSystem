using System;

namespace RemoteControl.Core.Models
{
    public class ScreenFrame
    {
        public byte[] ImageData { get; set; } = Array.Empty<byte>();
        public int Width { get; set; }
        public int Height { get; set; }
        public int Quality { get; set; } = 75;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public long FrameNumber { get; set; }
        public bool IsKeyFrame { get; set; }
        public string Format { get; set; } = "JPEG"; // JPEG, PNG, etc.
    }
}

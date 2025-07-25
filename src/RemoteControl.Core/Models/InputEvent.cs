using System;

namespace RemoteControl.Core.Models
{
    public class InputEvent
    {
        public InputEventType Type { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Button { get; set; } // Mouse button or key code
        public bool IsPressed { get; set; }
        public string? Text { get; set; } // For text input
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public enum InputEventType
    {
        MouseMove,
        MouseClick,
        MouseWheel,
        KeyPress,
        KeyRelease,
        TextInput
    }
}

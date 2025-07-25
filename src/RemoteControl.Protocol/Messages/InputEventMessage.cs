using System.Text.Json.Serialization;

namespace RemoteControl.Protocol.Messages;

/// <summary>
/// Message containing input events from viewer
/// </summary>
public class InputEventMessage : MessageBase
{
    public override string Type => "InputEvent";

    [JsonPropertyName("payload")]
    public InputEventPayload Payload { get; set; } = new();
}

public class InputEventPayload
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("events")]
    public InputEvent[] Events { get; set; } = Array.Empty<InputEvent>();
}

public class InputEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }

    [JsonPropertyName("button")]
    public string? Button { get; set; }

    [JsonPropertyName("virtualKey")]
    public int VirtualKey { get; set; }

    [JsonPropertyName("scanCode")]
    public int ScanCode { get; set; }

    [JsonPropertyName("flags")]
    public int Flags { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("delta")]
    public int Delta { get; set; }

    [JsonPropertyName("modifiers")]
    public string[] Modifiers { get; set; } = Array.Empty<string>();

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    /// <summary>
    /// For extended Win32 API support
    /// </summary>
    [JsonPropertyName("unicode")]
    public char? Unicode { get; set; }

    [JsonPropertyName("isExtended")]
    public bool IsExtended { get; set; }

    [JsonPropertyName("isSystemKey")]
    public bool IsSystemKey { get; set; }
}

/// <summary>
/// Input event types matching Win32 API constants
/// </summary>
public static class InputEventType
{
    public const string MouseMove = "MouseMove";
    public const string MouseDown = "MouseDown";
    public const string MouseUp = "MouseUp";
    public const string MouseWheel = "MouseWheel";
    public const string MouseHWheel = "MouseHWheel";
    public const string KeyDown = "KeyDown";
    public const string KeyUp = "KeyUp";
    public const string SysKeyDown = "SysKeyDown";
    public const string SysKeyUp = "SysKeyUp";
    public const string Char = "Char";
    public const string UniChar = "UniChar";
}

/// <summary>
/// Mouse button identifiers
/// </summary>
public static class MouseButton
{
    public const string Left = "Left";
    public const string Right = "Right";
    public const string Middle = "Middle";
    public const string X1 = "X1";
    public const string X2 = "X2";
}

/// <summary>
/// Keyboard modifier keys
/// </summary>
public static class KeyModifier
{
    public const string Ctrl = "Ctrl";
    public const string Alt = "Alt";
    public const string Shift = "Shift";
    public const string Windows = "Windows";
    public const string LeftCtrl = "LeftCtrl";
    public const string RightCtrl = "RightCtrl";
    public const string LeftAlt = "LeftAlt";
    public const string RightAlt = "RightAlt";
    public const string LeftShift = "LeftShift";
    public const string RightShift = "RightShift";
} 
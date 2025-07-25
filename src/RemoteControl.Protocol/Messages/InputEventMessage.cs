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
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }

    [JsonPropertyName("button")]
    public string? Button { get; set; }

    [JsonPropertyName("vk")]
    public int VirtualKey { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("delta")]
    public int Delta { get; set; }

    [JsonPropertyName("modifiers")]
    public string[] Modifiers { get; set; } = Array.Empty<string>();

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Input event kinds
/// </summary>
public static class InputEventKind
{
    public const string MouseMove = "MouseMove";
    public const string MouseDown = "MouseDown";
    public const string MouseUp = "MouseUp";
    public const string MouseWheel = "MouseWheel";
    public const string KeyDown = "KeyDown";
    public const string KeyUp = "KeyUp";
    public const string TextInput = "TextInput";
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
} 
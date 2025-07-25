using System.Text.Json;
using System.Text.Json.Serialization;
using RemoteControl.Protocol.Messages;

namespace RemoteControl.Protocol.Serialization;

/// <summary>
/// Handles serialization and deserialization of protocol messages
/// </summary>
public static class MessageSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = false
    };

    /// <summary>
    /// Serialize a message to JSON string
    /// </summary>
    public static string Serialize<T>(T message) where T : MessageBase
    {
        return JsonSerializer.Serialize(message, SerializerOptions);
    }

    /// <summary>
    /// Serialize a message to JSON bytes
    /// </summary>
    public static byte[] SerializeToBytes<T>(T message) where T : MessageBase
    {
        return JsonSerializer.SerializeToUtf8Bytes(message, SerializerOptions);
    }

    /// <summary>
    /// Deserialize a JSON string to message
    /// </summary>
    public static MessageBase? Deserialize(string json)
    {
        return JsonSerializer.Deserialize<MessageBase>(json, SerializerOptions);
    }

    /// <summary>
    /// Deserialize JSON bytes to message
    /// </summary>
    public static MessageBase? Deserialize(byte[] jsonBytes)
    {
        return JsonSerializer.Deserialize<MessageBase>(jsonBytes, SerializerOptions);
    }

    /// <summary>
    /// Deserialize a JSON string to specific message type
    /// </summary>
    public static T? Deserialize<T>(string json) where T : MessageBase
    {
        return JsonSerializer.Deserialize<T>(json, SerializerOptions);
    }

    /// <summary>
    /// Try to deserialize a JSON string, returning null on failure
    /// </summary>
    public static bool TryDeserialize(string json, out MessageBase? message)
    {
        try
        {
            message = Deserialize(json);
            return message != null;
        }
        catch
        {
            message = null;
            return false;
        }
    }

    /// <summary>
    /// Try to deserialize JSON bytes, returning null on failure
    /// </summary>
    public static bool TryDeserialize(byte[] jsonBytes, out MessageBase? message)
    {
        try
        {
            message = Deserialize(jsonBytes);
            return message != null;
        }
        catch
        {
            message = null;
            return false;
        }
    }

    /// <summary>
    /// Get the message type from JSON without full deserialization
    /// </summary>
    public static string? GetMessageType(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("type", out var typeProperty))
            {
                return typeProperty.GetString();
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return null;
    }

    /// <summary>
    /// Validate if a JSON string represents a valid protocol message
    /// </summary>
    public static bool IsValidMessage(string json)
    {
        return TryDeserialize(json, out var message) && message != null;
    }
} 
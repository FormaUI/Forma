namespace Forma.Blazor.Features.Input.Wheel;

using System.Text.Json.Serialization;

/// <summary>The axis a wheel turn mostly moves along.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<WheelAxis>))]
public enum WheelAxis
{
    /// <summary>Up or down, including a turn with no horizontal part.</summary>
    [JsonStringEnumMemberName("vertical")]
    Vertical,

    /// <summary>Left or right, when the horizontal part is the larger.</summary>
    [JsonStringEnumMemberName("horizontal")]
    Horizontal,
}

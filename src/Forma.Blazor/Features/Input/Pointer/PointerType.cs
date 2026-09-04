namespace Forma.Blazor.Features.Input.Pointer;

using System.Text.Json.Serialization;

/// <summary>The kind of device behind a pointer.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<PointerType>))]
public enum PointerType
{
    /// <summary>A device the browser did not identify.</summary>
    [JsonStringEnumMemberName("unknown")]
    Unknown,

    /// <summary>A mouse.</summary>
    [JsonStringEnumMemberName("mouse")]
    Mouse,

    /// <summary>A pen or stylus.</summary>
    [JsonStringEnumMemberName("pen")]
    Pen,

    /// <summary>A finger on a touch surface.</summary>
    [JsonStringEnumMemberName("touch")]
    Touch,
}

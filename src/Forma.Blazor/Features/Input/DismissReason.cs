namespace Forma.Blazor.Features.Input;

using System.Text.Json.Serialization;

/// <summary>What asked the foremost active input scope to go.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<DismissReason>))]
public enum DismissReason
{
    /// <summary>The Escape key.</summary>
    [JsonStringEnumMemberName("escape")]
    Escape,

    /// <summary>A pointer pressed away from the scope.</summary>
    [JsonStringEnumMemberName("press-away")]
    PressAway,

    /// <summary>A platform close request: a dialog's cancel, a back gesture, an assistive-technology dismiss.</summary>
    [JsonStringEnumMemberName("close-request")]
    CloseRequest,
}

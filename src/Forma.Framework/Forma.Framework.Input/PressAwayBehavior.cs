namespace Forma.Framework.Input;

using System.Text.Json.Serialization;

/// <summary>How the foremost active input scope responds when pressed away from.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<PressAwayBehavior>))]
public enum PressAwayBehavior
{
    /// <summary>The press is left alone.</summary>
    [JsonStringEnumMemberName("ignore")]
    Ignore,

    /// <summary>The scope is dismissed and the press reaches whatever was pressed.</summary>
    [JsonStringEnumMemberName("pass-through")]
    PassThrough,

    /// <summary>The scope is dismissed, focus stays where it is, and the click that follows the press is swallowed.</summary>
    [JsonStringEnumMemberName("dismiss")]
    Dismiss,
}

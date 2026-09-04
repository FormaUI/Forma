namespace Forma.Blazor.Features.Input;

using System.Text.Json.Serialization;

/// <summary>Whether an input a route has consumed continues to listeners outside its scope.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<InputPropagation>))]
public enum InputPropagation
{
    /// <summary>The input carries on to the listeners outside the scope.</summary>
    [JsonStringEnumMemberName("continue")]
    Continue,

    /// <summary>The input stops at the scope that consumed it.</summary>
    [JsonStringEnumMemberName("stop")]
    Stop,
}

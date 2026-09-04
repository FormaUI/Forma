namespace Forma.Blazor.Features.Input;

using System.Text.Json.Serialization;

/// <summary>What the foremost active input scope does when asked to go: by Escape, or by a platform close request.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<DismissBehavior>))]
public enum DismissBehavior
{
    /// <summary>The request is left alone, and no scope beneath is offered it.</summary>
    [JsonStringEnumMemberName("ignore")]
    Ignore,

    /// <summary>The scope is dismissed and the request is consumed.</summary>
    [JsonStringEnumMemberName("dismiss")]
    Dismiss,

    /// <summary>The scope is told to cancel and the request is consumed.</summary>
    [JsonStringEnumMemberName("cancel")]
    Cancel,
}

namespace Forma.Framework.Browser.Interop.Operations;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Playback state a media element holds as a DOM property.</summary>
public sealed class Playback(IJSEnvironment js) : IAsyncDisposable
{
    private readonly JSModule _module = new(js, "./_content/forma/browser/scripts/operations/Playback.js");

    /// <summary>Mutes the media element, resuming playback if it is paused.</summary>
    public ValueTask<bool> MuteAsync(ElementReference media)
        => _module.TryInvokeVoidAsync("mute", media);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => _module.DisposeAsync();
}

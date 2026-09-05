namespace Forma.Framework.Common.Interop.Operations;

using Forma.Framework.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Playback state a rendered media element only exposes as a property.</summary>
public sealed class Playback(IJSRuntime js) : IAsyncDisposable
{
    private readonly JsModule _module = new(js, "./_content/Forma.Framework.Common.Interop/scripts/operations/Playback.js");

    /// <summary>Mutes the media element, resuming playback if it is paused.</summary>
    public ValueTask<bool> MuteAsync(ElementReference media)
        => _module.TryInvokeVoidAsync("mute", media);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => _module.DisposeAsync();
}

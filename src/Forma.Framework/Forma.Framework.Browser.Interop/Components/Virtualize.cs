namespace Forma.Framework.Browser.Interop.Components;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>The browser side of a virtualized list: where the list stands in what scrolls it, how tall its drawn items are, and bringing one of them into view.</summary>
public sealed class Virtualize(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/browser/scripts/components/Virtualize.js")
{
    /// <summary>Attaches to the list opened by <paramref name="start"/>; <paramref name="owner"/> is told through <c>HandleViewportAsync</c> how far the viewport has scrolled into the list, how tall the viewport is and the average room a drawn item takes, whenever one of them changes.</summary>
    /// <param name="start">The spacer that opens the list.</param>
    /// <param name="owner">The owner told where the list stands.</param>
    /// <param name="spacerAttribute">The attribute every spacer carries, the closing one with the value <c>end</c>.</param>
    public ValueTask<bool> AttachAsync<TOwner>(ElementReference start, DotNetObjectReference<TOwner> owner, string spacerAttribute)
        where TOwner : class
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(spacerAttribute);

        return AttachCoreAsync(start, new { owner, spacerAttribute });
    }

    /// <summary>Scrolls so the point <paramref name="offset"/> pixels into the list stands at the top of the viewport.</summary>
    /// <param name="offset">The distance into the list, in pixels.</param>
    public ValueTask<bool> ScrollToAsync(double offset)
        => InvokeAsync("scrollTo", offset);

    /// <summary>Brings the drawn item at <paramref name="ordinal"/>, counted among the items the list draws, into view, answering whether it is in view.</summary>
    /// <param name="ordinal">The item's place among the items drawn.</param>
    /// <param name="start">Whether the item is aligned to the top rather than scrolled by the least that shows it.</param>
    public async ValueTask<bool> RevealAsync(int ordinal, bool start)
        => await InvokeAsync<bool>("reveal", ordinal, start ? "start" : "nearest");
}

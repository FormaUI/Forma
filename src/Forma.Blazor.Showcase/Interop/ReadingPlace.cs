namespace Forma.Blazor.Showcase.Interop;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;

/// <summary>Keeps the reader's place in the window across a link to the same page in another language, and shows the list's entry for the current page.</summary>
/// <remarks>
/// The place is noted when such a link is pressed, as the section at the top of the window with how far into it the window
/// stands, or as the window's own scroll above the first section, and the page the link leads to returns to it. The list
/// is scrolled to center its current entry when the entry is out of view, on every page, whenever the list is shown after
/// being hidden, and on each <see cref="RevealAsync"/>.
/// </remarks>
internal sealed class ReadingPlace(IJSEnvironment js)
    : JSInterop(js, "./scripts/ReadingPlace.js")
{
    /// <summary>Attaches to <paramref name="target"/>, the list holding the current page's entry, with the selectors for the <paramref name="links"/> that change the language, the <paramref name="sections"/> the place is measured by and the list's <paramref name="current"/> entry.</summary>
    public ValueTask<bool> AttachAsync(ElementReference target, string links, string sections, string current)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(links);
        ArgumentException.ThrowIfNullOrWhiteSpace(sections);
        ArgumentException.ThrowIfNullOrWhiteSpace(current);

        return AttachCoreAsync(target, new { links, sections, current });
    }

    /// <summary>Scrolls the list to center its current entry when the entry is out of view.</summary>
    public ValueTask<bool> RevealAsync()
        => InvokeAsync("reveal");
}

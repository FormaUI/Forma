namespace Forma.Framework.Browser.Interop.Text;

using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.Components;

/// <summary>A text field's entry: the range its selection spans, as the reader moves it, and whether the browser filled it.</summary>
public sealed class DomText : IDomTextObservation
{
    private static readonly DomCapability<TextSelectionWatchOptions> Selecting = new(DomRoot.Watcher, "selection", static _ => new SelectionDescription());
    private static readonly DomCapability<AutofillWatchOptions> Autofilling = new(DomRoot.Watcher, "autofill", static _ => new AutofillDescription());

    private readonly DomWatcher _watcher;
    private readonly ElementReference _element;

    internal DomText(DomWatcher watcher, ElementReference element)
    {
        _watcher = watcher;
        _element = element;
    }

    /// <inheritdoc />
    public async Task<DomObservation<TextSelectionWatchOptions, TextSelection>> WatchSelectionAsync(Func<TextSelection, Task> handler, bool shouldObserve = true)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var observation = new DomObservation<TextSelectionWatchOptions, TextSelection>(_watcher.Session, Selecting, DomJsonContext.Default.TextSelection, handler);
        await observation.SyncAsync(_element, new TextSelectionWatchOptions(), shouldObserve);

        return observation;
    }

    /// <inheritdoc />
    public async Task<DomObservation<AutofillWatchOptions, AutofillState>> WatchAutofillAsync(Func<AutofillState, Task> handler, bool shouldObserve = true)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var observation = new DomObservation<AutofillWatchOptions, AutofillState>(_watcher.Session, Autofilling, DomJsonContext.Default.AutofillState, handler);
        await observation.SyncAsync(_element, new AutofillWatchOptions(), shouldObserve);

        return observation;
    }

    /// <summary>The selection watch as the browser reads it, which takes no options.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    private sealed record SelectionDescription;

    /// <summary>The autofill watch as the browser reads it, which takes no options.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    private sealed record AutofillDescription;
}

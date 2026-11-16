namespace Forma.Framework.Browser;

/// <summary>A text field's entry, as an owner observes it.</summary>
public interface IDomTextObservation
{
    /// <summary>Observes the field's <see cref="TextSelection"/> while <paramref name="shouldObserve"/>, handing each range the browser measures to <paramref name="handler"/>.</summary>
    Task<IDomObservation<TextSelectionWatchOptions, TextSelection>> WatchSelectionAsync(Func<TextSelection, Task> handler, bool shouldObserve = true);

    /// <summary>Observes the field's <see cref="AutofillState"/> while <paramref name="shouldObserve"/>, handing each state the browser reports to <paramref name="handler"/>.</summary>
    Task<IDomObservation<AutofillWatchOptions, AutofillState>> WatchAutofillAsync(Func<AutofillState, Task> handler, bool shouldObserve = true);
}

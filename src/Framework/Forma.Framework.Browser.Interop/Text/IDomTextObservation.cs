namespace Forma.Framework.Browser.Interop.Text;

/// <summary>A text field's entry, as an owner observes it.</summary>
public interface IDomTextObservation
{
    /// <summary>Observes the field's <see cref="TextSelection"/> while <paramref name="shouldObserve"/>, handing each range the browser measures to <paramref name="handler"/>.</summary>
    Task<DomObservation<TextSelectionWatchOptions, TextSelection>> WatchSelectionAsync(Func<TextSelection, Task> handler, bool shouldObserve = true);
}

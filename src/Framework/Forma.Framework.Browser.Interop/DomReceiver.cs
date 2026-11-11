namespace Forma.Framework.Browser.Interop;

using Forma.Framework.JavaScript.Interop;

using Microsoft.JSInterop;

/// <summary>An owner's receiver of what its registrations report from the browser: the states its observations measure and the edits its text fields make.</summary>
public sealed class DomReceiver
{
    private readonly DomSession _session;

    internal DomReceiver(DomSession session)
    {
        _session = session;
    }

    /// <summary>Delivers the state the observation holding <paramref name="token"/> measured under <paramref name="generation"/>.</summary>
    /// <returns>A task that completes as the observation takes the state.</returns>
    [JSInvokable]
    public Task NotifyAsync(string token, int generation, JSTransfer state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return _session.DeliverAsync(token, generation, state);
    }

    /// <summary>Delivers the edit the text field whose edits <paramref name="token"/> holds made at <paramref name="revision"/>: from <paramref name="startIndex"/>, <paramref name="removedLength"/> characters removed and <paramref name="insertedText"/> put in their place.</summary>
    /// <returns>A task that completes as the field's edits take the edit.</returns>
    [JSInvokable]
    public Task EditAsync(string token, int revision, int startIndex, int removedLength, JSTransfer insertedText, bool isCommit)
    {
        ArgumentNullException.ThrowIfNull(insertedText);

        return _session.TryGetEditTarget(token, out var edits) ? edits.ReceiveEditAsync(revision, startIndex, removedLength, insertedText, isCommit) : insertedText.DiscardAsync().AsTask();
    }

    /// <summary>Delivers the whole text of the field whose edits <paramref name="token"/> holds, at <paramref name="revision"/>, in place of the edits that led to it.</summary>
    /// <returns>A task that completes as the field's edits take the text.</returns>
    [JSInvokable]
    public Task ReplaceAsync(string token, int revision, JSTransfer text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return _session.TryGetEditTarget(token, out var edits) ? edits.ReceiveReplacementAsync(revision, text) : text.DiscardAsync().AsTask();
    }
}

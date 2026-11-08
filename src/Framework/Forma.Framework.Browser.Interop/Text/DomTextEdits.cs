namespace Forma.Framework.Browser.Interop.Text;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;

/// <summary>A text field's text as .NET holds it, following the edits the field sends and writing .NET's own changes through the field.</summary>
/// <remarks>
/// The field numbers its edits by revision across every registration of it, and .NET applies them in that order: an edit arriving ahead of its
/// predecessor waits for it, and one whose revision the text has already passed is dropped. An edit that cannot be applied, its inserted text
/// refused or broken on its way or its range outside the text, has the field send its whole text in its place. A text of .NET's own is written
/// through the field, which answers the revision it then stands at; the edits arriving meanwhile wait, and those it supersedes are dropped,
/// one lost or still being read among them, with what they committed. A
/// write the field has not taken is made once it is attached, and again as the connection comes back. Each change is handed to the
/// handler in turn, the next edit applied once the handler has finished with the last, so a change's work is done before the next arrives.
/// The browser sends a field's edits in the order they happen and ahead of any action raised after them, so an action that settles the edits
/// before it reads the text sees every edit made before it; a field whose edits were attaching as the action began commits what it holds once
/// they land, since what the reader typed before they listened went unsent.
/// </remarks>
public sealed class DomTextEdits : IAsyncDisposable, IDomRegistrationHost<TextEditOptions>
{
    private static readonly DomCapability<TextEditOptions> Editing = new(DomRoot.Engine, "edits", DescribeEdits);

    private readonly DomSession _session;
    private readonly DomRegistration<TextEditOptions> _registration;
    private readonly Func<TextChange, Task> _handler;
    private readonly SortedList<int, Arrival> _arrivals = [];
    private Arrival? _replacement;
    private TextEditOptions _options = new();
    private string _text;
    private int _revision;
    private string? _shownText;
    private string? _pendingText;
    private bool _isApplying;
    private bool _isWriting;
    private bool _needsReplacement;
    private bool _isReplacementRequested;
    private bool _hasLostCommit;
    private bool _isFaultReported;
    private Task _sync = Task.CompletedTask;
    private TaskCompletionSource? _changed;

    /// <summary>Creates the edits of a text field holding <paramref name="text"/>, made through <paramref name="engine"/> and handing each change they make to <paramref name="handler"/>, which follow the field once synced with its element.</summary>
    public DomTextEdits(DomEngine engine, string? text, Func<TextChange, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(handler);

        _session = engine.Session;
        _handler = handler;
        _text = text ?? string.Empty;
        _shownText = text;
        _registration = new DomRegistration<TextEditOptions>(_session, Editing, this, isReleased: engine.IsDisposed);
        _session.AddEditTarget(_registration.Token, this);
    }

    /// <summary>The text as .NET holds it.</summary>
    public string Text
        => _text;

    /// <summary>Whether the field's edits have been released.</summary>
    public bool IsDisposed
        => _registration.IsDisposed;

    /// <summary>Whether the text can't follow the field's edits: an edit was lost on its way or fell outside the text, and the field's whole text, which replaces it, was refused, broke on its way, or could not be asked for.</summary>
    public bool IsFaulted
        => _needsReplacement && !_isReplacementRequested && !_isApplying;

    // Every edit received is applied, and nothing of .NET's own waits to be written.
    private bool IsSettled
        => !_isApplying && !_isWriting && _pendingText is null && _replacement is null && !_needsReplacement && _arrivals.Count == 0;

    // A field's maximum length counts UTF-16 code units, each at most three bytes of UTF-8.
    private long AcceptedLength
        => _options.MaxLength is { } maxLength ? 3L * maxLength : _session.TransferOptions.MaximumLength;

    /// <summary>The text a field renders as its value while its component shows <paramref name="text"/>.</summary>
    /// <remarks>
    /// While the text shown is the one .NET holds, the field keeps the text it was last rendered or written with, so a render never reverts typing
    /// that outpaced it; a text of .NET's own is rendered as it is written.
    /// </remarks>
    public string? GetShownText(string? text)
        => (text ?? string.Empty) == (_pendingText ?? _text) ? _shownText : text;

    /// <summary>Brings the edits in step with the field: <paramref name="element"/> sending them as <paramref name="options"/> sets out, with <paramref name="text"/> written through it where it differs from the text .NET holds.</summary>
    /// <exception cref="ArgumentException">The options name an empty count output.</exception>
    public async Task SyncAsync(ElementReference element, string? text, TextEditOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.MaxLength is { } maxLength)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, 1, nameof(options));
        }

        if (options.CountOutputId is { } countOutputId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(countOutputId, nameof(options));
        }

        if (IsDisposed)
        {
            return;
        }

        if ((text ?? string.Empty) != (_pendingText ?? _text))
        {
            _pendingText = text ?? string.Empty;
            _shownText = text;
        }

        _options = options;
        var sync = _sync = _registration.SyncAsync(element, options, shouldApply: true);
        await sync;
        await WriteAsync();
    }

    /// <summary>Waits until the text holds every edit the field sent before the call, an attach under way having landed first; a field whose edits were attaching commits what it holds.</summary>
    /// <returns>Whether the text holds them, which it doesn't where it can't follow the field's edits.</returns>
    public async Task<bool> SettleAsync()
    {
        var wasAttached = _registration.IsActive;
        await _sync;

        if (IsDisposed || !_registration.IsActive)
        {
            return !IsFaulted;
        }

        var target = _revision;

        // What the reader typed before the edits listened went unsent, so the field commits it, ahead of answering the revision it stands at.
        if (!wasAttached && await _registration.TryInvokeAsync<int?>("invoke", createsState: false, _session.RuntimeKey, _registration.Token, _registration.Generation, "flush") is { } flushed)
        {
            target = flushed;
        }

        while (!IsDisposed)
        {
            if (_pendingText is not null && !_isWriting)
            {
                await WriteAsync();

                if (_pendingText is not null)
                {
                    return false;
                }
            }

            if (IsFaulted)
            {
                return false;
            }

            if (IsSettled && _revision >= target)
            {
                return true;
            }

            // An edit sent ahead of the call that hasn't arrived was lost, and the field's whole text takes its place, asked for once.
            if (!_needsReplacement && !_isApplying && (IsSettled || (_arrivals.Count > 0 && !_arrivals.ContainsKey(_revision + 1))))
            {
                _needsReplacement = true;
                await ApplyAsync();
                continue;
            }

            await (_changed ??= new TaskCompletionSource()).Task;
        }

        return !IsFaulted;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => _registration.DisposeAsync();

    /// <summary>Takes the edit the field made at <paramref name="revision"/>.</summary>
    internal Task ReceiveEditAsync(int revision, int start, int removed, JSTransfer inserted, bool isCommit)
        => ReceiveAsync(new Arrival(revision, inserted, start, removed, isCommit));

    /// <summary>Takes the field's whole text at <paramref name="revision"/>, in place of the edits that led to it.</summary>
    internal Task ReceiveReplacementAsync(int revision, JSTransfer text)
        => ReceiveAsync(new Arrival(revision, text));

    /// <summary>Writes a text the field has not taken, and has the field send its whole text where it stands at another revision, as the connection comes back.</summary>
    internal async Task ResyncAsync()
    {
        if (IsDisposed || !_registration.IsActive)
        {
            return;
        }

        // A replacement asked for before the connection went may never have been sent.
        _isReplacementRequested = false;
        await WriteAsync();

        if (!_isReplacementRequested)
        {
            await RequestReplacementAsync();
        }
    }

    /// <inheritdoc />
    async Task<bool> IDomRegistrationHost<TextEditOptions>.EstablishAsync(ElementReference element, TextEditOptions options, int generation)
        => await _registration.TryInvokeAsync<bool>("attach", createsState: true, _session.RuntimeKey, _session.Receiver, new DomAttachRequest
        {
            Token = _registration.Token,
            Generation = generation,
            Capability = Editing.Name,
            Target = element,
            Options = (EditsDescription)Editing.DescribeOptions(options) with { Text = _text, Revision = _revision },
        });

    /// <inheritdoc />
    void IDomRegistrationHost<TextEditOptions>.OnEstablished(int generation)
    {
    }

    /// <inheritdoc />
    void IDomRegistrationHost<TextEditOptions>.OnInvalidated()
    {
        // Edits outlive a generation, since the field numbers them across its registrations; they go with the registration's release alone.
        if (IsDisposed)
        {
            _ = DiscardAsync();
            SignalChange();
        }
    }

    private async Task ReceiveAsync(Arrival arrival)
    {
        var isPassed = arrival.IsReplacement ? arrival.Revision < _revision : arrival.Revision <= _revision;

        if (IsDisposed || isPassed || (!arrival.IsReplacement && !_arrivals.TryAdd(arrival.Revision, arrival)))
        {
            // A replacement the text has passed answers the request for it, as one taken does.
            if (arrival.IsReplacement)
            {
                _isReplacementRequested = false;
            }

            await arrival.Transfer.DiscardAsync();
            return;
        }

        if (arrival.IsReplacement)
        {
            if (_replacement is { } replaced)
            {
                await replaced.Transfer.DiscardAsync();
            }

            _replacement = arrival;
        }

        // A write the field hasn't taken holds what arrives, so an arrival makes it again first.
        if (_pendingText is not null)
        {
            await WriteAsync();
            return;
        }

        await ApplyAsync();
    }

    private async Task WriteAsync()
    {
        if (_isWriting)
        {
            return;
        }

        _isWriting = true;

        try
        {
            while (!IsDisposed && _registration.IsActive && _pendingText is { } text)
            {
                var revision = await _registration.TryInvokeAsync<int?>("invoke", createsState: false, _session.RuntimeKey, _registration.Token, _registration.Generation, "write", text);

                if (revision is not { } written)
                {
                    break;
                }

                if (_pendingText == text)
                {
                    _pendingText = null;
                }

                await AdoptAsync(text, written);

                // The field holds .NET's text in place of every edit made before the write, those lost and what they committed among them.
                _needsReplacement = false;
                _hasLostCommit = false;
            }
        }
        finally
        {
            _isWriting = false;
        }

        await ApplyAsync();
        SignalChange();
    }

    // The field sends its replacement ahead of answering, so the request is marked as made before the call goes out.
    private async Task RequestReplacementAsync()
    {
        _isReplacementRequested = true;

        var isSent = await _registration.TryInvokeAsync<bool>("invoke", createsState: false, _session.RuntimeKey, _registration.Token, _registration.Generation, "resend", _revision);

        if (!isSent)
        {
            _isReplacementRequested = false;
        }
    }

    // One pass at a time applies what has arrived in revision order, none while a write of .NET's own is under way or waiting, and a pass asks
    // for a replacement once at most, so a field that can't be reached is asked again by the next arrival or reconnection rather than in a loop.
    private async Task ApplyAsync()
    {
        if (_isApplying || _isWriting || _pendingText is not null)
        {
            return;
        }

        _isApplying = true;

        try
        {
            var hasRequested = false;

            while (!IsDisposed && !_isWriting && _pendingText is null)
            {
                // A replacement refused is asked for again by the next arrival rather than in this pass.
                if (_replacement is { } replacement)
                {
                    _replacement = null;
                    hasRequested = true;
                    await ReplaceAsync(replacement);
                }
                else if (_needsReplacement)
                {
                    if (hasRequested || _isReplacementRequested)
                    {
                        break;
                    }

                    hasRequested = true;
                    await RequestReplacementAsync();
                }
                else if (_arrivals.TryGetValue(_revision + 1, out var edit))
                {
                    _arrivals.Remove(edit.Revision);
                    await ApplyEditAsync(edit);
                }
                else
                {
                    break;
                }
            }
        }
        finally
        {
            _isApplying = false;
        }

        await ReportFaultAsync();
        SignalChange();
    }

    // The handler hears as the text starts or stops being unable to follow the field, the text being the last it held.
    private async Task ReportFaultAsync()
    {
        if (IsFaulted != _isFaultReported && !IsDisposed)
        {
            await NotifyAsync(isCommit: false);
        }
    }

    private void SignalChange()
    {
        var changed = _changed;
        _changed = null;
        changed?.TrySetResult();
    }

    private async Task ApplyEditAsync(Arrival edit)
    {
        string? inserted;

        try
        {
            inserted = await edit.Transfer.ReadTextAsync(AcceptedLength) ?? string.Empty;
        }
        catch (JSTransferException ex)
        {
            DomLog.EditLost(_session.Logger, ex, _registration.Token, edit.Revision, _session.RuntimeId);
            inserted = null;
        }

        // A write of .NET's own that landed while the edit was read took its place.
        if (edit.Revision <= _revision)
        {
            return;
        }

        if (inserted is null)
        {
            MarkLost(edit);
            return;
        }

        if (edit.Start < 0 || edit.Removed < 0 || edit.Start > _text.Length || edit.Removed > _text.Length - edit.Start)
        {
            DomLog.EditMisplaced(_session.Logger, _registration.Token, edit.Revision, _session.RuntimeId);
            MarkLost(edit);
            return;
        }

        _text = string.Concat(_text.AsSpan(0, edit.Start), inserted, _text.AsSpan(edit.Start + edit.Removed));
        _revision = edit.Revision;
        await NotifyAsync(edit.IsCommit);
    }

    private async Task ReplaceAsync(Arrival replacement)
    {
        _isReplacementRequested = false;

        if (replacement.Revision < _revision)
        {
            await replacement.Transfer.DiscardAsync();
            return;
        }

        string? text;

        try
        {
            text = await replacement.Transfer.ReadTextAsync(AcceptedLength) ?? string.Empty;
        }
        catch (JSTransferException ex)
        {
            DomLog.EditLost(_session.Logger, ex, _registration.Token, replacement.Revision, _session.RuntimeId);
            text = null;
        }

        // A text lost at a revision .NET holds, as a write of its own that landed while the text was read leaves it, needs no replacing.
        if (text is null)
        {
            _needsReplacement |= replacement.Revision > _revision;
            return;
        }

        // A write of .NET's own that landed while the text was read took its place.
        if (replacement.Revision < _revision)
        {
            return;
        }

        _needsReplacement = false;
        await AdoptAsync(text, replacement.Revision);

        var isCommit = _hasLostCommit;
        _hasLostCommit = false;
        await NotifyAsync(isCommit);
    }

    // The text stands at the revision given, and every edit it supersedes is dropped, a commit among them carried by what replaced it.
    private async Task AdoptAsync(string text, int revision)
    {
        _text = text;
        _revision = Math.Max(_revision, revision);

        while (_arrivals.Count > 0 && _arrivals.Keys[0] <= _revision)
        {
            var superseded = _arrivals.Values[0];
            _arrivals.RemoveAt(0);
            _hasLostCommit |= superseded.IsCommit;
            await superseded.Transfer.DiscardAsync();
        }
    }

    private static EditsDescription DescribeEdits(TextEditOptions options)
        => new()
        {
            SendsOnInput = options.SendsOnInput,
            Count = options.CountOutputId is { } output ? new CountDescription { Output = output, MaxLength = options.MaxLength } : null,
        };

    private void MarkLost(Arrival edit)
    {
        _needsReplacement = true;
        _hasLostCommit |= edit.IsCommit;
    }

    [SuppressMessage("Design", "CA1031", Justification = "A handler's failure is the owner's, logged under it, and leaves later edits to be applied.")]
    private async Task NotifyAsync(bool isCommit)
    {
        try
        {
            _isFaultReported = IsFaulted;
            await _handler(new TextChange { Text = _text, IsCommit = isCommit, IsFaulted = _isFaultReported });
        }
        catch (Exception ex)
        {
            DomLog.HandlerFailed(_session.Logger, ex, Editing.Name, _registration.Token, _registration.Generation, _session.RuntimeId);
        }
    }

    private async Task DiscardAsync()
    {
        List<Arrival> arrivals = [.. _arrivals.Values];
        _arrivals.Clear();

        if (_replacement is { } replacement)
        {
            arrivals.Add(replacement);
            _replacement = null;
        }

        foreach (var arrival in arrivals)
        {
            await arrival.Transfer.DiscardAsync();
        }
    }

    /// <summary>What arrived from the field: an edit at its revision, or the field's whole text at the revision it stands at.</summary>
    private sealed record Arrival(int Revision, JSTransfer Transfer, int Start = 0, int Removed = 0, bool IsCommit = false, bool IsReplacement = false)
    {
        public Arrival(int revision, JSTransfer text)
            : this(revision, text, IsReplacement: true)
        {
        }
    }

    /// <summary>The edits as the browser reads them: whether the field sends each as it is typed, the count it shows, and the text .NET holds with its revision.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    private sealed record EditsDescription
    {
        public required bool SendsOnInput { get; init; }

        public CountDescription? Count { get; init; }

        public string Text { get; init; } = string.Empty;

        public int Revision { get; init; }
    }

    /// <summary>The count as the browser reads it: the element whose text shows it, and the most the field accepts.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    private sealed record CountDescription
    {
        public required string Output { get; init; }

        public required int? MaxLength { get; init; }
    }
}

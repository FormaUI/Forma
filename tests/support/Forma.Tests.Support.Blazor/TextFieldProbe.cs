namespace Forma.Tests.Blazor;

using System.Text;

using AngleSharp.Dom;

using Bunit;

using Forma.Framework.Browser.Interop;
using Forma.Framework.JavaScript.Interop;
using Forma.Tests.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>A stand-in for the text fields' edits script under a bUnit context: it takes each field's attach, sends the edits the reader's typing makes through the owner's receiver as the browser does, and answers the owner's writes and its requests for a field's whole text.</summary>
public sealed class TextFieldProbe
{
    /// <summary>The engine's entry module.</summary>
    public const string EnginePath = "./_content/forma/browser/scripts/DomEngine.js";

    // The attribute bUnit writes an element's reference into.
    private const string ElementReferenceAttribute = "blazor:elementReference";

    private readonly BunitContext _context;
    private readonly List<Field> _fields = [];
    private readonly AttachHandler _attach;
    private readonly WriteHandler _write;
    private readonly ResendHandler _resend;

    /// <summary>Sets the engine's module up on the context's interop, every other interaction attaching.</summary>
    public TextFieldProbe(BunitContext context)
        : this(context, SetupEngine(context))
    {
    }

    /// <summary>Takes the text fields' edits on <paramref name="engine"/>, an engine module set up already.</summary>
    public TextFieldProbe(BunitContext context, BunitJSModuleInterop engine)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(engine);

        _context = context;
        _attach = new AttachHandler(this);
        _write = new WriteHandler(this);
        _resend = new ResendHandler(this);
        engine.AddInvocationHandler(_attach);
        engine.AddInvocationHandler(_write);
        engine.AddInvocationHandler(_resend);
    }

    /// <summary>Leaves <paramref name="text"/> in <paramref name="field"/> as the reader types it, sending the edit at once where the field sends each edit as it is typed.</summary>
    public Task TypeAsync(IElement field, string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var edits = GetField(field);
        edits.Value = text;

        return edits.SendsOnInput ? SendAsync(edits, isCommit: false) : Task.CompletedTask;
    }

    /// <summary>Commits the edit of <paramref name="field"/>, as the field does losing the focus.</summary>
    public Task CommitAsync(IElement field)
        => SendAsync(GetField(field), isCommit: true);

    /// <summary>Leaves <paramref name="text"/> in <paramref name="field"/> and commits it, as the reader does typing and leaving the field.</summary>
    public async Task ChangeAsync(IElement field, string text)
    {
        await TypeAsync(field, text).ConfigureAwait(false);
        await CommitAsync(field).ConfigureAwait(false);
    }

    /// <summary>Leaves <paramref name="text"/> in <paramref name="field"/> and commits it as the browser sends a text too long for one message, its inserted text streamed and held on its way until the edit is released.</summary>
    /// <returns>The edit, whose release lets its text arrive.</returns>
    public HeldEdit CommitHeld(IElement field, string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var edits = GetField(field);
        edits.Value = text;
        var (start, removed, inserted) = Diff(edits.Sent, edits.Value);
        edits.Sent = edits.Value;
        var revision = ++edits.Revision;
        var stream = new HeldJSStreamReference(Encoding.UTF8.GetBytes(inserted));
        var transfer = new JSTransfer { Stream = stream, IsText = true };
        var delivery = _context.Renderer.Dispatcher.InvokeAsync(() => edits.Receiver.EditAsync(edits.Token, revision, start, removed, transfer, isCommit: true));

        return new HeldEdit(stream, delivery);
    }

    /// <summary>Leaves <paramref name="text"/> in <paramref name="field"/> and commits it as the browser sends a text too long for one message, its stream breaking on its way, and the whole text the browser then sends in its place breaking too, so the field's edits can't follow it until the reader's next edit.</summary>
    /// <returns>A task that completes once the field's edits have lost the replacement.</returns>
    public Task CommitBrokenAsync(IElement field, string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var edits = GetField(field);
        edits.Value = text;
        var (start, removed, inserted) = Diff(edits.Sent, edits.Value);
        edits.Sent = edits.Value;
        var revision = ++edits.Revision;
        edits.BreaksNextReplacement = true;

        return _context.Renderer.Dispatcher.InvokeAsync(() => edits.Receiver.EditAsync(edits.Token, revision, start, removed, CreateBrokenTransfer(inserted), isCommit: true));
    }

    /// <summary>The text <paramref name="field"/> holds.</summary>
    public string GetText(IElement field)
        => GetField(field).Value;

    // A text's stream that breaks as it is read.
    private static JSTransfer CreateBrokenTransfer(string text)
        => new() { Stream = new FakeJSStreamReference(Encoding.UTF8.GetBytes(text), new IOException("The stream broke on its way.")), IsText = true };

    private static BunitJSModuleInterop SetupEngine(BunitContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var engine = context.JSInterop.SetupModule(EnginePath);
        engine.Mode = JSRuntimeMode.Loose;
        engine.Setup<bool>("attach", _ => true).SetResult(true);

        return engine;
    }

    // The edit that turns one text into another, never parting a surrogate pair, as the browser computes it.
    private static (int Start, int Removed, string Inserted) Diff(string before, string after)
    {
        var shortest = Math.Min(before.Length, after.Length);
        var start = 0;

        while (start < shortest && before[start] == after[start])
        {
            start++;
        }

        if (start > 0 && char.IsHighSurrogate(before[start - 1]))
        {
            start--;
        }

        var end = 0;

        while (end < shortest - start && before[before.Length - 1 - end] == after[after.Length - 1 - end])
        {
            end++;
        }

        if (end > 0 && char.IsLowSurrogate(before[^end]))
        {
            end--;
        }

        return (start, before.Length - start - end, after[start..^end]);
    }

    // A field is found by the reference its element was rendered with. bUnit leaves the reference empty on an element it draws again, so
    // every lookup first notes the id of each field's element still carrying its reference, and a field is found by that id, or as the one
    // field whose reference no element in the markup carries.
    private Field GetField(IElement field)
    {
        ArgumentNullException.ThrowIfNull(field);

        IEnumerable<IElement> elements = field.GetRoot() is IParentNode root ? root.QuerySelectorAll("*") : [];
        var carried = new HashSet<string>();

        foreach (var element in elements)
        {
            if (element.GetAttribute(ElementReferenceAttribute) is { Length: > 0 } reference)
            {
                carried.Add(reference);

                foreach (var candidate in _fields.Where(candidate => candidate.ElementId == reference && !string.IsNullOrEmpty(element.Id)))
                {
                    candidate.DomId = element.Id;
                }
            }
        }

        var elementId = field.GetAttribute(ElementReferenceAttribute);
        var redrawn = _fields.Where(candidate => !carried.Contains(candidate.ElementId ?? string.Empty)).ToList();

        return (string.IsNullOrEmpty(elementId) ? null : _fields.LastOrDefault(candidate => candidate.ElementId == elementId))
            ?? (string.IsNullOrEmpty(field.Id) ? null : _fields.LastOrDefault(candidate => candidate.DomId == field.Id))
            ?? (string.IsNullOrEmpty(elementId) && redrawn.Count == 1 ? redrawn[0] : null)
            ?? throw new InvalidOperationException($"No text field's edits are attached to <{field.LocalName}>.");
    }

    private Field? FindField(object? runtimeKey, object? token)
        => _fields.SingleOrDefault(field => field.RuntimeKey == (string?)runtimeKey && field.Token == (string?)token);

    private Task SendAsync(Field field, bool isCommit)
    {
        if (field.Value == field.Sent && !isCommit)
        {
            return Task.CompletedTask;
        }

        var (start, removed, inserted) = Diff(field.Sent, field.Value);
        field.Sent = field.Value;
        var revision = ++field.Revision;

        return _context.Renderer.Dispatcher.InvokeAsync(() => field.Receiver.EditAsync(field.Token, revision, start, removed, InlineTransfer.Of(inserted), isCommit));
    }

    /// <summary>An edit whose text is streamed and held on its way.</summary>
    public sealed class HeldEdit
    {
        private readonly HeldJSStreamReference _stream;
        private readonly Task _delivery;

        internal HeldEdit(HeldJSStreamReference stream, Task delivery)
        {
            _stream = stream;
            _delivery = delivery;
        }

        /// <summary>Lets the edit's text arrive.</summary>
        /// <returns>A task that completes once the field's edits have taken it.</returns>
        public Task ReleaseAsync()
        {
            _stream.Release();

            return _delivery;
        }
    }

    /// <summary>A field's edits as the browser keeps them: the text it holds, the text it last sent and the revision it stands at.</summary>
    private sealed class Field(string runtimeKey, string token, DomReceiver receiver)
    {
        public string RuntimeKey { get; } = runtimeKey;

        public string Token { get; } = token;

        public DomReceiver Receiver { get; } = receiver;

        public string? ElementId { get; set; }

        public string? DomId { get; set; }

        public bool SendsOnInput { get; set; }

        public string Value { get; set; } = string.Empty;

        public string Sent { get; set; } = string.Empty;

        public int Revision { get; set; }

        public bool BreaksNextReplacement { get; set; }
    }

    /// <summary>Attaches a field's edits, which go on from where its previous attach left them.</summary>
    private sealed class AttachHandler(TextFieldProbe probe)
        : JSRuntimeInvocationHandlerBase<bool>(invocation => invocation.Identifier == "attach" && DomProbe<object>.GetRequestField(invocation, "capability") is "edits", isCatchAllHandler: false)
    {
        protected override Task<bool> HandleAsync(JSRuntimeInvocation invocation)
        {
            var runtimeKey = (string)invocation.Arguments[0]!;
            var token = (string)DomProbe<object>.GetRequestField(invocation, "token")!;
            var field = probe.FindField(runtimeKey, token);

            if (field is null)
            {
                var text = (string)DomProbe<object>.GetOption(invocation, "text")!;
                field = new Field(runtimeKey, token, ((DotNetObjectReference<DomReceiver>)invocation.Arguments[1]!).Value)
                {
                    Value = text,
                    Sent = text,
                    Revision = (int)DomProbe<object>.GetOption(invocation, "revision")!,
                };
                probe._fields.Add(field);
            }

            field.ElementId = ((ElementReference)DomProbe<object>.GetRequestField(invocation, "target")!).Id;
            field.SendsOnInput = (bool)DomProbe<object>.GetOption(invocation, "sendsOnInput")!;

            return Task.FromResult(true);
        }
    }

    /// <summary>Writes a text of the owner's own into a field, answering the revision it then stands at.</summary>
    private sealed class WriteHandler(TextFieldProbe probe)
        : JSRuntimeInvocationHandlerBase<int?>(invocation => invocation.Identifier == "invoke" && invocation.Arguments[3] is "write", isCatchAllHandler: false)
    {
        protected override Task<int?> HandleAsync(JSRuntimeInvocation invocation)
        {
            if (probe.FindField(invocation.Arguments[0], invocation.Arguments[1]) is not { } field)
            {
                return Task.FromResult<int?>(null);
            }

            field.Value = field.Sent = (string)invocation.Arguments[4]!;

            return Task.FromResult<int?>(++field.Revision);
        }
    }

    /// <summary>Sends the owner a field's whole text where the owner's copy stands at another revision, ahead of answering, as the browser's message does.</summary>
    private sealed class ResendHandler(TextFieldProbe probe)
        : JSRuntimeInvocationHandlerBase<bool>(invocation => invocation.Identifier == "invoke" && invocation.Arguments[3] is "resend", isCatchAllHandler: false)
    {
        protected override async Task<bool> HandleAsync(JSRuntimeInvocation invocation)
        {
            if (probe.FindField(invocation.Arguments[0], invocation.Arguments[1]) is not { } field || (int)invocation.Arguments[4]! == field.Revision)
            {
                return false;
            }

            var text = field.BreaksNextReplacement ? CreateBrokenTransfer(field.Sent) : InlineTransfer.Of(field.Sent);
            field.BreaksNextReplacement = false;
            await field.Receiver.ReplaceAsync(field.Token, field.Revision, text).ConfigureAwait(false);

            return true;
        }
    }
}

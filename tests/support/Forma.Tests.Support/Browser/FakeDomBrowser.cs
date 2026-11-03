namespace Forma.Tests.Browser;

using System.Text.Json;

using Forma.Framework.Browser.Interop;
using Forma.Framework.JavaScript.Interop;
using Forma.Tests.Interop;

using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

/// <summary>A stand-in for the browser's DOM watcher and engine entries: a runtime importing each by its path, recording every import and call, answering each call through one function or holding it open, and delivering states through an owner's receiver as the browser does.</summary>
public sealed class FakeDomBrowser
{
    /// <summary>The watcher's entry module.</summary>
    public const string WatcherPath = "./_content/forma/browser/scripts/DomWatcher.js";

    /// <summary>The engine's entry module.</summary>
    public const string EnginePath = "./_content/forma/browser/scripts/DomEngine.js";

    private readonly Dictionary<string, IJSObjectReference> _modules = [];

    /// <summary>Creates the browser, answering each call to an entry through <paramref name="respond"/> from the entry's path, the identifier and the arguments.</summary>
    public FakeDomBrowser(Func<string, string, object?[], object?> respond)
    {
        ArgumentNullException.ThrowIfNull(respond);

        Watcher = new RecordingJSObjectReference((identifier, args) => respond(WatcherPath, identifier, args));
        Engine = new RecordingJSObjectReference((identifier, args) => respond(EnginePath, identifier, args));
        _modules[WatcherPath] = Watcher;
        _modules[EnginePath] = Engine;
        Runtime = new RecordingJSRuntime((identifier, args) => identifier == "import"
            ? _modules.GetValueOrDefault((string?)args[0] ?? string.Empty) ?? throw new JSException($"No module is served at '{args[0]}'.")
            : throw new JSException($"'{identifier}' is not an import."));
    }

    /// <summary>The runtime the entries are imported through.</summary>
    public RecordingJSRuntime Runtime { get; }

    /// <summary>The watcher's entry.</summary>
    public RecordingJSObjectReference Watcher { get; }

    /// <summary>The engine's entry.</summary>
    public RecordingJSObjectReference Engine { get; }

    /// <summary>Serves <paramref name="module"/> at <paramref name="path"/>.</summary>
    public void Serve(string path, IJSObjectReference module)
        => _modules[path] = module;

    /// <summary>The imports of <paramref name="path"/> made so far.</summary>
    public int CountImports(string path)
        => Runtime.Recorder.GetCalls("import").Count(call => (string?)call.Arguments[0] == path);

    /// <summary>Creates the JS environment of an owner logging through <paramref name="loggerFactory"/>.</summary>
    public IJSEnvironment CreateEnvironment(ILoggerFactory loggerFactory, Guid? runtimeId = null)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        return new JSEnvironment(Runtime, loggerFactory.CreateLogger("Owner"), runtimeId ?? Guid.CreateVersion7());
    }

    /// <summary>Reads a field of what an attach asked the browser for: <c>token</c>, <c>generation</c>, <c>capability</c>, <c>target</c> or <c>options</c>.</summary>
    public static object? GetRequestField(RecordedCall attach, string field)
    {
        ArgumentNullException.ThrowIfNull(attach);

        var request = attach.Arguments[^1]!;

        return InteropPayload.GetValue(request, field);
    }

    /// <summary>Reads an option of what an attach asked the browser for.</summary>
    public static object? GetOption(RecordedCall attach, string option)
    {
        var options = GetRequestField(attach, "options")!;

        return InteropPayload.GetValue(options, option);
    }

    /// <summary>Delivers <paramref name="state"/> through the receiver the observation's <paramref name="attach"/> carried, under its token and generation, as the browser does.</summary>
    public static Task DeliverAsync(RecordedCall attach, object state)
    {
        ArgumentNullException.ThrowIfNull(attach);

        var receiver = ((DotNetObjectReference<DomReceiver>)attach.Arguments[1]!).Value;

        return receiver.NotifyAsync((string)GetRequestField(attach, "token")!, (int)GetRequestField(attach, "generation")!, JsonSerializer.SerializeToElement(state, JsonSerializerOptions.Web));
    }
}

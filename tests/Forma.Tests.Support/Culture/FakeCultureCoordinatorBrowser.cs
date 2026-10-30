namespace Forma.Tests.Culture;

using Forma.Framework.Culture.Browser.Interop;
using Forma.Tests.Interop;

using Microsoft.JSInterop;

/// <summary>A stand-in for the page's culture coordinator: a runtime importing its module, which accepts each request in the document's sequence and delivers each accepted pair to every registration as the script does, recording every call and failing the calls a test names.</summary>
public sealed class FakeCultureCoordinatorBrowser
{
    /// <summary>The culture coordinator's module.</summary>
    public const string ModulePath = "./_content/forma/culture/scripts/CultureCoordinator.js";

    private readonly Dictionary<string, Queue<Failure>> _failures = [];
    private readonly Dictionary<string, Registration> _registrations = new(StringComparer.Ordinal);

    /// <summary>Creates the coordinator over a page the server rendered in <paramref name="culture"/> and <paramref name="uiCulture"/>.</summary>
    public FakeCultureCoordinatorBrowser(string culture = "en-US", string? uiCulture = null)
    {
        Accepted = (culture, uiCulture ?? culture, 0);
        Module = new RecordingJSObjectReference(RespondToModule);
        Runtime = new RecordingJSRuntime((identifier, args) => identifier == "import" && (string?)args[0] == ModulePath
            ? Run("import", () => Module)
            : throw new JSException($"No module is served at '{args.FirstOrDefault()}'."));
    }

    /// <summary>The runtime the module is imported through.</summary>
    public RecordingJSRuntime Runtime { get; }

    /// <summary>The module.</summary>
    public RecordingJSObjectReference Module { get; }

    /// <summary>The key of the document the coordinator belongs to.</summary>
    public string DocumentKey { get; } = $"document-{Guid.CreateVersion7()}";

    /// <summary>The pair the coordinator accepted last, with its number in the document's sequence.</summary>
    public (string Culture, string UICulture, int Number) Accepted { get; private set; }

    /// <summary>The identities of the live registrations.</summary>
    public IReadOnlyCollection<string> Identities
        => _registrations.Keys;

    /// <summary>The registrations asked for so far.</summary>
    public int CountRegistrations()
        => Module.Recorder.GetCalls("register").Count;

    /// <summary>The registration the browser holds for <paramref name="identity"/>.</summary>
    public RecordingJSObjectReference GetRegistration(string identity)
        => _registrations[identity].Reference;

    /// <summary>Faults the next call to <paramref name="identifier"/> with <paramref name="exception"/> before it takes effect.</summary>
    public void FailNext(string identifier, Exception exception)
        => Enqueue(identifier, exception);

    /// <summary>Accepts a landing at the pair of <paramref name="culture"/> and <paramref name="uiCulture"/> as the next switch, delivering it to every registration.</summary>
    /// <returns>A task that completes once every registration's state has applied the pair.</returns>
    public Task LandAsync(string culture, string uiCulture)
        => Task.WhenAll(Accept(culture, uiCulture).Select(DeliverAsync));

    /// <summary>Closes the circuit, releasing its registrations as the initializer does.</summary>
    public void CloseCircuit()
        => _registrations.Clear();

    private void Enqueue(string identifier, Exception exception)
    {
        if (!_failures.TryGetValue(identifier, out var failures))
        {
            failures = new Queue<Failure>();
            _failures[identifier] = failures;
        }

        failures.Enqueue(new Failure(exception));
    }

    private T Run<T>(string identifier, Func<T> effect)
        => _failures.TryGetValue(identifier, out var failures) && failures.Count > 0 ? throw failures.Dequeue().Exception : effect();

    private RecordingJSObjectReference RespondToModule(string identifier, object?[] args)
        => identifier switch
        {
            "register" => Run(identifier, () => Register((string)args[0]!, (DotNetObjectReference<CultureCoordinatorReceiver>)args[1]!)),
            _ => throw new JSException($"The module exports no '{identifier}'."),
        };

    private RecordingJSObjectReference Register(string identity, DotNetObjectReference<CultureCoordinatorReceiver> receiver)
    {
        if (!_registrations.TryGetValue(identity, out var registration))
        {
            registration = new Registration(identity);
            registration.Reference = new RecordingJSObjectReference((identifier, args) => RespondToRegistration(registration, identifier, args));
            _registrations[identity] = registration;
        }

        registration.Receiver = receiver;
        _ = DeliverAsync(registration);

        return registration.Reference;
    }

    private object? RespondToRegistration(Registration registration, string identifier, object?[] args)
        => identifier switch
        {
            "request" => Run(identifier, () => Request(registration, (string)args[0]!, (string)args[1]!)),
            "dispose" => Run(identifier, () => Dispose(registration)),
            _ => throw new JSException($"A registration has no '{identifier}'."),
        };

    private object? Request(Registration registration, string culture, string uiCulture)
    {
        if (!_registrations.ContainsKey(registration.Identity))
        {
            throw new JSException("The culture state requesting the switch is no longer registered with the page.");
        }

        foreach (var live in Accept(culture, uiCulture))
        {
            _ = DeliverAsync(live);
        }

        return null;
    }

    private List<Registration> Accept(string culture, string uiCulture)
    {
        Accepted = (culture, uiCulture, Accepted.Number + 1);

        return [.. _registrations.Values];
    }

    private Task DeliverAsync(Registration registration)
        => registration.Receiver!.Value.ApplyAsync(Accepted.Culture, Accepted.UICulture, DocumentKey, Accepted.Number);

    private object? Dispose(Registration registration)
    {
        _registrations.Remove(registration.Identity);

        return null;
    }

    private sealed record Failure(Exception Exception);

    private sealed class Registration(string identity)
    {
        public string Identity { get; } = identity;

        public RecordingJSObjectReference Reference { get; set; } = default!;

        public DotNetObjectReference<CultureCoordinatorReceiver>? Receiver { get; set; }
    }
}

namespace Forma.Tests.Culture;

using Forma.Framework.Culture.Browser.Interop;
using Forma.Tests.Interop;

using Microsoft.JSInterop;

/// <summary>A stand-in for the page's culture coordinator: a runtime importing its module, which accepts each request, each interactive landing and each pair an application's coordinator has it follow in the document's sequence and delivers each accepted pair to every registration as the script does, recording every call and failing the calls a test names.</summary>
public sealed class FakeCultureCoordinatorBrowser
{
    /// <summary>The culture coordinator's module.</summary>
    public const string ModulePath = "./_content/forma/culture/scripts/CultureCoordinator.js";

    private readonly Dictionary<string, Queue<Failure>> _failures = [];
    private readonly Dictionary<string, Registration> _registrations = new(StringComparer.Ordinal);
    private readonly Queue<string> _requestOutcomes = [];
    private readonly List<string> _routeCookies = [];
    private readonly List<(string Culture, string UICulture)> _followed = [];
    private (string Token, string Culture, string UICulture)[] _routes = [];
    private string? _defaultToken;
    private bool _hasMarker = true;
    private bool _isStarted;

    /// <summary>Creates the coordinator over a page the server rendered in <paramref name="culture"/> and <paramref name="uiCulture"/>.</summary>
    public FakeCultureCoordinatorBrowser(string culture = "en-US", string? uiCulture = null)
    {
        Accepted = (culture, uiCulture ?? culture, 0);
        Module = new RecordingJSObjectReference(RespondToModule);
        Runtime = new RecordingJSRuntime((identifier, args) => identifier == "import" && (string?)args[0] == ModulePath
            ? Run("import", () => Module)
            : throw new JSException($"No module is served at '{args.FirstOrDefault()}'."));
    }

    /// <summary>Creates the coordinator over a page no server published a culture marker in, a standalone app's or a web view's, which starts from the start its first registration supplies.</summary>
    public static FakeCultureCoordinatorBrowser CreateWithoutMarker()
        => new() { _hasMarker = false };

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

    /// <summary>The tokens the coordinator wrote to the culture-route cookie, one per interactive landing it accepted, in order.</summary>
    public IReadOnlyList<string> RouteCookies
        => _routeCookies;

    /// <summary>The pairs an application's coordinator had the document follow, in order.</summary>
    public IReadOnlyList<(string Culture, string UICulture)> Followed
        => _followed;

    /// <summary>Publishes the culture routes the page's address policy maps, each token to its pair, the address naming none taking <paramref name="defaultToken"/>.</summary>
    public void UseRoutes(string defaultToken, params (string Token, string Culture, string UICulture)[] routes)
    {
        _defaultToken = defaultToken;
        _routes = routes;
    }

    /// <summary>Answers the next request with <paramref name="outcome"/>; <c>canceled</c> and <c>timed-out</c> accept nothing, as a navigation that didn't land.</summary>
    public void AnswerNextRequest(string outcome)
        => _requestOutcomes.Enqueue(outcome);

    /// <summary>The registrations asked for so far.</summary>
    public int CountRegistrations()
        => Module.Recorder.GetCalls("register").Count;

    /// <summary>The registration the browser holds for <paramref name="identity"/>.</summary>
    public RecordingJSObjectReference GetRegistration(string identity)
        => _registrations[identity].Reference;

    /// <summary>Holds every later registration open until <see cref="AnswerRegistrations"/>, as a runtime still importing the module holds it.</summary>
    public void HoldRegistrations()
        => Module.Recorder.Hold("register");

    /// <summary>Answers every registration held open as the script does, delivering the accepted pair to each, and answers later ones at once.</summary>
    public void AnswerRegistrations()
    {
        Module.Recorder.Release("register");

        foreach (var call in Module.Recorder.GetCalls("register").Where(call => call.IsHeld && !call.IsAnswered))
        {
            call.Complete(Register((string)call.Arguments[0]!, (DotNetObjectReference<CultureCoordinatorReceiver>)call.Arguments[1]!, (CultureCoordinatorStart?)call.Arguments[3]));
        }
    }

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
            "register" => Run(identifier, () => Register((string)args[0]!, (DotNetObjectReference<CultureCoordinatorReceiver>)args[1]!, (CultureCoordinatorStart?)args[3])),
            _ => throw new JSException($"The module exports no '{identifier}'."),
        };

    // A page without a marker starts from the first registration's start, as the script publishes it, at number zero.
    private RecordingJSObjectReference Register(string identity, DotNetObjectReference<CultureCoordinatorReceiver> receiver, CultureCoordinatorStart? start)
    {
        if (!_isStarted)
        {
            _isStarted = true;

            if (!_hasMarker)
            {
                Accepted = start is null
                    ? throw new JSException("The page carries no valid culture marker.")
                    : (start.Marker["data-culture"], start.Marker["data-ui-culture"], 0);
            }
        }

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
            "reportLanding" => Run(identifier, () => ReportLanding(registration, (string?)args[0], (int)args[1]!)),
            "follow" => Run(identifier, () => Follow(registration, (string)args[0]!, (string)args[1]!)),
            "dispose" => Run(identifier, () => Dispose(registration)),
            _ => throw new JSException($"A registration has no '{identifier}'."),
        };

    private string Request(Registration registration, string culture, string uiCulture)
    {
        if (!_registrations.ContainsKey(registration.Identity))
        {
            throw new JSException("The culture state requesting the switch is no longer registered with the page.");
        }

        if (_requestOutcomes.TryDequeue(out var outcome) && outcome is "canceled" or "timed-out")
        {
            return outcome;
        }

        foreach (var liveRegistration in Accept(culture, uiCulture))
        {
            _ = DeliverAsync(liveRegistration);
        }

        return "accepted";
    }

    // A report this registration made before one already received is obsolete; an accepted landing at a pair already
    // accepted delivers nothing.
    private object? ReportLanding(Registration registration, string? token, int order)
    {
        if (order <= registration.LastLanding)
        {
            return null;
        }

        registration.LastLanding = order;
        var route = _routes.FirstOrDefault(candidate => string.Equals(candidate.Token, token ?? _defaultToken, StringComparison.OrdinalIgnoreCase));

        if (route.Token is null)
        {
            return null;
        }

        _routeCookies.Add(route.Token);

        if (route.Culture != Accepted.Culture || route.UICulture != Accepted.UICulture)
        {
            foreach (var liveRegistration in Accept(route.Culture, route.UICulture))
            {
                _ = DeliverAsync(liveRegistration);
            }
        }

        return null;
    }

    // The document takes the pair it follows, delivering it to every registration but the one that has it follow.
    private object? Follow(Registration registration, string culture, string uiCulture)
    {
        _followed.Add((culture, uiCulture));

        if (culture != Accepted.Culture || uiCulture != Accepted.UICulture)
        {
            foreach (var liveRegistration in Accept(culture, uiCulture).Where(other => other != registration))
            {
                _ = DeliverAsync(liveRegistration);
            }
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

        public int LastLanding { get; set; }
    }
}

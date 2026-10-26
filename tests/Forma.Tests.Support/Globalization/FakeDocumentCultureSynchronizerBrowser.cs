namespace Forma.Tests.Globalization;

using Forma.Tests.Interop;

using Microsoft.JSInterop;

/// <summary>A stand-in for the browser's document culture synchronizer: a runtime importing its module, which keeps one owner's pair on the document as the script does, recording every call and failing the calls a test names.</summary>
public sealed class FakeDocumentCultureSynchronizerBrowser
{
    /// <summary>The document culture synchronizer's module.</summary>
    public const string ModulePath = "./_content/forma/globalization/scripts/DocumentCultureSynchronizer.js";

    private readonly Dictionary<string, Queue<Failure>> _failures = [];
    private readonly List<(string Language, string Direction)> _updates = [];
    private Owner? _owner;

    /// <summary>Creates the browser over a document the server served in <paramref name="servedLanguage"/> and <paramref name="servedDirection"/>.</summary>
    public FakeDocumentCultureSynchronizerBrowser(string servedLanguage = "en", string servedDirection = "ltr")
    {
        Served = (servedLanguage, servedDirection);
        Document = Served;
        Module = new RecordingJSObjectReference(RespondToModule);
        Runtime = new RecordingJSRuntime((identifier, args) => identifier == "import" && (string?)args[0] == ModulePath
            ? Run("import", () => Module)
            : throw new JSException($"No module is served at '{args.FirstOrDefault()}'."));
    }

    /// <summary>The runtime the module is imported through.</summary>
    public RecordingJSRuntime Runtime { get; }

    /// <summary>The module.</summary>
    public RecordingJSObjectReference Module { get; }

    /// <summary>The pair the server served the document.</summary>
    public (string Language, string Direction) Served { get; }

    /// <summary>The document's language and direction.</summary>
    public (string Language, string Direction) Document { get; private set; }

    /// <summary>The identity of the document's owner, where one owns it.</summary>
    public string? OwnerIdentity
        => _owner?.Identity;

    /// <summary>The registration the document's owner holds, where one owns it.</summary>
    public RecordingJSObjectReference? OwnerRegistration
        => _owner?.Registration;

    /// <summary>The pairs every registration was updated with, in order.</summary>
    public IReadOnlyList<(string Language, string Direction)> Updates
        => _updates;

    /// <summary>The imports of the module made so far.</summary>
    public int CountImports()
        => Runtime.Recorder.GetCalls("import").Count;

    /// <summary>The registrations asked for so far.</summary>
    public int CountRegistrations()
        => Module.Recorder.GetCalls("register").Count;

    /// <summary>Faults the next call to <paramref name="identifier"/> with <paramref name="exception"/> before it takes effect.</summary>
    public void FailNext(string identifier, Exception exception)
        => Enqueue(identifier, new Failure(exception, IsAfterEffect: false));

    /// <summary>Lets the next call to <paramref name="identifier"/> take effect and then faults it with <paramref name="exception"/>, so its caller never hears the result.</summary>
    public void LoseNextResult(string identifier, Exception exception)
        => Enqueue(identifier, new Failure(exception, IsAfterEffect: true));

    /// <summary>Closes the circuit, releasing the owner as the initializer does and restoring the served pair.</summary>
    public void CloseCircuit()
        => Release();

    private void Release()
    {
        _owner = null;
        Document = Served;
    }

    private void Enqueue(string identifier, Failure failure)
    {
        if (!_failures.TryGetValue(identifier, out var failures))
        {
            failures = new Queue<Failure>();
            _failures[identifier] = failures;
        }

        failures.Enqueue(failure);
    }

    private T Run<T>(string identifier, Func<T> effect)
    {
        var failure = _failures.TryGetValue(identifier, out var failures) && failures.Count > 0 ? failures.Dequeue() : null;

        if (failure is { IsAfterEffect: false })
        {
            throw failure.Exception;
        }

        var result = effect();

        return failure is null ? result : throw failure.Exception;
    }

    private RecordingJSObjectReference RespondToModule(string identifier, object?[] args)
        => identifier switch
        {
            "register" => Run(identifier, () => Register((string)args[0]!, (string)args[1]!, (string)args[2]!)),
            _ => throw new JSException($"The module exports no '{identifier}'."),
        };

    private RecordingJSObjectReference Register(string identity, string language, string direction)
    {
        if (_owner is not null && _owner.Identity != identity)
        {
            throw new JSException("A document follows one culture state.");
        }

        if (_owner is null)
        {
            var owner = new Owner(identity);
            owner.Registration = new RecordingJSObjectReference((identifier, args) => RespondToRegistration(owner, identifier, args));
            _owner = owner;
        }

        Document = (language, direction);

        return _owner.Registration!;
    }

    private object? RespondToRegistration(Owner owner, string identifier, object?[] args)
        => identifier switch
        {
            "update" => Run(identifier, () => Update(owner, (string)args[0]!, (string)args[1]!)),
            "dispose" => Run(identifier, () => Dispose(owner)),
            _ => throw new JSException($"A registration has no '{identifier}'."),
        };

    private bool Update(Owner owner, string language, string direction)
    {
        _updates.Add((language, direction));

        if (_owner != owner)
        {
            return false;
        }

        Document = (language, direction);

        return true;
    }

    private object? Dispose(Owner owner)
    {
        if (_owner == owner)
        {
            Release();
        }

        return null;
    }

    private sealed record Failure(Exception Exception, bool IsAfterEffect);

    private sealed class Owner(string identity)
    {
        public string Identity { get; } = identity;

        public RecordingJSObjectReference? Registration { get; set; }
    }
}

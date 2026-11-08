namespace Forma.Tests.Blazor;

using AngleSharp.Dom;

using Bunit;

using Forma.Tests.Browser;

using Microsoft.AspNetCore.Components;

/// <summary>A stand-in for the DOM watcher's and engine's scripts under a bUnit context: it holds each observation's attach until the test reports the state the browser measured, then delivers later states through the owner's receiver, and attaches every interaction.</summary>
/// <typeparam name="TState">The state the observations measure.</typeparam>
public sealed class DomProbe<TState>
    where TState : class
{
    private readonly BunitContext _context;
    private readonly HeldAttach _heldAttach = new();

    /// <summary>Sets the watcher's and engine's modules up on the context's interop.</summary>
    public DomProbe(BunitContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
        Watcher = context.JSInterop.SetupModule(FakeDomBrowser.WatcherPath);
        Watcher.Mode = JSRuntimeMode.Loose;
        Watcher.AddInvocationHandler(_heldAttach);
        Engine = context.JSInterop.SetupModule(FakeDomBrowser.EnginePath);
        Engine.Mode = JSRuntimeMode.Loose;
        Engine.Setup<bool>("attach", _ => true).SetResult(true);
    }

    /// <summary>The watcher's module on the context's interop.</summary>
    public BunitJSModuleInterop Watcher { get; }

    /// <summary>The engine's module on the context's interop.</summary>
    public BunitJSModuleInterop Engine { get; }

    /// <summary>Every observation attached so far, in order.</summary>
    public IReadOnlyList<JSRuntimeInvocation> Observations
        => _heldAttach.Attaches;

    /// <summary>Every interaction attached so far, in order.</summary>
    public IReadOnlyList<JSRuntimeInvocation> Interactions
        => [.. Engine.Invocations.Where(invocation => invocation.Identifier == "attach")];

    /// <summary>Reads a field of what an attach asked the browser for: <c>token</c>, <c>generation</c>, <c>capability</c>, <c>target</c> or <c>options</c>.</summary>
    public static object? GetRequestField(JSRuntimeInvocation attach, string field)
        => FakeDomBrowser.GetRequestField(attach.Arguments, field);

    /// <summary>Reads an option of what an attach asked the browser for.</summary>
    public static object? GetOption(JSRuntimeInvocation attach, string option)
        => FakeDomBrowser.GetOption(attach.Arguments, option);

    /// <summary>Reports <paramref name="state"/> as the browser measured it for the last observation attached, on the renderer.</summary>
    public Task ReportAsync(TState state)
        => ReportAsync(_heldAttach.Attaches[^1], state);

    /// <summary>Reports <paramref name="state"/> as the browser measured it for the last observation attached by the owner whose runtime identity is <paramref name="runtimeId"/>, on the renderer.</summary>
    public Task ReportAsync(string runtimeId, TState state)
        => ReportAsync(_heldAttach.Attaches.Last(attach => (string?)attach.Arguments[0] == runtimeId), state);

    /// <summary>Reports <paramref name="state"/> as the browser measured it for the last observation attached to <paramref name="target"/>, on the renderer.</summary>
    public Task ReportAsync(IElement target, TState state)
    {
        ArgumentNullException.ThrowIfNull(target);

        var id = target.GetAttribute("blazor:elementReference");

        return ReportAsync(_heldAttach.Attaches.Last(attach => GetRequestField(attach, "target") is ElementReference observed && observed.Id == id), state);
    }

    // The state answers the attach where it still waits, and otherwise is the next the browser delivers.
    private Task ReportAsync(JSRuntimeInvocation attach, TState state)
        => _context.Renderer.Dispatcher.InvokeAsync(() => _heldAttach.TryAnswer(attach, state) ? Task.CompletedTask : FakeDomBrowser.DeliverAsync(attach.Arguments, state));

    /// <summary>Holds each observation's attach until the test answers it.</summary>
    private sealed class HeldAttach()
        : JSRuntimeInvocationHandlerBase<TState>(invocation => invocation.Identifier == "attach", isCatchAllHandler: false)
    {
        // Plain, so the observation's continuation runs inline where the answer completes it.
        private readonly List<(JSRuntimeInvocation Attach, TaskCompletionSource<TState> Answer)> _heldAttaches = [];

        public IReadOnlyList<JSRuntimeInvocation> Attaches
            => [.. _heldAttaches.Select(held => held.Attach)];

        // An invocation equals only itself, since each holds its own arguments.
        public bool TryAnswer(JSRuntimeInvocation attach, TState state)
            => _heldAttaches.Single(held => held.Attach.Equals(attach)).Answer.TrySetResult(state);

        protected override Task<TState> HandleAsync(JSRuntimeInvocation invocation)
        {
            var answer = new TaskCompletionSource<TState>();
            _heldAttaches.Add((invocation, answer));

            return answer.Task;
        }
    }
}

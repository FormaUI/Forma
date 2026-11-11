namespace Forma.Tests.Blazor;

using System.Runtime.CompilerServices;

using Bunit;

using Microsoft.AspNetCore.Components;

/// <summary>Releases the completion a rendered component awaits on the renderer, returning once the component has acted on it, and waits on a rendered component's state while recording whether the wait was needed.</summary>
public static class RenderedComponentExtensions
{
    // The environment variable naming the folder the waits record themselves in, one file per test process.
    private const string WaitLogVariable = "FORMA_WAIT_LOG";

    private static readonly Lock WaitLogLock = new();

    /// <summary>Runs a release on the renderer, such as completing a gate a fake holds, and returns once the component has acted on it.</summary>
    /// <remarks>The release runs on the renderer, and a second turn there follows it. A plain <see cref="TaskCompletionSource"/> runs the component's continuation inline where the release completes it; one created to run its continuations asynchronously, as bUnit's held script answers are, queues the continuation on the renderer, where the second turn runs after it.</remarks>
    /// <typeparam name="TComponent">The rendered component's type.</typeparam>
    /// <param name="cut">The rendered component.</param>
    /// <param name="release">The action that lets the component continue.</param>
    public static async Task ReleaseAsync<TComponent>(this IRenderedComponent<TComponent> cut, Action release)
        where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(cut);
        ArgumentNullException.ThrowIfNull(release);

        await cut.InvokeAsync(release).ConfigureAwait(false);
        await cut.InvokeAsync(static () => { }).ConfigureAwait(false);
    }

    /// <summary>Waits until the state predicate holds, as bUnit's <c>WaitForState</c> does, recording whether it already held when the wait began.</summary>
    /// <remarks>While <c>FORMA_WAIT_LOG</c> names a folder, the predicate is checked once on the calling thread as the wait begins, ahead of any work queued on the renderer, and a line is appended to the test process's file in it, <c>held</c> or <c>waited</c> followed by the calling file and line, before any wait; otherwise the call is bUnit's wait alone.</remarks>
    /// <typeparam name="TComponent">The rendered component's type.</typeparam>
    /// <param name="cut">The rendered component.</param>
    /// <param name="statePredicate">The predicate the rendered state has to satisfy.</param>
    /// <param name="timeout">The time the wait may take.</param>
    /// <param name="callerFilePath">The calling file, supplied by the compiler.</param>
    /// <param name="callerLineNumber">The calling line, supplied by the compiler.</param>
    public static void WaitForStateWithDiagnostics<TComponent>(this IRenderedComponent<TComponent> cut, Func<bool> statePredicate, TimeSpan? timeout = null, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
        where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(cut);
        ArgumentNullException.ThrowIfNull(statePredicate);

        if (Environment.GetEnvironmentVariable(WaitLogVariable) is not { Length: > 0 } waitLog)
        {
            cut.WaitForState(statePredicate, timeout);
            return;
        }

        bool hasHeld;
        try
        {
            hasHeld = statePredicate();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            hasHeld = false;
        }

        RecordWait(waitLog, hasHeld, callerFilePath, callerLineNumber);
        if (!hasHeld)
        {
            cut.WaitForState(statePredicate, timeout);
        }
    }

    /// <summary>Waits until the assertion passes, as bUnit's <c>WaitForAssertion</c> does, recording whether it already passed when the wait began.</summary>
    /// <remarks>While <c>FORMA_WAIT_LOG</c> names a folder, the assertion is run once on the calling thread as the wait begins, ahead of any work queued on the renderer, and a line is appended to the test process's file in it, <c>held</c> or <c>waited</c> followed by the calling file and line, before any wait; otherwise the call is bUnit's wait alone.</remarks>
    /// <typeparam name="TComponent">The rendered component's type.</typeparam>
    /// <param name="cut">The rendered component.</param>
    /// <param name="assertion">The assertion the rendered state has to pass.</param>
    /// <param name="timeout">The time the wait may take.</param>
    /// <param name="callerFilePath">The calling file, supplied by the compiler.</param>
    /// <param name="callerLineNumber">The calling line, supplied by the compiler.</param>
    public static void WaitForAssertionWithDiagnostics<TComponent>(this IRenderedComponent<TComponent> cut, Action assertion, TimeSpan? timeout = null, [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
        where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(cut);
        ArgumentNullException.ThrowIfNull(assertion);

        if (Environment.GetEnvironmentVariable(WaitLogVariable) is not { Length: > 0 } waitLog)
        {
            cut.WaitForAssertion(assertion, timeout);
            return;
        }

        bool hasHeld;
        try
        {
            assertion();
            hasHeld = true;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            hasHeld = false;
        }

        RecordWait(waitLog, hasHeld, callerFilePath, callerLineNumber);
        if (!hasHeld)
        {
            cut.WaitForAssertion(assertion, timeout);
        }
    }

    // One file per process, since test projects run side by side and a file several processes append to loses lines.
    private static void RecordWait(string waitLog, bool hasHeld, string callerFilePath, int callerLineNumber)
    {
        lock (WaitLogLock)
        {
            Directory.CreateDirectory(waitLog);
            File.AppendAllText(Path.Combine(waitLog, $"{Environment.ProcessId}.log"), $"{(hasHeld ? "held" : "waited")} {Path.GetFileName(callerFilePath)}:{callerLineNumber}{Environment.NewLine}");
        }
    }
}

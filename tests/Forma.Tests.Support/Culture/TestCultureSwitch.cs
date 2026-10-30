namespace Forma.Tests.Culture;

using System.Globalization;

using Forma.Framework.Culture;

/// <summary>A culture switch a test directs, which accepts each switch in a sequence of its own, fails it, or holds it until the test releases it.</summary>
/// <param name="integration">The integration that applies each accepted pair to the user scope's culture state.</param>
public sealed class TestCultureSwitch(ICultureStateIntegration integration) : ICultureSwitch
{
    private readonly string _documentKey = $"test-{Guid.CreateVersion7()}";
    private readonly Queue<TaskCompletionSource> _held = [];
    private int _number;

    /// <summary>The pairs requested, in the order they were requested.</summary>
    public List<(CultureInfo Culture, CultureInfo UICulture)> Requests { get; } = [];

    /// <summary>The exception each switch fails with, changing nothing.</summary>
    public Exception? Failure { get; set; }

    /// <summary>Whether each switch waits for <see cref="Release"/> before it is accepted.</summary>
    public bool IsHolding { get; set; }

    /// <inheritdoc />
    /// <remarks>A switch accepted at once is applied before the method returns, and only the notification completes later.</remarks>
    public Task SwitchAsync(CultureInfo culture, CultureInfo? uiCulture = null)
    {
        ArgumentNullException.ThrowIfNull(culture);

        var accepted = new AcceptedCulture(culture, uiCulture ?? culture, new CultureVersion(_documentKey, ++_number));
        Requests.Add((accepted.Culture, accepted.UICulture));

        if (Failure is { } failure)
        {
            return Task.FromException(failure);
        }

        if (!IsHolding)
        {
            return integration.ApplyAsync(accepted);
        }

        var release = new TaskCompletionSource();
        _held.Enqueue(release);

        return AcceptOnReleaseAsync(release.Task, accepted);
    }

    /// <summary>Accepts every held switch, in the order they were requested, each applied where the release runs.</summary>
    public void Release()
    {
        while (_held.TryDequeue(out var release))
        {
            release.SetResult();
        }
    }

    private async Task AcceptOnReleaseAsync(Task release, AcceptedCulture accepted)
    {
        await release.ConfigureAwait(false);
        await integration.ApplyAsync(accepted).ConfigureAwait(false);
    }
}

namespace Forma.Framework.Themes;

using Forma.Framework.Events;

/// <summary>The <see cref="IThemeState"/> a user scope holds, starting from the registry's default choice.</summary>
/// <param name="registry">The themes registered, which every choice the state holds is resolved against.</param>
internal sealed class DefaultThemeState(ThemeRegistry registry) : IThemeState, IThemeStateIntegration
{
    private readonly StateNotifier _notifier = new();
    private int _revision;

    /// <inheritdoc />
    public ThemeChoice Choice { get; private set; } = registry.Default;

    /// <inheritdoc />
    public int Revision
        => Volatile.Read(ref _revision);

    /// <inheritdoc />
    public ThemeVersion Version { get; private set; } = ThemeVersion.Initial;

    /// <inheritdoc />
    public Task ApplyAsync(AcceptedTheme acceptedTheme)
    {
        ArgumentNullException.ThrowIfNull(acceptedTheme);

        if (!acceptedTheme.Version.Follows(Version))
        {
            return Task.CompletedTask;
        }

        var choice = registry.Resolve(acceptedTheme.Choice);
        Version = acceptedTheme.Version;

        if (choice == Choice)
        {
            return Task.CompletedTask;
        }

        Choice = choice;
        Volatile.Write(ref _revision, _revision + 1);

        return NotifyAsync();
    }

    /// <inheritdoc />
    public void Subscribe(Func<Task> callback)
        => _notifier.Subscribe(callback);

    /// <inheritdoc />
    public void Unsubscribe(Func<Task> callback)
        => _notifier.Unsubscribe(callback);

    // The notification goes on in the caller's context once the call that applied the change has returned.
    private async Task NotifyAsync()
    {
        await Task.Yield();
        await _notifier.NotifyAsync().ConfigureAwait(false);
    }
}

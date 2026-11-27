namespace Forma.Framework.Themes;

using Forma.Framework.Events;

/// <summary>The <see cref="IThemeState"/> a user scope holds, starting from the choice its scope starts in.</summary>
/// <param name="registry">The themes registered, which every choice the state holds is resolved against.</param>
/// <param name="startingChoice">The choice the scope starts in; the registry's default where the scope has none of its own.</param>
internal sealed class DefaultThemeState(ThemeRegistry registry, ThemeChoice? startingChoice = null) : IThemeState, IThemeStateIntegration
{
    private readonly StateNotifier _notifier = new();
    private int _revision;

    /// <inheritdoc />
    public ThemeChoice Choice { get; private set; } = registry.Resolve(startingChoice ?? registry.Default);

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

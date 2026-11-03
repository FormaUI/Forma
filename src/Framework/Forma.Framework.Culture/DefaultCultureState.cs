namespace Forma.Framework.Culture;

using System.Globalization;

using Forma.Framework.Events;

/// <summary>The <see cref="ICultureState"/> a user scope holds, starting from the cultures in force when the scope first resolves it.</summary>
internal sealed class DefaultCultureState : ICultureState, ICultureStateIntegration
{
    private readonly StateNotifier _notifier = new();
    private int _revision;

    /// <inheritdoc />
    public CultureInfo Culture { get; private set; } = CultureInfo.ReadOnly(CultureInfo.CurrentCulture);

    /// <inheritdoc />
    public CultureInfo UICulture { get; private set; } = CultureInfo.ReadOnly(CultureInfo.CurrentUICulture);

    /// <inheritdoc />
    public bool IsRightToLeft
        => UICulture.TextInfo.IsRightToLeft;

    /// <inheritdoc />
    public int Revision
        => Volatile.Read(ref _revision);

    /// <inheritdoc />
    public CultureVersion Version { get; private set; } = CultureVersion.Initial;

    /// <inheritdoc />
    /// <remarks>The method is synchronous up to the notification's task it returns, so the cultures are assigned on the caller's flow and stay in force after the call.</remarks>
    public Task ApplyAsync(AcceptedCulture accepted)
    {
        ArgumentNullException.ThrowIfNull(accepted);

        if (!accepted.Version.Follows(Version))
        {
            return Task.CompletedTask;
        }

        var culture = CultureInfo.ReadOnly(accepted.Culture);
        var uiCulture = CultureInfo.ReadOnly(accepted.UICulture);
        Version = accepted.Version;

        if (IsHeld(culture, Culture) && IsHeld(uiCulture, UICulture))
        {
            CultureInfo.CurrentCulture = Culture;
            CultureInfo.CurrentUICulture = UICulture;

            return Task.CompletedTask;
        }

        Culture = culture;
        UICulture = uiCulture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = uiCulture;
        Volatile.Write(ref _revision, _revision + 1);

        return _notifier.NotifyAsync();
    }

    /// <inheritdoc />
    public void Subscribe(Func<Task> callback)
        => _notifier.Subscribe(callback);

    /// <inheritdoc />
    public void Unsubscribe(Func<Task> callback)
        => _notifier.Unsubscribe(callback);

    // A culture of the same name that writes dates or numbers otherwise, as a clone pinning its own patterns does, is a change.
    private static bool IsHeld(CultureInfo culture, CultureInfo held)
        => culture.Equals(held) && CultureFormats.AreEqual(culture, held);
}

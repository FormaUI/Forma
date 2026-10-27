namespace Forma.Framework.Culture;

using System.Globalization;

using Forma.Framework.Events;

/// <summary>The <see cref="ICultureState"/> a user scope holds, starting from the cultures in force when the scope first resolves it.</summary>
internal sealed class DefaultCultureState : ICultureState
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
    /// <remarks>The method is synchronous up to the notification's task it returns, so the cultures are assigned on the caller's flow and stay in force after the call.</remarks>
    public Task ChangeAsync(CultureInfo culture, CultureInfo? uiCulture = null)
    {
        ArgumentNullException.ThrowIfNull(culture);

        var changedCulture = CultureInfo.ReadOnly(culture);
        var changedUICulture = CultureInfo.ReadOnly(uiCulture ?? culture);

        if (changedCulture.Equals(Culture) && changedUICulture.Equals(UICulture))
        {
            return Task.CompletedTask;
        }

        Culture = changedCulture;
        UICulture = changedUICulture;
        CultureInfo.CurrentCulture = changedCulture;
        CultureInfo.CurrentUICulture = changedUICulture;
        Volatile.Write(ref _revision, _revision + 1);

        return _notifier.NotifyAsync();
    }

    /// <inheritdoc />
    public void Subscribe(Func<Task> callback)
        => _notifier.Subscribe(callback);

    /// <inheritdoc />
    public void Unsubscribe(Func<Task> callback)
        => _notifier.Unsubscribe(callback);
}

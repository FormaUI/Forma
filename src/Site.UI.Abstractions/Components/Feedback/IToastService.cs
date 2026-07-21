using Site.UI.Abstractions.Foundation;

namespace Site.UI.Abstractions.Components.Feedback;

/// <summary>Manages the transient toast notifications shown to the user.</summary>
public interface IToastService : INotifyStateChanged
{
    /// <summary>The currently visible toasts.</summary>
    IReadOnlyList<ToastMessage> Toasts { get; }

    /// <summary>Removes the toast with the given identifier.</summary>
    Task RemoveAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Adds a toast with the given message, severity, and lifetime.</summary>
    Task ShowAsync(
        string message,
        ToastType type = ToastType.Success,
        TimeSpan? duration = null,
        CancellationToken cancellationToken = default);
}

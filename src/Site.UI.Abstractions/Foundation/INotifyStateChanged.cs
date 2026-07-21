namespace Site.UI.Abstractions.Foundation;

/// <summary>Notifies subscribers when the implementing object's state changes.</summary>
public interface INotifyStateChanged
{
    /// <summary>Registers a callback invoked on each state change.</summary>
    void Subscribe(Func<Task> callback);

    /// <summary>Removes a previously registered callback.</summary>
    void Unsubscribe(Func<Task> callback);
}

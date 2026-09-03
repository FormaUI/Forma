namespace Forma.Blazor.Components;

/// <summary>The parameters a component takes from its host, each applied again only when the host changes it.</summary>
internal sealed class ParameterStateTracker
{
    private readonly List<Action> _applications = [];

    /// <summary>Tracks the parameter the reader returns, applying its value whenever the host changes it.</summary>
    public void Track<T>(Func<T> read, Action<T> apply)
    {
        ArgumentNullException.ThrowIfNull(read);
        ArgumentNullException.ThrowIfNull(apply);

        var state = new ParameterState<T>();

        _applications.Add(() =>
        {
            var value = read();

            if (state.Update(value))
            {
                apply(value);
            }
        });
    }

    /// <summary>Takes every tracked parameter's current value, in the order tracked, applying those the host changed.</summary>
    public void Apply()
    {
        foreach (var application in _applications)
        {
            application();
        }
    }
}

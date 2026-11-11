namespace Forma.Blazor.Components;

/// <summary>One parameter's value as last taken from the host, telling a change the host made from a render that repeats the same value.</summary>
public sealed class ParameterState<T>
{
    private T _takenValue = default!;

    /// <summary>Takes the parameter's current value, reporting whether the host changed it since it was last taken.</summary>
    public bool Update(T parameter)
    {
        if (EqualityComparer<T>.Default.Equals(_takenValue, parameter))
        {
            return false;
        }

        _takenValue = parameter;

        return true;
    }
}

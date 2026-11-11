namespace Forma.Blazor.Components.DataView;

using Microsoft.AspNetCore.Components;

/// <summary>Counts the times the parameters a component is given change, apart from the ones that never change what its items look like.</summary>
/// <param name="ignoredNames">The names of the parameters that are left out.</param>
/// <param name="project">What stands for a parameter's value in the comparison, where the value itself would differ each time it is given.</param>
internal sealed class ParameterRevision(string[] ignoredNames, Func<ParameterValue, object?>? project = null)
{
    private readonly HashSet<string> _ignoredNames = [.. ignoredNames];
    private readonly Dictionary<string, object?> _values = [];

    /// <summary>How many times a parameter that is counted has changed, which is one for the first values it is given.</summary>
    public int Value { get; private set; }

    /// <summary>Compares the parameters about to be set with the last ones, counting one change however many differ.</summary>
    /// <param name="parameters">The parameters the parent gives.</param>
    public void Observe(ParameterView parameters)
    {
        var isChanged = false;

        foreach (var parameter in parameters)
        {
            if (parameter.Cascading || _ignoredNames.Contains(parameter.Name))
            {
                continue;
            }

            // A value is the same by Equals, so a record or a string given again is the same where a new delegate or expression tree is not.
            var value = project is null ? parameter.Value : project(parameter);

            if (!_values.TryGetValue(parameter.Name, out var lastValue) || !Equals(lastValue, value))
            {
                _values[parameter.Name] = value;
                isChanged = true;
            }
        }

        if (isChanged)
        {
            Value++;
        }
    }
}

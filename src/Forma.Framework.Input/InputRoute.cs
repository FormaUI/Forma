namespace Forma.Framework.Input;

using System.Text.RegularExpressions;

/// <summary>A semantic action a scope answers, and the bindings that raise it unless the host remaps them.</summary>
public sealed partial record InputRoute
{
    /// <summary>Declares <paramref name="action"/>, raised by <paramref name="bindings"/> by default.</summary>
    /// <param name="action">The action's name, <c>{owner}.{action}</c> such as <c>composer.bold</c>, which the host remaps bindings by and the owner receives.</param>
    /// <param name="bindings">The inputs that raise the action by default.</param>
    public InputRoute(string action, params IReadOnlyList<InputBinding> bindings)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(bindings);

        if (!ActionIdentifier().IsMatch(action))
        {
            throw new ArgumentException($"The action '{action}' must be named '{{owner}}.{{action}}', such as 'composer.bold', so a host override keys one thing.", nameof(action));
        }

        Action = action;
        Bindings = bindings;
    }

    /// <summary>The action's name, which the host remaps bindings by and the owner receives.</summary>
    public string Action { get; }

    /// <summary>The inputs that raise the action by default.</summary>
    public IReadOnlyList<InputBinding> Bindings { get; }

    /// <summary>Decides between routes whose bindings match equally well; higher wins.</summary>
    public int Priority { get; init; }

    /// <summary>Whether the browser's default for the input is prevented once the route consumes it; <see langword="null"/> takes the input class's default.</summary>
    public bool? PreventDefault { get; init; }

    /// <summary>Whether the input continues past the scope once the route consumes it; <see langword="null"/> takes the input class's default.</summary>
    public InputPropagation? Propagation { get; init; }

    [GeneratedRegex("^[a-z0-9-]+(\\.[a-z0-9-]+)+$")]
    private static partial Regex ActionIdentifier();
}

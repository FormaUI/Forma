namespace Forma.Framework.Input;

using System.Text;
using System.Text.RegularExpressions;

/// <summary>A semantic action a scope answers, and the bindings that raise it by default.</summary>
public sealed partial record InputRoute
{
    /// <summary>Declares <paramref name="action"/>, raised by <paramref name="bindings"/> by default.</summary>
    /// <param name="action">The action's name, <c>{owner}.{action}</c>, such as <c>composer.bold</c>.</param>
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

    /// <summary>The action's name, <c>{owner}.{action}</c>.</summary>
    public string Action { get; }

    /// <summary>The inputs that raise the action by default.</summary>
    public IReadOnlyList<InputBinding> Bindings { get; }

    /// <summary>The route's priority among routes whose bindings match equally well; higher wins.</summary>
    public int Priority { get; init; }

    /// <summary>Whether the browser's default for the input is prevented once the route consumes it; <see langword="null"/> takes the input class's default.</summary>
    public bool? PreventDefault { get; init; }

    /// <summary>Whether the input continues past the scope once the route consumes it; <see langword="null"/> takes the input class's default.</summary>
    public InputPropagation? Propagation { get; init; }

    /// <summary>Qualifies <paramref name="name"/> under <paramref name="owner"/> as an action identifier, <c>{owner}.{name}</c>.</summary>
    /// <remarks>The name is lowered, every run of characters outside letters, digits and hyphens becomes one hyphen, and hyphens at either end are trimmed.</remarks>
    public static string Qualify(string owner, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var slug = new StringBuilder(name.Length);

        foreach (var character in name)
        {
            var lowered = char.ToLowerInvariant(character);

            if (lowered is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-')
            {
                slug.Append(lowered);
            }
            else if (slug.Length > 0 && slug[^1] != '-')
            {
                slug.Append('-');
            }
        }

        var action = $"{owner}.{slug.ToString().Trim('-')}";

        if (!ActionIdentifier().IsMatch(action))
        {
            throw new ArgumentException($"'{name}' under '{owner}' leaves no action identifier.", nameof(name));
        }

        return action;
    }

    [GeneratedRegex("^[a-z0-9-]+(\\.[a-z0-9-]+)+$")]
    private static partial Regex ActionIdentifier();
}

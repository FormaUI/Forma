namespace Forma.Blazor.Components.Navigation;

using Forma.Framework.Icons;

/// <summary>A command a palette offers: its name, where it lives, its icon, and the address it goes to or the action it runs.</summary>
/// <param name="Name">The command's name, which the reader searches for.</param>
/// <remarks>A command goes to its <see cref="Url"/> or runs its <see cref="Run"/>, exactly one of the two.</remarks>
public sealed record PaletteCommand(string Name)
{
    /// <summary>Where the command lives, shown beside its name so two of one name tell apart, and searched as its name is.</summary>
    public string? Place { get; init; }

    /// <summary>The icon drawn before the command's name.</summary>
    public Icon? Icon { get; init; }

    /// <summary>The address the command goes to.</summary>
    public Uri? Url { get; init; }

    /// <summary>The action the command runs.</summary>
    public Func<Task>? Run { get; init; }
}

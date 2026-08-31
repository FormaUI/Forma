namespace Forma.Framework.Localization;

/// <summary>A value a format's hole names, paired with the name that hole uses.</summary>
/// <param name="Name">The name a hole refers to the value by.</param>
/// <param name="Value">The value the hole renders.</param>
public readonly record struct LocalizedArgument(string Name, object? Value)
{
    /// <summary>An argument held under a name no hole can spell, reachable only by an expression naming none.</summary>
    /// <param name="value">The value the expression reads.</param>
    public static LocalizedArgument Unnamed(object? value)
        => new(string.Empty, value);
}

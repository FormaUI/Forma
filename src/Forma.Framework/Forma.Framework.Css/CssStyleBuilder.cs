namespace Forma.Framework.Css;

/// <summary>Collects the declarations an element carries and renders them as one style attribute value.</summary>
public sealed class CssStyleBuilder
{
    private readonly List<string> _declarations = [];

    /// <summary>Whether no declaration has been added.</summary>
    public bool IsEmpty
        => _declarations.Count == 0;

    /// <summary>Adds a declaration for a property; a blank value adds nothing.</summary>
    public CssStyleBuilder Add(string property, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(property);

        if (!string.IsNullOrWhiteSpace(value))
        {
            _declarations.Add($"{property}: {value.Trim()};");
        }

        return this;
    }

    /// <summary>Adds a declaration for a property when the condition holds.</summary>
    public CssStyleBuilder Add(string property, string? value, bool condition)
        => condition ? Add(property, value) : this;

    /// <summary>Adds a declaration for a property measured as a length; no length adds nothing.</summary>
    public CssStyleBuilder Add(string property, CssLength? length)
        => Add(property, length?.ToString());

    /// <summary>Adds declarations written as CSS text, each terminated; blank text adds nothing.</summary>
    public CssStyleBuilder Add(string? declarations)
    {
        if (string.IsNullOrWhiteSpace(declarations))
        {
            return this;
        }

        var trimmed = declarations.Trim().TrimEnd(';').TrimEnd();

        if (trimmed.Length > 0)
        {
            _declarations.Add($"{trimmed};");
        }

        return this;
    }

    /// <summary>Removes every declaration added.</summary>
    public void Clear()
        => _declarations.Clear();

    /// <summary>The declarations as one attribute value, each terminated and separated by spaces; empty when none was added.</summary>
    public override string ToString()
        => string.Join(' ', _declarations);

    /// <summary>The declarations as one attribute value, or null when none was added.</summary>
    public string? ToStringOrNull()
        => IsEmpty ? null : ToString();
}

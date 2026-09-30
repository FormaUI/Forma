namespace Forma.Framework.Css;

/// <summary>Collects the classes an element carries and renders them as one class attribute value.</summary>
public sealed class CssClassBuilder
{
    private readonly List<string> _classes = [];

    /// <summary>Whether no class has been added.</summary>
    public bool IsEmpty
        => _classes.Count == 0;

    /// <summary>Adds a class.</summary>
    public CssClassBuilder Add(string? className)
    {
        if (!string.IsNullOrWhiteSpace(className))
        {
            _classes.Add(className);
        }

        return this;
    }

    /// <summary>Adds a class when the condition holds.</summary>
    public CssClassBuilder Add(string? className, bool condition)
        => condition ? Add(className) : this;

    /// <summary>Removes every class added.</summary>
    public void Clear()
        => _classes.Clear();

    /// <inheritdoc />
    public override string ToString()
        => string.Join(' ', _classes);

    /// <summary>The classes as one attribute value, or null when none was added.</summary>
    public string? ToStringOrNull()
        => IsEmpty ? null : ToString();
}

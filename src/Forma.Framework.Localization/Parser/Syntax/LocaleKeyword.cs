namespace Forma.Framework.Localization.Parser.Syntax;

/// <summary>A keyword an expression may name, and the field names it defines.</summary>
public sealed class LocaleKeyword
{
    private readonly string[] _fieldNames;

    /// <summary>Creates a keyword whose fields carry the given names, or any name at all when none are given.</summary>
    /// <param name="name">The word written after the argument index.</param>
    /// <param name="fieldNames">The names its fields may use.</param>
    public LocaleKeyword(string name, params string[] fieldNames)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(fieldNames);

        foreach (var fieldName in fieldNames)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName, nameof(fieldNames));
        }

        if (fieldNames.Distinct(StringComparer.Ordinal).Count() != fieldNames.Length)
        {
            throw new ArgumentException("A field name is repeated.", nameof(fieldNames));
        }

        Name = name;
        _fieldNames = fieldNames;
    }

    /// <summary>The word written after the argument index.</summary>
    public string Name { get; }

    /// <summary>The names its fields may use, empty when it accepts any name.</summary>
    public ReadOnlySpan<string> FieldNames => _fieldNames;
}

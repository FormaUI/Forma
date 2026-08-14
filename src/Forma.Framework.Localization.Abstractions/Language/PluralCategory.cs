namespace Forma.Framework.Localization.Language;

/// <summary>The CLDR plural categories a language can distinguish.</summary>
public enum PluralCategory
{
    /// <summary>The form a language uses for none.</summary>
    Zero,

    /// <summary>The form a language uses for one.</summary>
    One,

    /// <summary>The form a language uses for a pair.</summary>
    Two,

    /// <summary>The form a language uses for a small count.</summary>
    Few,

    /// <summary>The form a language uses for a large count.</summary>
    Many,

    /// <summary>The form a language uses when no other category applies.</summary>
    Other,
}

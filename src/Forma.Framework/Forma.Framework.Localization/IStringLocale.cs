namespace Forma.Framework.Localization;

/// <summary>Provides translated strings for one resource base name.</summary>
public interface IStringLocale
{
    /// <summary>The translation for a key, falling back to the key itself.</summary>
    /// <param name="name">The key to look up, which is the English source text.</param>
    LocalizedText this[string name] { get; }

    /// <summary>The translation for a key, formatted with values paired to the key's holes by position.</summary>
    /// <param name="name">The key to look up, which is the English source text.</param>
    /// <param name="values">One value per distinct hole, in the order the holes first appear in the key.</param>
    /// <remarks>
    /// A different number of values throws. A key naming no hole takes a single value, which is what an
    /// expression written without a name reads. A value stays <see langword="object"/> rather than text,
    /// so a specifier written in the translation, such as <c>{count:N0}</c>, still reaches it.
    /// </remarks>
    LocalizedText this[string name, params ReadOnlySpan<object?> values] { get; }

    /// <summary>The translated format for a key, with plural forms resolved and every hole left open.</summary>
    /// <param name="name">The key to look up, which is the English source text.</param>
    LocalizedPattern Resolve(string name);

    /// <summary>The translated format for a key, with values paired to its holes by position.</summary>
    /// <param name="name">The key to look up, which is the English source text.</param>
    /// <param name="values">One value per distinct hole, in the order the holes first appear in the key.</param>
    /// <remarks>A value stays <see langword="object"/>, so a hole can carry rendered content rather than text.</remarks>
    LocalizedPattern Resolve(string name, params object?[] values);

    /// <summary>Every translation available for the current culture.</summary>
    /// <param name="includeParentCultures">Whether to include translations inherited from parent cultures.</param>
    IEnumerable<LocalizedText> GetAllStrings(bool includeParentCultures);
}

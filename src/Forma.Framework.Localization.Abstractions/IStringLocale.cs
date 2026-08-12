namespace Forma.Framework.Localization;

/// <summary>Provides translated strings for one resource base name.</summary>
public interface IStringLocale
{
    /// <summary>The translation for a key, falling back to the key itself.</summary>
    /// <param name="name">The key to look up, which is the English source text.</param>
    LocalizedText this[string name] { get; }

    /// <summary>The translation for a key, formatted with the supplied arguments.</summary>
    /// <param name="name">The key to look up, which is the English source text.</param>
    /// <param name="arguments">The values the holes name, read during the call and not retained.</param>
    /// <remarks>
    /// Takes a span because the arguments are consumed before this returns, which lets the compiler keep them
    /// off the heap; <see cref="Resolve(string, object?[])"/> takes an array for the opposite reason. The values
    /// stay <see langword="object"/> rather than text so that a format specifier written in the translation,
    /// such as <c>{0:N0}</c>, still reaches the value — a specifier is ignored once an argument is a string.
    /// </remarks>
    LocalizedText this[string name, params ReadOnlySpan<object?> arguments] { get; }

    /// <summary>The translation for an interpolated string, whose holes supply the arguments.</summary>
    /// <param name="handler">The interpolated string, whose literal text rebuilds the key.</param>
    LocalizedText this[LocalizedFormatHandler handler] { get; }

    /// <summary>The translated format for a key, with plural forms resolved and holes left open.</summary>
    /// <param name="name">The key to look up, which is the English source text.</param>
    /// <param name="arguments">The values the holes name, retained by the returned pattern.</param>
    /// <remarks>
    /// Takes an array rather than the span the indexer takes, because <see cref="LocalizedPattern"/> holds the
    /// arguments after this returns and a <see cref="ReadOnlySpan{T}"/> cannot be stored in a field, so a span
    /// would only force a copy back into an array. The values stay <see langword="object"/> so a hole can carry
    /// rendered content instead of text, which is what lets a caller render holes without trusting markup.
    /// </remarks>
    LocalizedPattern Resolve(string name, params object?[] arguments);

    /// <summary>The translated format for an interpolated string, with plural forms resolved and holes left open.</summary>
    /// <param name="handler">The interpolated string, whose literal text rebuilds the key.</param>
    LocalizedPattern Resolve(LocalizedFormatHandler handler);

    /// <summary>Every translation available for the current culture.</summary>
    /// <param name="includeParentCultures">Whether to include translations inherited from parent cultures.</param>
    IEnumerable<LocalizedText> GetAllStrings(bool includeParentCultures);
}

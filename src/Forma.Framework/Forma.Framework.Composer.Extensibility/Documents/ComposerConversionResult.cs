namespace Forma.Framework.Composer.Documents;

/// <summary>The outcome of a conversion, and what the target format could not carry.</summary>
public sealed record ComposerConversionResult
{
    /// <summary>Whether the conversion carried the whole document.</summary>
    public required bool Succeeded { get; init; }

    /// <summary>The converted content.</summary>
    public string? Content { get; init; }

    /// <summary>The features the target format cannot represent.</summary>
    public IReadOnlyList<string> UnsupportedFeatures { get; init; } = [];

    /// <summary>A result carrying the whole document.</summary>
    public static ComposerConversionResult Success(string content)
        => new() { Succeeded = true, Content = content };

    /// <summary>A refusal naming what the target format cannot represent.</summary>
    public static ComposerConversionResult Refused(IReadOnlyList<string> unsupportedFeatures)
        => new() { Succeeded = false, UnsupportedFeatures = unsupportedFeatures };
}

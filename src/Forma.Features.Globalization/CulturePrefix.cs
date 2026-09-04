namespace Forma.Features.Globalization;

/// <summary>Where a culture token appears in a URL.</summary>
public enum CulturePrefix
{
    /// <summary>Every culture is addressed by a token, the default included.</summary>
    Always,

    /// <summary>The default culture is addressed without a token; every other culture carries one.</summary>
    ExceptDefault
}

namespace Forma.Framework.Globalization;

/// <summary>Where a culture token appears in a URL.</summary>
public enum CulturePrefix
{
    /// <summary>Every culture is addressed by a token, the default included.</summary>
    Always,

    /// <summary>Every culture except the default is addressed by a token.</summary>
    ExceptDefault
}

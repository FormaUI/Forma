namespace Forma.Framework.Sanitization;

/// <summary>What a markup policy is being asked to permit.</summary>
public enum HtmlSubject
{
    /// <summary>An element, by name.</summary>
    Element,

    /// <summary>The content an element encloses.</summary>
    ElementContent,

    /// <summary>An attribute, by name, owned by the element naming it.</summary>
    Attribute,

    /// <summary>An inline style property, by name.</summary>
    StyleProperty,

    /// <summary>A whole inline style declaration.</summary>
    StyleDeclaration,

    /// <summary>An address an attribute carries.</summary>
    Url,
}

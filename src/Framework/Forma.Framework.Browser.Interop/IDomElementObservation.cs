namespace Forma.Framework.Browser.Interop;

using Forma.Framework.Browser.Interop.Focus;
using Forma.Framework.Browser.Interop.Scrolling;
using Forma.Framework.Browser.Interop.Text;

using Microsoft.AspNetCore.Components;

/// <summary>An element as an owner observes it.</summary>
public interface IDomElementObservation
{
    /// <summary>The element.</summary>
    ElementReference Element { get; }

    /// <summary>The element's scrolling, as an owner observes it.</summary>
    IDomScrollObservation Scroll { get; }

    /// <summary>Whether the element is in view of its root, as an owner observes it.</summary>
    IDomIntersectionObservation Intersection { get; }

    /// <summary>The element's focus, as an owner observes it.</summary>
    IDomFocusObservation Focus { get; }

    /// <summary>The element's text entry, as an owner observes it.</summary>
    IDomTextObservation Text { get; }
}

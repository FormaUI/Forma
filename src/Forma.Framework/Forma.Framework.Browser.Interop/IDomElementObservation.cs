namespace Forma.Framework.Browser.Interop;

using Forma.Framework.Browser.Interop.Scrolling;

using Microsoft.AspNetCore.Components;

/// <summary>An element as an owner observes it.</summary>
public interface IDomElementObservation
{
    /// <summary>The element.</summary>
    ElementReference Element { get; }

    /// <summary>The element's scrolling, as an owner observes it.</summary>
    IDomScrollObservation Scroll { get; }
}

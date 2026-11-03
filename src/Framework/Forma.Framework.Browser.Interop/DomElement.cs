namespace Forma.Framework.Browser.Interop;

using Forma.Framework.Browser.Interop.Focus;
using Forma.Framework.Browser.Interop.Scrolling;
using Forma.Framework.Browser.Interop.Text;

using Microsoft.AspNetCore.Components;

/// <summary>An element an owner observes and can act on.</summary>
public sealed class DomElement : IDomElementObservation
{
    private readonly DomWatcher _watcher;
    private readonly DomEngine? _engine;

    internal DomElement(DomWatcher watcher, DomEngine? engine, ElementReference element)
    {
        _watcher = watcher;
        _engine = engine;
        Element = element;
    }

    /// <inheritdoc />
    public ElementReference Element { get; }

    /// <summary>The element's scrolling.</summary>
    public DomScroll Scroll
        => field ??= new DomScroll(_watcher, _engine, Element);

    /// <summary>Whether the element is in view of its root.</summary>
    public DomIntersection Intersection
        => field ??= new DomIntersection(_watcher, Element);

    /// <summary>The element's focus.</summary>
    public DomFocus Focus
        => field ??= new DomFocus(_watcher, _engine, Element);

    /// <summary>The element's text entry, where it is a text field.</summary>
    public DomText Text
        => field ??= new DomText(_watcher, _engine, Element);

    /// <inheritdoc />
    IDomScrollObservation IDomElementObservation.Scroll
        => Scroll;

    /// <inheritdoc />
    IDomIntersectionObservation IDomElementObservation.Intersection
        => Intersection;

    /// <inheritdoc />
    IDomFocusObservation IDomElementObservation.Focus
        => Focus;
}

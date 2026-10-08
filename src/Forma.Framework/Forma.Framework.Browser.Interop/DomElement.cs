namespace Forma.Framework.Browser.Interop;

using Forma.Framework.Browser.Interop.Scrolling;

using Microsoft.AspNetCore.Components;

/// <summary>An element an owner observes and acts on.</summary>
/// <remarks>An element got from a watcher observes alone; acting on it needs one got from an engine.</remarks>
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

    /// <inheritdoc />
    IDomScrollObservation IDomElementObservation.Scroll
        => Scroll;

    /// <inheritdoc />
    IDomIntersectionObservation IDomElementObservation.Intersection
        => Intersection;
}

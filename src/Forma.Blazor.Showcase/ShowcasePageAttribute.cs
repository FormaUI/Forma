namespace Forma.Blazor.Showcase;

/// <summary>Marks a routed component as a Showcase page and names the component it demonstrates.</summary>
/// <param name="title">The component the page demonstrates.</param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ShowcasePageAttribute(string title) : Attribute
{
    /// <summary>The component the page demonstrates.</summary>
    public string Title { get; } = title;
}

namespace Forma.UI.Blazor.Showcase;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ShowcasePageAttribute(string title) : Attribute
{
    public string Title { get; } = title;
}

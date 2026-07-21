namespace Site.UI.Showcase;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ShowcasePageAttribute(string title) : Attribute
{
    public string Title { get; } = title;
}

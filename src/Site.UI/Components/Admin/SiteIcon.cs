namespace Site.UI.Components.Admin;

public readonly record struct SiteIcon(string CssClass)
{
    public static SiteIcon FontAwesome(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new($"fa-solid fa-{name}");
    }

    public static SiteIcon FontAwesomeRegular(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new($"fa-regular fa-{name}");
    }

    public static SiteIcon From(string cssClass)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cssClass);
        return new(cssClass);
    }
}

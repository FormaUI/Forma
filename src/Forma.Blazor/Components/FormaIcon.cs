namespace Forma.Blazor.Components;

public readonly record struct FormaIcon(string CssClass)
{
    public static FormaIcon FontAwesome(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new($"fa-solid fa-{name}");
    }

    public static FormaIcon FontAwesomeRegular(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new($"fa-regular fa-{name}");
    }

    public static FormaIcon From(string cssClass)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cssClass);
        return new(cssClass);
    }
}

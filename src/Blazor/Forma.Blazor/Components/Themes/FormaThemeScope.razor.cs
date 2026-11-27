namespace Forma.Blazor.Components.Themes;

using Forma.Framework.Css;
using Forma.Framework.Themes;

using Microsoft.AspNetCore.Components;

/// <summary>A part of the page drawn in a theme, a color scheme or both of its own, whatever its surroundings'.</summary>
public sealed partial class FormaThemeScope : FormaElementBase
{
    /// <summary>The theme the scope's content draws in, as <c>data-forma-theme</c> names it; its surroundings' where unset.</summary>
    [Parameter] public string? Theme { get; set; }

    /// <summary>The color scheme the scope's content draws in; its surroundings' where unset.</summary>
    [Parameter] public ColorSchemePreference? Scheme { get; set; }

    /// <summary>The content drawn in the scope's theme and scheme.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private string? SchemeValue
        => Scheme is { } scheme ? ThemeAttributes.GetSchemeValue(scheme) : null;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Theme is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(Theme);
        }

        if (Scheme is { } scheme && !Enum.IsDefined(scheme))
        {
            throw new ArgumentOutOfRangeException(nameof(Scheme), scheme, null);
        }
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-theme-scope");
}

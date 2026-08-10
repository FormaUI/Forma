namespace Forma.UI.Blazor.Showcase.Components.Pages.Metadata;

using Forma.UI.Globalization;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

public sealed partial class FormaPageLocalizationPage : ComponentBase
{
    private string _relative = string.Empty;

    [Inject] private IStringLocalizer<FormaPageLocalizationPage> L { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Inject] private CultureRouter Router { get; set; } = default!;

    private string Emitted
        => string.Join('\n', Router.Routes
            .Select(route => Tag("alternate", route.HrefLang, UrlFor(route)))
            .Prepend(Tag("canonical", null, UrlFor(Router.CurrentRoute)))
            .Append(Tag("alternate", "x-default", UrlFor(Router.DefaultRoute))));

    protected override void OnParametersSet()
        => _relative = Navigation.ToBaseRelativePath(Navigation.Uri);

    private static string Tag(string rel, string? hrefLang, string url)
        => hrefLang is null
            ? $"""<link rel="{rel}" href="{url}" />"""
            : $"""<link rel="{rel}" hreflang="{hrefLang}" href="{url}" />""";

    private string UrlFor(CultureRoute route)
        => Navigation.BaseUri.TrimEnd('/') + Router.GetUrl(route, _relative);
}

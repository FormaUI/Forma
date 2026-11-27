# Forma.Framework.Themes.Blazor.Server

The server's half of Forma's theme system. It resolves the theme choice each request starts in, so a server-rendered page
is drawn in the reader's theme and scheme from its first paint, keeps the browser's record of the choice beside a host's
store, and takes a switch a statically rendered picker posts, as the
[theme design](https://github.com/FormaUI/Forma/blob/main/docs/theme-design.md#the-choice-a-request-starts-in) sets out.

## Install

```pwsh
dotnet add package formaui-net.Framework.Themes.Blazor.Server --prerelease
```

`AddFormaServerIntegration()`, in `formaui-net.Blazor.Server`, registers it ahead of the component library's own
registration, so a request's scope draws the request's choice. A host that registers the theme system itself calls
`AddFormaThemeServerIntegration()` ahead of `AddFormaUIServices()`.

## Setting it up

The pipeline resolves each request's choice before its page renders, and maps the endpoint a statically rendered
picker posts a switch to:

```csharp
var app = builder.Build();

app.UseFormaThemes();
app.UseRouting();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapFormaThemes();
```

The host's `App` writes the choice on `<html>`, as it writes `lang` and `dir`, and `FormaPageHead` links every
registered theme's stylesheet and publishes the theme marker the page reads:

```razor
@using Forma.Framework.Themes
@inject ThemeRegistry Themes
@inject IThemeState ThemeState

<!DOCTYPE html>
<html lang="en" @attributes="ThemeAttributes.Of(Themes, ThemeState.Choice)">
```

## The choice a request starts in

A request starts in the first of these that names a registered theme:

1. the choice the host gives the request, `ServerIntegrationOptions.GivenThemeChoice`;
2. the host's store's choice, an `IThemePreferenceStore` the host registers, where its revision is greater than the one
   the browser's record agreed with, or the browser has no record;
3. the browser's record;
4. the registration's default.

A choice naming a theme that isn't registered starts in the default theme in its scheme preference, and a response that
starts in the store's choice writes it into the record with the store's revision. A store that fails to read leaves the
request in the record or the default, and writes a warning.

The record is the cookie `__Host-forma-theme` over HTTPS, so only the page's own origin sets it, and `forma-theme` over
plain HTTP, as a development host serves, where a browser refuses a secure cookie.

## A choice given to a request

A host gives a request its choice where it registers the server's half, for a page drawn in a choice that isn't the
reader's, such as a preview in another theme:

```csharp
builder.AddFormaServerIntegration(options =>
{
    options.GivenThemeChoice = context => context.Request.Query.ContainsKey("preview")
        ? new ThemeChoice("harbor", ColorSchemePreference.Dark)
        : null;
});
```

The request starts in that choice ahead of the store and the record, and its page keeps nothing: its response writes no
record and reads no store, and its theme marker says so.

## The switch endpoint

`MapFormaThemes()` takes a switch at `/_forma/themes`. The form posts `theme`, a registered theme's name, `scheme`,
`system`, `light` or `dark`, and `returnUrl`, an address of the application's own, with the antiforgery token the
application validates. The endpoint keeps the choice in the store first, one revision past the latest it knows of, then
in the record with that revision, and sends the reader back. A store that fails leaves the record holding the choice
beside the earlier revision, so the browser draws the new choice and the store keeps its earlier one. A form naming no
registered theme or no scheme preference is refused with 400, and a return address that isn't the application's own
sends the reader to its root.

## Caching pages

A page a host caches for readers who aren't signed in is cached once per choice, since the choice is written into the
page:

```csharp
builder.Services.AddOutputCache(options => options.AddBasePolicy(policy => policy.VaryByFormaTheme()));
```

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).

# Forma.Blazor.WebView

The WebView half of Forma's component library: the registration a Blazor Hybrid application makes, in
WPF, Windows Forms or MAUI on Windows, which registers what the components need in a web view and the one
culture the application keeps across its web views.

## Install

```pwsh
dotnet add package formaui-net.Blazor.WebView
```

It brings `formaui-net.Blazor` and the Framework packages the components build on.

## Register

Beside the platform's own registrar, `AddWpfBlazorWebView()`, `AddWindowsFormsBlazorWebView()` or
`AddMauiBlazorWebView()`, in either order:

```csharp
using Forma.Blazor;

services.AddWpfBlazorWebView();
services.AddFormaUIWebView();
```

`AddFormaUIWebView()` registers what `AddFormaUI()` registers for every render mode, each only where
nothing is registered for it yet, and the culture's WebView half,
[`Forma.Framework.Culture.Blazor.WebView`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Culture.Blazor.WebView/README.md):
one culture state and one switch for the application, which every web view follows.
`UIWebViewOptions.InputRouting` sets the application's input policy:

```csharp
services.AddFormaUIWebView(options => options.InputRouting = new InputRouterOptions { ... });
```

`UIWebViewOptions.CultureArrangement` gives each web view a culture of its own in place of the application's
one, each web view switching alone:

```csharp
services.AddFormaUIWebView(options => options.CultureArrangement = CultureArrangement.Independent);
```

## The page

The application's `wwwroot/index.html` links Forma's stylesheets, in the order and from the place the
[library's README](https://github.com/FormaUI/Forma/blob/main/README.md#styling-and-theming) sets out, then
its own scoped styles and `_framework/blazor.webview.js`, as a standalone WebAssembly app's page does:

```html
<link rel="stylesheet" href="_content/Forma.Blazor/forma-tokens.css" />
<link rel="stylesheet" href="_content/Forma.Blazor/forma-reset.css" />
<link rel="stylesheet" href="_content/Forma.Blazor/forma-base.css" />
<link rel="stylesheet" href="_content/Forma.Blazor/forma-input.css" />
<link rel="stylesheet" href="MyApp.styles.css" />
```

A MAUI application's page loads the script with `autostart="false"`, leaving Blazor's start to MAUI, which
starts it once the web view's channel to the application is in place.

Each web view's document takes the application's culture from its first Forma component, so the page
carries no culture marker of its own.

## The application

Every web view resolves from the application's one service provider, so they share its culture, and a WPF
application disposes a web view as its native window closes, since WPF disposes no control on its own. The
[library's README](https://github.com/FormaUI/Forma/blob/main/README.md#blazor-hybrid) sets each host up in full,
its native user interface and the culture it keeps from one run to the next among it.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).

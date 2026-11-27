# Forma.Framework.Themes.Blazor.WebAssembly

What Forma's theme system needs from a WebAssembly client: the runtime's theme state, registered with the page's theme
coordinator as the runtime starts, before it renders, and the switch that requests through the coordinator, so a
WebAssembly component switches the page's theme as a circuit's does.

## Install

```pwsh
dotnet add package formaui-net.Framework.Themes.Blazor.WebAssembly --prerelease
```

`AddFormaWebAssemblyIntegration()`, in `formaui-net.Blazor.WebAssembly`, registers it ahead of the component library's
own registration. A client that registers the theme system itself calls `AddFormaThemeWebAssemblyIntegration()` ahead of
`AddFormaUIServices()`.

A client registers the same themes its server registers, with `AddFormaThemes`, since the runtime holds only a choice
naming a theme it registers.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).

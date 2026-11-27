# Forma.Framework.Themes.Browser.Interop

The page's theme coordinator. One per document, it orders every theme switch on the page, keeps the accepted choice in
the browser's record and in a host's store in order, and delivers it to the theme state of every runtime on the page,
each Interactive Server circuit and WebAssembly runtime, so they converge on one choice, as the
[theme design](https://github.com/FormaUI/Forma/blob/main/docs/theme-design.md#switching-in-place) sets out. It is built
on the switch coordinator the culture coordinator shares.

## Install

```pwsh
dotnet add package formaui-net.Framework.Themes.Browser.Interop --prerelease
```

`AddFormaServerIntegration()` and `AddFormaWebAssemblyIntegration()` register it, with each runtime's registration
with the coordinator and the switch that requests through it.

## Switching

A component switches the page's theme through `IThemeSwitch`, in any runtime, and reads the choice in force through
`IThemeState`, which tells its subscribers of each change:

```razor
@inject IThemeSwitch ThemeSwitch

<button type="button" @onclick="@(() => ThemeSwitch.SwitchAsync(new ThemeChoice("harbor", ColorSchemePreference.Dark)))">
    Harbor, dark
</button>
```

The switch changes the page in place, in every runtime, with no load. Its task completes once these hold:

- the switch was accepted;
- the browser's record holds it, where the page keeps the reader's choice;
- the runtime that requested it applied it;
- the host's store holds it or a newer choice, where the host registered an `IThemePreferenceStore`.

## What a switch refuses

- **A theme that isn't selectable,** one that isn't registered or whose stylesheet didn't load on the page, throws
  `InvalidOperationException` before anything changes.
- **A runtime without a registration,** as in a static render, which switches with a request, throws
  `InvalidOperationException`.
- **A record the coordinator can't read back** fails the switch, changing nothing.
- **A switch a later one requested through the same runtime overtook** completes as canceled.
- **A store's write that fails** after the switch was accepted fails the switch with the store's exception, the choice
  staying accepted on the page and in the record.

## The store's writes

The store is written after a switch is accepted, so the page changes at once:

- **in the order the choices were accepted,** one at a time, by the runtime that requested the switch, or another
  registered with a store where that one has gone;
- **the latest waiting,** so a newer switch takes the place of a write that hasn't started, and the switch it replaced
  completes once the newer write does;
- **the record following the store,** each completed write's revision written beside the choice accepted last.

A page given its choice by the host keeps nothing: each switch is accepted at once, writing neither the record nor the
store.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).

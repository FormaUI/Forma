# Forma.Framework.Localization.StringLocalizerAdapter

Forma's localization behind Microsoft's `IStringLocalizer`. Code written against `IStringLocalizer<T>` and
`IStringLocalizerFactory` reads Forma's locale files: the culture folders and packs, the holes and the plural
forms of
[`Forma.Framework.Localization`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Localization/README.md).

## Install and register

```pwsh
dotnet add package formaui-net.Framework.Localization.StringLocalizerAdapter --prerelease
dotnet add package formaui-net.Framework.Localization.Json --prerelease
```

```csharp
using Forma.Framework.Localization.Json;
using Forma.Framework.Localization.StringLocalizerAdapter;

builder.Services.AddJsonLocalization();
builder.Services.AddFormaStringLocalizerAdapter();
```

`AddFormaStringLocalizerAdapter` registers an `IStringLocalizerFactory` over the registered `IStringLocaleFactory`,
a singleton, and `IStringLocalizer<T>`, each with `TryAdd`, so a factory registered first is the one used. Call it
in place of Microsoft's `AddLocalization()`, or before it. A localizer reads the thread's UI culture and formatting
culture at each lookup, as Microsoft's does, so one instance serves every user.

## Reading strings

```csharp
public sealed class Checkout(IStringLocalizer<Checkout> localizer)
{
    public string Title => localizer["Checkout"];
    public string Total(decimal amount) => localizer["Total: {0:C}", amount];
    public string Items(int count) => localizer["[$ plural; one: # item; other: # items; $]", count];
}
```

The key decides how a string is filled, never the text a culture gives it:

| Key | Filled by |
| --- | --- |
| Names a Forma hole, `{Name}`, or holds an expression, `[$ … $]` | Forma, which pairs the arguments by position with the key's holes and the arguments its expressions name |
| Holds a composite placeholder, `{0}`, or neither | `string.Format` in the formatting culture, as Microsoft's localizer fills it |

One formatter runs over a string, so a doubled brace, `{{`, is an escape resolved once. A key read without
arguments returns a composite string's text as written, unformatted. A key mixing `{0}` with a Forma hole or
expression throws `FormatException`, and so does a text that uses the syntax its key doesn't, such as `{Name}`
in the text for `Hello {0}`.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).

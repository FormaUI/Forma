# Forma.AddOns.Emojis.Twemoji

The [Twemoji](https://github.com/jdecked/twemoji) artwork and emoji catalog for `Forma.Blazor`.
With it, `FormaEmoji` draws every emoji the same way on every device, and `FormaEmojiPicker` browses
the emoji by group and finds them by name and keyword.

## Install and register

```pwsh
dotnet add package formaui-net.AddOns.Emojis.Twemoji
```

```csharp
using Forma.Framework.Emojis.Twemoji;

builder.Services.AddFormaTwemojiPack();
builder.AddFormaUIServer();
```

`AddFormaTwemojiPack` registers `TwemojiEmojiProvider` as the `IEmojiProvider` and
`TwemojiEmojiCatalog` as the `IEmojiCatalog`, both singletons, each only where nothing is registered
for it yet. Call it before `AddFormaUIServer`, which registers the platform-font provider the same way.

The components then draw from the pack:

```razor
<p>The stage winner <FormaEmoji Emoji="@("🏆")" /> took the mountains classification as well.</p>

<FormaEmojiPicker OnSelect="Append" />
<p>@_reaction</p>

@code {
    private string _reaction = string.Empty;

    private void Append(string emoji) => _reaction += emoji;
}
```

## What it covers

The catalog holds the fully qualified emoji of every group in `FormaEmojiPicker.DefaultGroups`. Each
of them draws from the pack's sprite sheets; a skin-toned variant, or an emoji from a newer Unicode
release, draws in the reader's own font. An unqualified spelling such as "⚠" draws as its fully
qualified "⚠️".

## Artwork and licenses

The graphics are Twemoji, copyright 2020 Twitter, Inc and other contributors and copyright 2023 the
Twemoji contributors, used under [CC-BY 4.0](https://creativecommons.org/licenses/by/4.0/), whose
attribution travels with the artwork into every application that ships it. The names and search
keywords come from the [Unicode CLDR annotations](https://cldr.unicode.org), used under the
[Unicode License](https://www.unicode.org/license.txt). Both notices are in
[`THIRD-PARTY-NOTICES.md`](https://github.com/FormaUI/Forma/blob/main/src/Forma.AddOns/Forma.AddOns.Emojis.Twemoji/THIRD-PARTY-NOTICES.md),
which ships inside this package.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).

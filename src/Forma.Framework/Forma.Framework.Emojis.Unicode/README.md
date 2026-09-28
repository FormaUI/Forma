# Forma.Framework.Emojis.Unicode

The default emoji provider for `Forma.Blazor`. It implements `IEmojiProvider` from
`Forma.Framework.Emojis` by drawing every emoji with the reader's own platform font, so
`FormaEmoji` renders without any artwork installed.

## Install and register

```pwsh
dotnet add package FormaUI.NET-Framework.Emojis.Unicode
```

```csharp
using Forma.Framework.Emojis.Unicode;

builder.Services.AddFormaEmojiServices();
```

That registers `UnicodeEmojiProvider` as the `IEmojiProvider` singleton. Registration uses
`TryAdd`, so a host that registered its own provider first keeps it. `Forma.Blazor` calls
`AddFormaEmojiServices` from `AddFormaUI`, so a host of the components registers nothing here
itself; the call is for a host consuming the contract on its own.

## What it provides

`UnicodeEmojiProvider.GetArtwork` returns `EmojiArtwork.Unicode` for any emoji, known or not:

```csharp
using Forma.Framework.Emojis;
using Forma.Framework.Emojis.Unicode;

IEmojiProvider provider = new UnicodeEmojiProvider();

var artwork = provider.GetArtwork("😀");   // EmojiArtwork.Unicode
```

The reader's font decides what it can draw, so the same emoji looks different per device rather
than the same everywhere.

## Without a catalog

This package registers no `IEmojiCatalog`, so `FormaEmojiPicker` shows its empty state under it.
An artwork pack such as `Forma.AddOns.Emojis.Twemoji` supplies both a provider and a catalog;
registered before `AddFormaUI`, it replaces this provider and fills the picker.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).

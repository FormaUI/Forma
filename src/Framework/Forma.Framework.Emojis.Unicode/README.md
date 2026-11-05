# Forma.Framework.Emojis.Unicode

The default emoji provider for Forma. `UnicodeEmojiProvider` implements `IEmojiProvider` from
`Forma.Framework.Emojis` by drawing every emoji with the reader's own emoji font, so each emoji
renders straight from text, in the style of the reader's platform.

## Install and register

```pwsh
dotnet add package formaui-net.Framework.Emojis.Unicode --prerelease
```

```csharp
using Forma.Framework.Emojis.Unicode;

builder.Services.AddFormaEmojiServices();
```

`AddFormaEmojiServices` registers `UnicodeEmojiProvider` as the `IEmojiProvider` singleton with
`TryAdd`, so a provider registered first is kept. Forma's components draw through
`UnicodeEmojiProvider.Instance` where no provider is registered, so a host needs this call only to resolve
the provider itself.

## Use

`GetArtwork` returns `EmojiArtwork.Unicode` for every emoji:

```csharp
using Forma.Framework.Emojis;
using Forma.Framework.Emojis.Unicode;

IEmojiProvider provider = new UnicodeEmojiProvider();

var artwork = provider.GetArtwork("😀");   // EmojiArtwork.Unicode
```

For one artwork on every platform, and a catalog for `FormaEmojiPicker` to browse, install an
artwork pack such as
[`formaui-net.AddOns.Emojis.Twemoji`](https://github.com/FormaUI/Forma/blob/main/src/AddOns/Forma.AddOns.Emojis.Twemoji/README.md).

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).

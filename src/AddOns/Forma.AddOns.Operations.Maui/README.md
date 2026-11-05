# Forma.AddOns.Operations.Maui

What a MAUI application does for its web views on Android, iOS and Mac Catalyst, whose web views can't save a
file, print or share by themselves: the platform's share sheet takes a file Forma's components hand the reader,
a document they print and an address they share. On Windows the web view does all three itself, and the
add-on leaves them to it.

## Install and register

```pwsh
dotnet add package formaui-net.AddOns.Operations.Maui --prerelease
```

```csharp
using Forma.Framework.Browser.Interop.Operations.Maui;

builder.Services.AddMauiBlazorWebView();
builder.Services.AddFormaUIWebView();
builder.Services.AddFormaMauiOperations();
```

`AddFormaMauiOperations` registers `MauiOperations` as the `INativeOperations` Forma's components consult,
a singleton, only where nothing is registered for it yet, in any order beside `AddFormaUIWebView`.

## What the reader gets

| Component | What it does with the add-on on Android, iOS and Mac Catalyst |
| --- | --- |
| `FormaShareMenu` | Share opens the platform's share sheet with the item's address; Print hands the page, as a document of its own carrying the page's styles, to the share sheet, from which the reader prints or keeps it. |
| `FormaComposer`'s preview | Print hands the document to the share sheet in the same way; Download hands it over as the HTML file it is. |
| `FormaComposer`'s source view | Download hands the document over as the HTML file it is. |

A file is written to a folder of its own in the application's cache before the share sheet takes it.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).

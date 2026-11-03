# Forma.AddOns.Share.Targets

Ready-made share targets for `FormaShareMenu`: the social networks, Messenger and Discord, the cloud
drives, TikTok and Mastodon. Each is an `IShareTarget` on the
[`Forma.Framework.Share`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Share/README.md)
contracts with a registrar of its own, and joins the menu for the items it accepts.

## Install

```pwsh
dotnet add package formaui-net.AddOns.Share.Targets
```

## Register

A host registers the targets it wants, from the `Forma.Framework.Share.Targets` namespace.

### Networks

```csharp
using Forma.Framework.Share.Targets;

builder.Services.AddFormaShareNetworks();
```

Registers Facebook, X, WhatsApp, Telegram, Bluesky, Reddit, Snapchat, LinkedIn and Pinterest. Each
opens the network's composer in a new tab on a link or a picture that has an address; Pinterest pins
pictures.

### The host's app or channel

| Registrar | Options | Target |
| --- | --- | --- |
| `AddFormaMessengerSharing(configure)` | `MessengerShareOptions`: `AppId`, the host's Facebook app id, and `RedirectAddress`, where the dialog returns to | Opens Messenger's send dialog on a link or a picture that has an address. |
| `AddFormaDiscordSharing(webhookAddress)` or `AddFormaDiscordSharing(configure)` | `DiscordShareOptions`: `WebhookAddress`, the channel webhook's address | Posts the item's name and address into the channel. |

The `configure` delegate sets options on top of any the host binds from configuration, and the
options are validated when the application starts.

### The user's account

```csharp
using Forma.Framework.Share;
using Forma.Framework.Share.Targets;

builder.Services.AddScoped<IShareTokenSource, UserShareTokens>();
builder.Services.AddFormaDropboxSharing();
builder.Services.AddFormaGoogleDriveSharing();
builder.Services.AddFormaOneDriveSharing();
builder.Services.AddFormaTikTokSharing();
```

Each shares under the access token the host's `IShareTokenSource` answers for the target's id: Dropbox
saves the item's resource, Google Drive and OneDrive upload it, and TikTok publishes it as a video.
They take an item whose resource is at a public `http` or `https` address, which the service fetches
it from. The targets are scoped, beside the scoped token source.

```csharp
using Forma.Framework.Share;

public sealed class UserShareTokens(IUserCredentials credentials) : IShareTokenSource
{
    public ValueTask<string?> GetTokenAsync(string targetId, CancellationToken cancellationToken = default)
        => credentials.GetAccessTokenAsync(targetId, cancellationToken);
}
```

### The user's server

```csharp
using Forma.Framework.Share;
using Forma.Framework.Share.Targets;

builder.Services.AddScoped<IShareServerSource, UserShareServers>();
builder.Services.AddFormaMastodonSharing();
```

Mastodon opens the composer of the server the host's `IShareServerSource` answers, on a link or a
picture that has an address. With `IShareConnections` registered, the menu offers to connect a server
until the user names one.

## Target ids

The token and server sources are asked by these ids.

| Target | Id | Target | Id |
| --- | --- | --- | --- |
| Facebook | `facebook` | Messenger | `messenger` |
| X | `x` | Discord | `discord` |
| WhatsApp | `whatsapp` | Mastodon | `mastodon` |
| Telegram | `telegram` | Dropbox | `dropbox` |
| Bluesky | `bluesky` | Google Drive | `google-drive` |
| Reddit | `reddit` | OneDrive | `onedrive` |
| Snapchat | `snapchat` | TikTok | `tiktok` |
| LinkedIn | `linkedin` | | |
| Pinterest | `pinterest` | | |

## Example

The networks for every user, a Discord channel the host owns, and Dropbox under the user's account:

```csharp
builder.Services.AddFormaShareNetworks();
builder.Services.AddFormaDiscordSharing(new Uri("https://discord.com/api/webhooks/123/abc"));
builder.Services.AddScoped<IShareTokenSource, UserShareTokens>();
builder.Services.AddFormaDropboxSharing();
```

A file at a public address, which Discord and Dropbox accept:

```razor
<FormaShareMenu Item="@_kit" />

@code {
    private readonly ShareItem _kit = new()
    {
        Kind = ShareItemKind.File,
        Name = "kit.zip",
        Meta = "ZIP archive, 4 MB",
        Address = "https://example.com/files/kit",
        Resource = new ResourceInfo("https://example.com/files/kit.zip"),
    };
}
```

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).

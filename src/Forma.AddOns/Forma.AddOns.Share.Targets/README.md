# Forma.AddOns.Share.Targets

Ready-made share targets on the `Forma.Framework.Share` contracts: the social networks, the
messengers, the cloud drives, and a federated network, each an `IShareTarget` with its own
registrar. A host installs this package beside the contract and registers the targets it wants;
each joins `FormaShareMenu` for the items it accepts. The contract, the shells the targets stand
on, and the credential sources a host answers are documented in
[`Forma.Framework.Share`](../../Forma.Framework/Forma.Framework.Share/README.md).

## Install

```pwsh
dotnet add package Forma.AddOns.Share.Targets
```

The code lives in the `Forma.Framework.Share.Targets` namespace.

## Register

What a host supplies decides how a target is registered. A network needs nothing; a target
under the host's own app or channel takes options; a target under the user's account reads a
source the host implements.

### The networks

```csharp
using Forma.Framework.Share.Targets;

builder.Services.AddFormaShareNetworks();
```

Registers Facebook, X, WhatsApp, Telegram, Bluesky, Reddit, Snapchat, LinkedIn and Pinterest,
once for every user. Each opens the network's composer in a new tab on a link or a picture that
has an address; Pinterest pins a picture only, pointed at its media. A file or a contact reaches
none of them.

### The host's own app or channel

| Registrar | Options | What the target does |
| --- | --- | --- |
| `AddFormaMessengerSharing(configure)` | `MessengerShareOptions`: `AppId`, the host's Facebook app id, and `RedirectAddress`, where the dialog returns to. | Opens Messenger's send dialog in a new tab on a link or a picture that has an address. |
| `AddFormaDiscordSharing(webhookAddress)`, or `AddFormaDiscordSharing(configure)` | `DiscordShareOptions`: `WebhookAddress`, the channel webhook's address from Discord's channel settings. | Posts the item's name and address as a message into the channel, for an item with an address or with a resource at a public `http` or `https` address. |

Both are registered once for every user. The `configure` delegate is optional and sets the
options on top of whatever the host bound from configuration; the options are validated when the
host starts, so a missing app id or webhook address fails startup rather than the first share.

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

Each of these shares under the token the host's `IShareTokenSource` answers for the target's id,
and shares nothing when the user has connected no account. Dropbox saves the item's resource,
Google Drive and OneDrive upload it, and TikTok starts publishing it as a video; all four accept
an item of any kind whose resource lives at a public `http` or `https` address, because the
service fetches the bytes from there. A token is the user's, so the host registers its source
scoped, and the registrars register the targets scoped beside it.

### The user's server

```csharp
using Forma.Framework.Share;
using Forma.Framework.Share.Targets;

builder.Services.AddScoped<IShareServerSource, UserShareServers>();
builder.Services.AddFormaMastodonSharing();
```

Mastodon opens the composer of whichever server the host's `IShareServerSource` answers for its
id, on a link or a picture that has an address. It accepts such an item whether or not the user
has named a server, and answers no address until one is named; with `IShareConnections`
registered, the menu draws it as a Connect entry until then.

## Target ids

The token and server sources are asked by these ids.

| Target | Id |
| --- | --- |
| Facebook | `facebook` |
| X | `x` |
| WhatsApp | `whatsapp` |
| Telegram | `telegram` |
| Bluesky | `bluesky` |
| Reddit | `reddit` |
| Snapchat | `snapchat` |
| LinkedIn | `linkedin` |
| Pinterest | `pinterest` |
| Messenger | `messenger` |
| Discord | `discord` |
| Mastodon | `mastodon` |
| Dropbox | `dropbox` |
| Google Drive | `google-drive` |
| OneDrive | `onedrive` |
| TikTok | `tiktok` |

## Example

Registration offering the networks to every user, a Discord channel the host owns, and Dropbox
under the user's own account:

```csharp
using Forma.Framework.Share;
using Forma.Framework.Share.Targets;

builder.Services.AddFormaShareNetworks();
builder.Services.AddFormaDiscordSharing(new Uri("https://discord.com/api/webhooks/123/abc"));
builder.Services.AddScoped<IShareTokenSource, UserShareTokens>();
builder.Services.AddFormaDropboxSharing();
```

The token source Dropbox reads, answering from wherever the host keeps the signed-in user's
credentials, keyed by the target's id:

```csharp
using Forma.Framework.Share;

public sealed class UserShareTokens(IUserCredentials credentials) : IShareTokenSource
{
    public ValueTask<string?> GetTokenAsync(string targetId, CancellationToken cancellationToken = default)
        => credentials.GetAccessTokenAsync(targetId, cancellationToken);
}
```

The menu over a file at a public address, which Discord and Dropbox accept and the networks do
not:

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

MIT, under the repository's [`LICENSE.md`](../../../LICENSE.md).

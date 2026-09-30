# Forma.Framework.Share

The share contracts: the item a share menu acts on, the targets an item can be shared to, and the
sources a host answers a user's credentials through. A host builds its own targets on them, a
cloud drive, a chat channel, an internal vault, and each appears in `FormaShareMenu` for the items
it accepts.

## Install

```pwsh
dotnet add package FormaUI.NET-Framework.Share
```

`FormaUI.NET-Blazor` depends on this package, so a host with Forma installed already has it.
Ready-made targets for the social networks and account-backed destinations ship in
`FormaUI.NET-AddOns.Share.Targets`.

## Use it

Write a target on one of the shells, register it as an `IShareTarget`, and every
`FormaShareMenu` offers it for the items it supports.

A link target, which answers the address a network's composer opens at:

```csharp
using Forma.Framework.Icons;
using Forma.Framework.Share;

public sealed class TeamChatShare() : LinkShare("team-chat", "Team Chat", Glyph.Share)
{
    public override string? GetAddress(ShareItem item)
        => EscapeText(item) is { } text ? $"https://chat.example.com/compose?text={text}" : null;
}
```

An account target, saving a file into the user's own storage under the user's token:

```csharp
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Forma.Framework.Icons;
using Forma.Framework.Share;

public sealed class VaultShare(IShareTokenSource tokens, HttpClient? http = null)
    : AccountShare("vault", "Vault", Glyph.Folder, tokens, http)
{
    private static readonly Uri SaveEndpoint = new("https://vault.example.com/api/save");

    public override bool Supports(ShareItem item)
        => GetReachableSource(item) is not null;

    protected override async Task ShareAsync(ShareItem item, string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, SaveEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(new { name = item.Resource!.Name, url = GetReachableSource(item) });

        using var response = await Http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }
}
```

The token source it reads, answering from wherever the host keeps the signed-in user's
credentials:

```csharp
using Forma.Framework.Share;

public sealed class UserShareTokens(IUserCredentials credentials) : IShareTokenSource
{
    public ValueTask<string?> GetTokenAsync(string targetId, CancellationToken cancellationToken = default)
        => credentials.GetAccessTokenAsync(targetId, cancellationToken);
}
```

Registration, with the link target shared by every user and the account target and its token
source in the user's scope:

```csharp
using Forma.Framework.Share;

builder.Services.AddSingleton<IShareTarget, TeamChatShare>();
builder.Services.AddScoped<IShareTokenSource, UserShareTokens>();
builder.Services.AddScoped<IShareTarget, VaultShare>();
```

The menu over a file, which the vault accepts:

```razor
@using Forma.Blazor.Components.Menus
@using Forma.Framework.Resources
@using Forma.Framework.Share

<FormaShareMenu Item="_kit" />

@code {
    private readonly ShareItem _kit = new()
    {
        Kind = ShareItemKind.File,
        Name = "kit.zip",
        Meta = "ZIP archive, 4 MB",
        Resource = new ResourceInfo("https://example.com/files/kit.zip"),
    };
}
```

## The item

`ShareItem` is what a menu acts on.

| Member | What it is |
| --- | --- |
| `Kind` | A `ShareItemKind`, `Link`, `File`, `Image` or `Contact`, deciding which baseline actions apply. Defaults to `Link`. |
| `Name` | The item's name, shown in the menu's header and carried into shares. |
| `Meta` | A short line beneath the name, such as a type and size. |
| `Icon` | The icon drawn beside the name. |
| `Address` | The address the item is shared and copied as; the network and link actions need one. |
| `Resource` | The `ResourceInfo` the item downloads as; the download action needs one. |

`ShareAction` is a command added to the menu beside the baseline through its `Actions`: an `Id`,
a localized `Title`, the `Command` the menu raises through `OnCommand`, an optional `Icon`, and
`IsDisabled`.

## Targets

`IShareTarget` is one destination.

| Member | What it does |
| --- | --- |
| `Id` | The target's unique identifier, which the credential sources are asked by. |
| `Title` | The displayed title. |
| `Icon` | The icon drawn with the title. |
| `RequiresAccount` | Whether sharing goes through an account the user has connected. Defaults to `false`. |
| `Supports(item)` | Whether the target shares this item. |
| `GetAddress(item)` | The address that shares the item by navigation, opened in a new tab; `null` for a target that shares by a call. |
| `ShareAsync(item, cancellationToken)` | Shares the item by a call, reporting whether it did. |

Each shell takes the target's `id`, `title` and `icon` through its constructor:

| Shell | What it is | What a derived class writes |
| --- | --- | --- |
| `LinkShare` | A target reached by navigation, supporting every item it answers an address for. | `GetAddress`, with `EscapeAddress`, `EscapeName` and `EscapeText` to percent-encode the item for a query string. |
| `ApiShare` | A target that posts over `Http`, an `HttpClient` the host passes or one it creates and disposes. | `Supports` and `ShareAsync`, with `GetReachableSource` for the public `http` or `https` address of the item's resource. |
| `WebhookShare` | An `ApiShare` that posts a JSON payload to the webhook address the host configured. | `GetPayload`, answering `null` for an item it cannot post. |
| `AccountShare` | An `ApiShare` that shares under the current user's token from `IShareTokenSource`, reporting `false` without one. | `Supports` and the protected `ShareAsync(item, accessToken, cancellationToken)`. |

## What the host answers

A token or a server belongs to the current user, so the host implements these in the user's
context and registers them scoped.

| Type | What it answers |
| --- | --- |
| `IShareTokenSource` | `GetTokenAsync(targetId, cancellationToken)`: the user's access token for a target, or `null`. |
| `IShareServerSource` | `GetServer(targetId)`: the server the user's account for a federated target lives on, or `null`. |
| `IShareConnections` | `IsConnected(targetId)` and `ConnectAsync(targetId, cancellationToken)`, with change notification through `INotifyStateChanged`. |

`IShareConnections` is optional. When it is registered, `FormaShareMenu` draws an account target
the user has not connected as a Connect entry, and redraws when it notifies.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).

# Forma.Framework.Share

The share contracts `FormaShareMenu` reads: the item a menu acts on, a target an item can be
shared to, the actions a consumer adds beside the baseline, and the sources a host answers a
user's credentials through. A host writes its own targets on these contracts, a cloud drive, a
chat channel, an internal vault, and each joins the menu for the items it accepts.

`Forma.AddOns.Share.Targets` ships ready-made targets on the same contracts, the social networks
and the account-backed destinations, each with its own registrar. A host that wants those
installs that package beside this one.

## Install

```pwsh
dotnet add package 2Site.Forma.Framework.Share
```

The package has no registrar of its own. A host registers each `IShareTarget` it wants offered,
and the credential contracts its account-backed targets read.

## The contract

| Type | What it is |
| --- | --- |
| `IShareTarget` | A target an item can be shared to, such as a social network, a cloud drive, or a messaging service. |
| `ShareItem` | What a share menu acts on: what kind of thing it is, how it is named, where it lives, and the resource it downloads as. |
| `ShareItemKind` | The kinds an item can be: `Link`, `File`, `Image`, `Contact`. |
| `ShareAction` | One command a consumer adds to a share menu beside the baseline. |

### `IShareTarget`

| Member | What it does |
| --- | --- |
| `Id` | The target's unique identifier, which the credential contracts are asked by. |
| `Title` | The displayed title. |
| `Icon` | The icon drawn with the title. |
| `RequiresAccount` | Whether sharing goes through an account the user has connected. Defaults to `false`. |
| `Supports(item)` | Whether the target shares this item. |
| `GetAddress(item)` | The address the item is shared to through navigation, or `null` when sharing takes a call. Defaults to `null`. |
| `ShareAsync(item, cancellationToken)` | Shares the item when the target takes a call, reporting whether it did. A target that requires an account reports `false` when the user has none connected. |

A target answering an address is opened in a new tab; one answering `null` is called when chosen.

### `ShareItem`

| Member | What it is |
| --- | --- |
| `Kind` | One of `ShareItemKind`, deciding which baseline actions apply. Defaults to `ShareItemKind.Link`. |
| `Name` | The item's name, shown in the menu's header and carried into shares. |
| `Meta` | A short line beneath the name, such as a type and size. |
| `Icon` | The icon drawn beside the name. |
| `Address` | The address the item is shared and copied as; without one, no network or link action shows. |
| `Resource` | The `ResourceInfo` the item downloads as; without one, no download action shows. |

### `ShareAction`

| Member | What it is |
| --- | --- |
| `Id` | The action's unique identifier. |
| `Title` | The title, rendered as given, so the caller localizes it. |
| `Command` | The command `FormaShareMenu` raises through its `OnCommand` callback when the action is chosen. |
| `Icon` | The icon drawn with the title. |
| `IsDisabled` | Whether the action shows but cannot be chosen. |

## Shells to build a target on

Each shell takes the target's `id`, `title` and `icon` through its constructor and leaves one or
two members to the derived class.

| Type | What it is | What a derived class writes |
| --- | --- | --- |
| `LinkShare` | A target reached by navigation: a network whose composer opens at an address carrying the item. | `GetAddress`. `Supports` is true wherever an address is answered. |
| `ApiShare` | A target that posts over an `HttpClient`, its own or one the host passes. | `Supports` and `ShareAsync`. |
| `WebhookShare` | An `ApiShare` that posts the JSON payload it makes of an item to the address the host configured. | `GetPayload`, answering `null` for an item it cannot post. |
| `AccountShare` | An `ApiShare` that shares under the current user's token and shares nothing without one. | `Supports` and the protected `ShareAsync(item, accessToken, cancellationToken)`. |

`LinkShare` gives a derived class `EscapeAddress`, `EscapeName` and `EscapeText`, the item's address,
its name, and the two as one message, percent-encoded for a query string. `EscapeAddress` answers
`null` for any kind but a link or a picture, so a file or a contact reaches no link target.

`ApiShare` exposes the connection as `Http` and gives a derived class `GetReachableSource`, the
public `http` or `https` address the item's resource can be fetched from, for a destination that
fetches the file itself. It disposes the connection it created; one the host passed is the host's.

`AccountShare` sets `RequiresAccount` to `true`, asks the `IShareTokenSource` for a token under
the target's `Id` only for an item it supports, and reports `false` from `ShareAsync` when none
comes back.

## What the host answers

| Type | What it does |
| --- | --- |
| `IShareTokenSource` | `GetTokenAsync(targetId, cancellationToken)` answers the current user's access token for a target, or `null` when the user has connected none. |
| `IShareServerSource` | `GetServer(targetId)` answers the server the current user's account for a federated target lives on, or `null` when the user has named none. |
| `IShareConnections` | `IsConnected(targetId)` says whether the user has connected an account for a target, `ConnectAsync(targetId, cancellationToken)` starts connecting one, and its `INotifyStateChanged` half notifies subscribers when that changes. |

A token or a server is the user's, so the host implements these in the user's context and
registers them scoped, with the targets that read them. `IShareConnections` is optional:
`FormaShareMenu` takes it when registered, draws an account target the user has not connected
as a Connect entry, and redraws when it notifies. A connected status is advisory; the share
itself reports whether it happened.

## Example

A link target: the derived class answers the address, and the shell does the rest.

```csharp
using Forma.Framework.Icons;
using Forma.Framework.Share;

public sealed class TeamChatShare() : LinkShare("team-chat", "Team Chat", Glyph.Share)
{
    public override string? GetAddress(ShareItem item)
        => EscapeText(item) is { } text ? $"https://chat.example.com/compose?text={text}" : null;
}
```

An account target, saving a file into the user's own storage:

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
        ArgumentNullException.ThrowIfNull(item);

        using var request = new HttpRequestMessage(HttpMethod.Post, SaveEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(new { name = item.Resource!.Name, url = GetReachableSource(item) });

        using var response = await Http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }
}
```

The token source the vault reads, answering from wherever the host keeps the signed-in user's
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

The menu over a file, which the vault accepts and the link target does not:

```razor
<FormaShareMenu Item="@_kit" />

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

## License

MIT, under the repository's [`LICENSE.md`](../../../LICENSE.md).

# Forma.Framework.Media

The provider contract `FormaVideoPlayer` plays through. A player provider turns an address into a
`PlayerFrame`, what the player loads, and the player asks its providers in turn: those the host
registered, then the bundled hosting services, then any file the browser can fetch. A host
registers a provider to teach the player addresses of its own, or to serve a hosting service's
addresses its own way.

## Use it

The assembly ships inside the `FormaUI.NET-Blazor` package, with the contract in the
`Forma.Framework.Media` namespace and the bundled providers in `Forma.Framework.Media.Providers`.
The bundled providers need no registration; a host registers each of its own as an
`IPlayerProvider`, and they are asked in registration order before any bundled one:

```csharp
using Forma.Framework.Media;

builder.Services.AddScoped<IPlayerProvider, LibraryPlayerProvider>();
```

A provider returns a frame for the addresses it claims and `null` for the rest. One that resolves
the application's own video identifiers against its library:

```csharp
using Forma.Framework.Media;

public sealed class LibraryPlayerProvider(IVideoLibrary library) : IPlayerProvider
{
    private const string Scheme = "library:";

    public async ValueTask<PlayerFrame?> GetPlayerFrameAsync(string source, PlayerOptions options, CancellationToken cancellationToken)
    {
        if (!source.StartsWith(Scheme, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var video = await library.FindAsync(source[Scheme.Length..], cancellationToken);

        return video is null ? null : new NativePlayerFrame(video.Url);
    }
}
```

The player then takes that address like any other:

```razor
@using Forma.Blazor.Components.Media

<FormaVideoPlayer Source="library:stage-4-finish" Title="Stage 4 finish" />
```

## The contract

`IPlayerProvider.GetPlayerFrameAsync(source, options, cancellationToken)` returns the frame for
an address, or `null` for an address it leaves to the next provider. `PlayerOptions` carries how
the player was asked to behave:

| Member | What it is |
| --- | --- |
| `Autoplay` | Whether playback starts on its own, muted. |
| `LimitTracking` | Whether the hosting service is asked to collect as little as it offers to. On by default. |
| `PageAddress` | The address of the page the player renders on, which some services require. |

`FormaVideoPlayer` fills `Autoplay` from its own parameter and `PageAddress` from the page's base
address.

## Frames

Every frame carries `Source`, the address it stands for.

| Record | What the player loads |
| --- | --- |
| `EmbeddedPlayerFrame(Source)` | A hosting service's player page, in an `<iframe>` sized to the player's aspect ratio. |
| `NativePlayerFrame(Source)` | A file the browser plays itself, in a `<video>` sized the same way, with the player's `Captions`. |
| `HtmlPlayerFrame(Source, Html, ScriptUrl)` | Markup a hosting service's script turns into a player: the player renders `Html`, loads `ScriptUrl` once, and lets the result size itself. |

The player renders an `HtmlPlayerFrame`'s `Html` as trusted markup, so a provider encodes every
part of the address it writes into it.

## The bundled providers

Asked in this order after every registered provider:

| Provider | Claims | Returns |
| --- | --- | --- |
| `YouTubePlayerProvider` | A watch, share, embed, shorts or live address; embeds from `youtube-nocookie.com` while `LimitTracking` is on. | `EmbeddedPlayerFrame` |
| `VimeoPlayerProvider` | A watch or player address; adds `dnt=1` while `LimitTracking` is on. | `EmbeddedPlayerFrame` |
| `TwitchPlayerProvider` | A channel, video or clip address, while `PageAddress` is set, since Twitch requires the embedding host. | `EmbeddedPlayerFrame` |
| `TikTokPlayerProvider` | A video address, as markup plus TikTok's script. | `HtmlPlayerFrame` |
| `InstagramPlayerProvider` | A post, reel or video address, as markup plus Instagram's script. | `HtmlPlayerFrame` |
| `FacebookPlayerProvider` | A video, watch or reel address, as markup plus Facebook's script. | `HtmlPlayerFrame` |
| `TwitterPlayerProvider` | A post on X, as markup plus the widget script, with `data-dnt`. | `HtmlPlayerFrame` |
| `FilePlayerProvider` | A relative address, or an absolute one under `http`, `https`, `data` or `blob`. | `NativePlayerFrame` |

A registered provider that claims a service's addresses answers before that service's bundled
provider, so a host can serve its own copy of a video it keeps.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).

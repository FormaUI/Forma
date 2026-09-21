# Forma.Framework.Media

The contract `FormaVideoPlayer` plays through. A player provider turns an address into a
`PlayerFrame`, the thing the player loads, or declines the address, and the player asks a chain
of providers in turn: those the host registered, then the hosting services the assembly
recognizes, then any file the browser can fetch. A host registers an `IPlayerProvider` to teach
the player an address of its own, or to take over one a bundled service would otherwise claim.

The assembly ships inside `Forma.Blazor`; nothing is installed or registered for it
beyond a provider a host adds. Its types live in the `Forma.Framework.Media` namespace and the
bundled providers in `Forma.Framework.Media.Providers`.

## The contract

`IPlayerProvider.GetPlayerFrameAsync(source, options, cancellationToken)` returns the frame for
an address, or null when the address is not the provider's own. `PlayerOptions` is how the
caller asked the player to behave:

| Member | What it is |
| --- | --- |
| `Autoplay` | Starts playback without interaction, always muted. |
| `LimitTracking` | Asks the hosting service to collect as little as it offers to; on unless turned off. |
| `PageAddress` | The address of the page the player renders on, which some services demand the host of. |

`FormaVideoPlayer` builds the options from its own `Autoplay` parameter and the page's base
address, leaving `LimitTracking` on.

## Frames

Every frame carries `Source`, the address it stands for. The record says what the player does
with it:

| Record | What the player loads |
| --- | --- |
| `EmbeddedPlayerFrame(Source)` | A player page supplied by a hosting service, loaded in an `<iframe>`. |
| `NativePlayerFrame(Source)` | A file the browser plays itself, in a `<video>`; the one frame the player's `Captions` render on. |
| `HtmlPlayerFrame(Source, Html, ScriptUrl)` | Markup a hosting service's own script turns into a player: the player renders `Html`, loads `ScriptUrl` once, and leaves the service to size the result, with `Source` linked in the player's place until the script has drawn it. |

An embedded or markup frame is held behind a play control until the reader presses it, unless
the player's `DeferLoading` is off or `Autoplay` is on; a native frame loads at once. An
embedded or native frame is sized to the player's aspect ratio, a markup frame is left to flow.
Captions supplied alongside an embedded or markup frame are an error, and when every provider
declines an address the player shows an unavailable notice.

A provider that returns an `HtmlPlayerFrame` encodes whatever part of the address reaches its
`Html`, because the player renders that markup as trusted.

## The bundled providers

`FormaVideoPlayer` tries the bundled providers in this order once every registered provider has
declined:

| Provider | Claims | Returns |
| --- | --- | --- |
| `YouTubePlayerProvider` | A watch, share, embed, shorts or live address; embeds from `youtube-nocookie.com` while `LimitTracking` is on. | `EmbeddedPlayerFrame` |
| `VimeoPlayerProvider` | A watch or player address; sets `dnt=1` while `LimitTracking` is on. | `EmbeddedPlayerFrame` |
| `TwitchPlayerProvider` | A channel, video or clip address, only while `PageAddress` is set; Twitch loads only with the embedding host named. | `EmbeddedPlayerFrame` |
| `TikTokPlayerProvider` | A video address, as markup plus TikTok's script. | `HtmlPlayerFrame` |
| `InstagramPlayerProvider` | A post, reel or video address, as markup plus Instagram's script. | `HtmlPlayerFrame` |
| `FacebookPlayerProvider` | A video, watch or reel address, as markup plus Facebook's script. | `HtmlPlayerFrame` |
| `TwitterPlayerProvider` | A post on X, as markup plus the widget script, always with `data-dnt`. | `HtmlPlayerFrame` |
| `FilePlayerProvider` | A relative address, or an absolute one under `http`, `https`, `data` or `blob`; a scheme the browser will not fetch, such as `file:`, is declined. | `NativePlayerFrame` |

With `Autoplay` on, YouTube and Vimeo are asked to start muted in the embed's query; Twitch,
which starts by itself unless told not to, is told not to while `Autoplay` is off and to start
muted while it is on.

## Registering a provider

A host's providers are tried in registration order, before every bundled one:

```csharp
using Forma.Framework.Media;

builder.Services.AddScoped<IPlayerProvider, LibraryPlayerProvider>();
```

A provider claims what it knows and returns null for the rest. One that resolves the
application's own video identifiers against its library:

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

A registered provider that claims a service's own addresses answers before that service's
bundled provider is asked, so a host serves its own copy of a video where it keeps one.

## License

MIT, under the repository's [`LICENSE.md`](../../../LICENSE.md).

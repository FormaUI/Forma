# Forma Media Design

## Scope

`FormaVideoPlayer` and the `Forma.Framework.Media` contracts it plays through:
`IPlayerProvider`, `PlayerOptions` and `PlayerFrame`.

## Decision summary

- **A chain of providers resolves the address.** A watch link, a share link and an embed
  link all work without the caller naming the service. Providers the host registered are
  tried first, so an application can teach the player an address shape the library never
  knew, or take over one it would otherwise claim, the same way a host extends
  `FormaFileViewer`; the built-in providers follow, with `FilePlayerProvider` last because
  it accepts any fetchable address. `PlayerOptions` carries `Autoplay`, `LimitTracking`
  and the `PageAddress` of the page the player renders on, which Twitch demands the host
  of and without which it declines the address.
- **Two kinds of service, three kinds of frame.** YouTube, Vimeo and Twitch return an
  `EmbeddedPlayerFrame`, an embed address the player loads in an `<iframe>`. TikTok,
  Instagram, Facebook and X return an `HtmlPlayerFrame`, markup plus a script, which a
  module loads once and asks to scan, each service sizing its own player. Any other
  address a page can fetch falls back to a `NativePlayerFrame`, the browser's own
  `<video>`, with optional `<track>` captions; captions supplied for a service's frame are
  an error rather than silently dropped, and an address a page cannot fetch, such as
  `file://`, is refused and the player shows an unavailable notice. The player is sized
  with `aspect-ratio`, except for a service that sizes its own player, which is left to
  flow.
- **External script is confined to the services whose player is a script.** They publish
  no embed address; the iframe providers need none.
- **Nothing is fetched until the reader presses play.** `DeferLoading` holds both the
  embed address and the service script behind a play control, which `Autoplay` overrides
  and a self-hosted file never needs. A third-party frame that loads with every article is
  both a tracking surface a public site has to account for and a large fixed cost on a
  mobile connection.
- **Ask the service for privacy where it offers it.** `LimitTracking`, which the player
  leaves on, embeds from `youtube-nocookie.com` and sets `dnt=1` on Vimeo; X always
  carries `data-dnt="true"`.
- **The still is the host's to supply.** `Placeholder` takes an image the host serves from
  its own origin, and stands in for a self-hosted file too. Deriving one from the service,
  which publishes a thumbnail per video id, would send the service the very request the
  play control exists to withhold, so the component never does it.
- **A name is required.** `Title` names the frame, because an unnamed `<iframe>` is an
  unlabeled stop in the tab order.
- **Autoplay is muted.** Anything that plays by itself needs a control over it, so autoplay
  is forced muted: an embed asks for it in its query, and Twitch, which autoplays unless
  told not to, is told not to; a self-hosted file is muted through the media element's
  property, because the `muted` attribute no longer reaches the element once Blazor has
  created it.
- **A script that never runs leaves a link.** A service script that is blocked or slow
  leaves a link to the video in the player's place rather than an empty box; the link is
  hidden once the service has rendered its player.

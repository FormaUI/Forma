# Forma Media Design

## Scope

`FormaVideoPlayer` and the `Forma.Framework.Media` contracts it plays through: the
providers that resolve an address, the options a host passes them, and the frames they
answer with.

## Decision summary

- **A chain of providers resolves the address.** A watch link, a share link and an embed
  link all work without the caller naming the service. Providers the host registered are
  tried first, so an application can teach the player an address shape the library never
  knew, or take over one it would otherwise claim, the same way a host extends
  `FormaFileViewer`; the built-in providers follow, with the file provider last because it
  accepts any fetchable address. The options carry autoplay, limited tracking, and the
  address of the page the player renders on, whose host Twitch requires, declining the
  address without it.
- **Two kinds of service, three kinds of frame.** YouTube, Vimeo and Twitch answer with an
  embed address the player loads in an `<iframe>`. TikTok, Instagram, Facebook and X
  answer with markup and a script, which a module loads once and asks to scan, each
  service sizing its own player. Any other address a page can fetch plays in the browser's
  own `<video>`, with optional captions; captions supplied for a service's frame are an
  error, and an address a page cannot fetch, such as a local file, is refused with an
  unavailable notice. The player keeps the video's proportions, except where a service
  sizes its own player and the player lets it flow.
- **External script belongs to the services whose player is a script.** Those publish no
  embed address, and the iframe services need none.
- **Loading waits for play.** Deferred loading holds both the embed address and the
  service script behind a play control until the reader presses it; autoplay overrides it,
  and a self-hosted file loads as any video does. A third-party frame that loads with
  every article is both a tracking surface a public site has to account for and a large
  fixed cost on a mobile connection.
- **Ask the service for privacy where it offers it.** Limited tracking, on by default,
  asks YouTube and Vimeo for their privacy-preserving embeds, and X is always asked not to
  track.
- **The still is the host's to supply.** The placeholder is an image the host serves from
  its own origin, and stands in for a self-hosted file too. A still derived from the
  service would send it the very request the play control exists to withhold, so the still
  comes from the host alone.
- **A name is required.** The title names the frame, so it is a labeled stop in the tab
  order.
- **Autoplay is muted.** Anything that plays by itself needs a control over it, so
  autoplay is muted for every source: an embed asks for muted play, Twitch, which
  autoplays by its own default, plays only when autoplay is asked for, and a self-hosted
  file is muted as it starts.
- **A script that never runs leaves a link.** A service script that is blocked or slow
  leaves a link to the video in the player's place, hidden once the service has rendered
  its player.

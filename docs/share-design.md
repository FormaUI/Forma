# Forma Share Design

## Scope

`FormaShareMenu`, the share contracts in `Forma.Framework.Share`, and the destinations
`Forma.AddOns.Share.Targets` ships.

## Purpose

A menu of everything the app can do with an item: send it to a network or a destination,
hand it to the platform's own share sheet, or run one of the built-in commands. The menu
answers "what can the app do"; the OS sheet answers "where does this go", and each keeps
to its question.

## Decision summary

- **Composed over the drop-down menu wholesale.** Trigger, anchored popover, dismissal,
  keyboard and focus all come from `FormaDropDownMenu`; the share menu adds what a share
  surface means: an item header, a networks row, the built-in commands standing in the
  open beneath it, and consumer commands behind More.
- **The networks row is a command bar track.** It scrolls with arrows as it outgrows the
  menu, and carries the link networks, the native share hand-off and every registered
  destination. Its items are menu items laid out in columns, so they take part in the
  menu's arrow-key navigation like every other row.
- **Every network on the row is an `IShareTarget` resolved from DI.** A destination
  answering an address renders as a link opening the network's composer in a new tab; one
  without posts through its API when chosen. A host sends items elsewhere, a cloud drive,
  a chat channel, an internal vault, by registering its own targets, each declaring what
  it accepts and joining the row for those items alone.
- **Kinds decide what applies.** An image adds Pinterest, pointed at its media; a file
  reaches the destinations that take its resource and keeps Download and Copy Link; a
  contact keeps email and its card. A resource yields Download as a plain download link
  saved from its source under its name, the same way `FormaFileViewer` offers a file. A
  consumer holding content in memory adds its own action for it.
- **The built-in commands are Download, Email, Copy Link and Print.** Where the browser
  exposes a native share surface, a Share entry at the head of the row hands an item with
  an address to the platform's own sheet.
- **Consumer actions are data.** Each has an identity, a title, a command, and optionally
  an icon, and can be disabled, so a composer plugin can contribute without markup; they
  all stand behind More.
- **Every string comes localized.** Baseline labels are the component's own resources,
  network names are proper nouns, and action titles arrive caller-localized like toolbar
  titles.
- **The networks ship in an add-on.** The library carries the contracts, and the baseline
  networks, Facebook, X, WhatsApp, Telegram, Bluesky, Reddit, Snapchat, LinkedIn and
  Pinterest, ship as navigation destinations in the add-on, registered together beside the
  library's own registration. The add-on also ships destinations the host switches on one
  by one, each by its own registrar: Messenger's send dialog under the host's Facebook
  app, which Facebook documents as unsupported on mobile devices, Discord through a
  channel webhook, Mastodon's composer on the server the user names, Dropbox through its
  save-URL API, Google Drive and OneDrive as uploads, and TikTok by publish-by-pull.
- **Host settings and a user's credentials are two different things.** A value the website
  owns, an app id or a webhook address, is an options class beside its target in the
  add-on, bound the conventional way and settable at registration. A value the user owns,
  the token for their Dropbox or TikTok, comes from an `IShareTokenSource` the host
  implements in the current user's context, answering a token for a target, or null when
  the user has connected none; a failure to retrieve one throws, so it reads as a failure
  and "not connected" stays its own answer. A federated network's value the user owns is
  the server their account lives on, which an `IShareServerSource` answers synchronously,
  since a stored preference needs no refresh and a link target composes its address while
  rendering.
- **A target says whether it shares through an account.** The target declares it, so the
  menu keeps no list of platforms. An account-backed target is registered in the user's
  context, so the user's token source lives only as long as that context, and its share
  reports whether it happened, so the menu can say the share needs a connected account.
- **Connection status is advisory and comes from the host.** An optional
  `IShareConnections`, kept apart from the token source because starting a sign-in and
  answering a token are different responsibilities, says whether the user has connected a
  target, starts the host's sign-in for one, and notifies when a connection changes. The
  menu reads it to draw an account target the user has not connected as a Connect entry,
  drawing itself from status alone. A connected status can still fail at share time, so a
  share is reported inside the menu either way: shared, or needing a connected account,
  which is also what a host that reports no status gets.
- **The native hand-off reaches the rest.** A network whose share needs an instance the
  reader chooses, or an app with a share endpoint only on its own platform, is reached
  through the native hand-off, where the platform lists it itself. Instagram publishes
  only through Meta's Graph API for professional accounts, so it would be an account
  destination shaped like TikTok's. TikTok and Discord share content through their APIs
  alone, which is why each ships here by publish-by-pull and by webhook. Skype's share
  endpoint redirects to Teams.

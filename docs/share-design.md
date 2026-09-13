# Forma Share Design

## Scope

`FormaShareMenu`, the share contracts in `Forma.Framework.Share`, and the destinations
`Forma.AddOns.Share.Targets` ships.

## Purpose

A menu of everything the app can do with an item: send it to a network or a destination,
hand it to the platform's own share sheet, or run one of the built-in commands. The menu
answers "what can the app do"; the OS sheet answers "where does this go", so the two never
compete.

## Decision summary

- **Composed over the drop-down menu wholesale.** Trigger, anchored popover, dismissal,
  keyboard and focus all come from `FormaDropDownMenu`; the share menu adds only what a
  share surface means: an item header, a networks row, the built-in commands standing in
  the open beneath it, and consumer commands alone behind More.
- **The networks row is a command bar track.** It scrolls with arrows rather than
  wrapping or widening the menu, and carries the link networks, the native share hand-off
  and every registered destination. Its items are still menu items, restyled into columns,
  so they register with the menu's arrow-key navigation like every other row.
- **Every network on the row is an `IShareTarget` resolved from DI.** None is hardcoded in
  the component. A destination answering an address renders as a link opening the network's
  composer in a new tab; one without posts through its API when chosen. A host sends
  items elsewhere, a cloud drive, a chat channel, an internal vault, by registering its
  own targets, each declaring what it accepts and joining the row for those items alone.
- **Kinds decide what applies.** An image adds Pinterest, pointed at its media; a file or
  a contact gets no link network, a file still reaching the destinations that take its
  resource; the file keeps Download and Copy Link and the contact keeps email and its
  card; a `ResourceInfo` yields Download as a plain download link saved from its source
  under its name, the same way `FormaFileViewer` offers a file. In-memory content is not
  modeled: a consumer holding content adds its own action for it.
- **The built-in commands are Download, Email, Copy Link and Print.** Where the browser
  exposes a native share surface, a Share entry at the head of the row hands an item with
  an address to the platform's own sheet.
- **Consumer actions are data.** Each is a `ShareAction`, an id, a title, a command, an
  optional icon and a disabled flag, so a composer plugin can contribute without markup;
  they all stand behind More, whose section collapses through `FormaVisibility`.
- **No strings for a caller to localize.** Baseline labels are the component's own
  resources, network names are proper nouns, and action titles arrive caller-localized like
  toolbar titles.
- **The library references only the contracts.** The baseline networks, Facebook, X,
  WhatsApp, Telegram, Bluesky, Reddit, Snapchat, LinkedIn and Pinterest, ship as navigation
  destinations in the add-on, registered by `AddFormaShareNetworks()` beside `AddFormaUI()`.
  The add-on also ships destinations the host switches on one by one, each by its own
  registrar: Messenger's send dialog under the host's Facebook app, which Facebook documents
  as unsupported on mobile devices, Discord through a channel webhook, Mastodon's composer
  on the server the user names, Dropbox through its save-URL API, Google Drive and OneDrive
  as uploads, and TikTok by publish-by-pull.
- **Host settings and a user's credentials are two different things.** A value the website
  owns, an app id or a webhook address, is an options class beside its target in the
  add-on, bound the conventional way and settable through the registrar's configure
  overload; reloading it at runtime is the host's wiring. A value the user owns, the token
  for their Dropbox or TikTok, comes from `IShareTokenSource`, a contract in
  `Forma.Framework.Share` the host implements in the current user's context and that
  answers a token for a target id, or null when the user has connected none. A retrieval
  failure throws, so it is never mistaken for "not connected". A federated network has no
  central server, so the value the user owns there is the server their account lives on:
  `IShareServerSource` answers it synchronously, since a stored preference needs no
  refresh and a link target composes its address while rendering.
- **A target says whether it shares through an account.** `RequiresAccount` on the target
  contract, true on `AccountShare`, the shell the account-backed destinations stand on;
  the menu never keeps a list of platforms. Such a target is registered in the user's
  context, so no singleton ever holds the user's token source, and its share reports
  whether it happened: false with no token, so the menu can say the share needs a
  connected account.
- **Connection status is advisory and comes from the host.** `IShareConnections`, optional
  and separate from the token source because starting a sign-in and answering a token are
  different responsibilities, says whether the user has connected a target and starts the
  host's sign-in for one; it notifies through `INotifyStateChanged` when that changes,
  since starting a sign-in does not establish that anything connected. The menu reads it
  to draw an account target the user has not connected as a Connect entry and never
  fetches a token to draw itself. A connected status guarantees nothing at share time, so
  a call is reported inside the menu either way: shared, or needing a connected account,
  which is also what a host that reports no status gets.
- **What needs per-user configuration to share at all is not a baseline network.** A
  network whose share needs an instance the reader chooses, or an app with no public web
  share endpoint, is reachable only through the native hand-off, where the platform lists
  it itself.

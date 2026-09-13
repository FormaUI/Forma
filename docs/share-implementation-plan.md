# Forma Share Implementation Plan

The plan for the share menu and its targets that [the share design](share-design.md)
states, and the record of what has landed.

## Status

The menu, the contracts, the nine baseline networks, Messenger's send dialog, and the five
API destinations are built. LinkedIn landed once its share-offsite endpoint was confirmed
to work; its mark comes from Bootstrap Icons, which brought the brand pack its second
source, since Simple Icons no longer carries it. The account-backed destinations first took
a token delegate at registration; they now share through `IShareTokenSource` in the user's
context, with `IShareConnections` driving the menu's Connect entry, and the state-change
contract they notify through moved out of the feedback assembly into
`Forma.Framework.Events` so the share contracts could depend on it as a package.

## Deliberately absent

- Mastodon, whose share needs the reader's instance.
- Instagram, whose publishing goes through Meta's Graph API for professional accounts
  alone; it would be an account destination shaped like TikTok's, and waits on a host
  with a reviewed app to test against.
- TikTok's share kit and Discord's share dialog: TikTok is reached by publish-by-pull and
  Discord through a webhook, and neither platform offers a web share link for content.
- Skype, retired; its share endpoint redirects to Teams.

## Open

Nothing.

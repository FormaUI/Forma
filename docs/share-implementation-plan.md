# Forma Share Implementation Plan

The plan for the share menu and its targets that [the share design](share-design.md)
states, and the record of what has landed.

## Status

The menu, the contracts, the eight baseline networks and the five API destinations are
built.

## Deliberately absent

- Mastodon, whose share needs the reader's instance.
- Instagram, TikTok's share kit (which needs a client key; the API destination is the
  publish-by-pull path), Discord's and Messenger's share dialogs (which need an app id):
  apps with no public web share endpoint, reachable only through the native hand-off.
- Skype, retired; its share endpoint redirects to Teams.

## Open

- LinkedIn's share-offsite endpoint works, but its mark is no longer in Simple Icons, so
  the network waits on lawful artwork.

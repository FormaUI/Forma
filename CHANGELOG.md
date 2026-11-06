# Changelog

Each release of Forma's packages, newest first, every package of a release at its version. An entry is dated when its
release is cut, and reads *Unreleased* until then. It lists every change a consumer can notice, and a change that
breaks what the release before it allowed says how to migrate, as [Versioning's rules](https://github.com/FormaUI/Versioning/blob/v1.0.0-preview.4/docs/releases.md#the-release-record)
set out. What Forma gained before its first release, month by month, is in [the feature history](FEATURE-HISTORY.md).

## `1.0.0-preview.3`

*Unreleased*

- Every labeled field centers its label, while the label rests in the box, on the box itself, and so does what sits
  over the box, such as `FormaPasswordBox`'s reveal toggle. Inside a form, where a field keeps room for a message
  beneath the box, they sat at its bottom edge.
- `FormaField` takes `FooterContent`, the content beneath its box and ahead of its messages, such as
  `FormaPasswordBox`'s strength meter.

## `1.0.0-preview.2`

*November 6, 2026*

- `FormaPagination` stays within the width it's given in every language: its navigation buttons show their glyphs
  only where the glyphs stand in for the words, and where a language's words need more room than English's, the
  page box or the steps give way.

## `1.0.0-preview.1`

*November 5, 2026*

- The first release of Forma's packages, each described by its README, with what Forma held at the time in
  [the features status](docs/features-status.md) and [the catalog](docs/component-catalog.md).

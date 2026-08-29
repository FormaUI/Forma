# Brand marks

A holding library of brand logos as single-path SVGs, kept on hand so one can be
promoted into the glyph pipeline when a component or host needs it. Nothing in the
library compiles or ships them; only the marks that are promoted into
`assets/Glyphs` and the `Glyph` enum reach a package.

`brands.txt` names each mark and its Simple Icons slug. `scripts/Get-SimpleIconsPack.ps1`
fetches every entry from a pinned Simple Icons release and writes it here as
`{Name}.svg`.

## Credits

The artwork is from [Simple Icons](https://simpleicons.org), released under
[CC0 1.0](https://github.com/simple-icons/simple-icons/blob/develop/LICENSE.md).

CC0 covers the drawings, not the marks they depict. Each logo remains the trademark
of its owner and is subject to that owner's brand guidelines; the accepted use is
linking to a presence on that brand's platform. Simple Icons removes marks at a
brand's request, so a slug that stops resolving means that brand no longer permits
third-party reproduction and the entry is to be deleted rather than replaced.

# Brand marks

The brand logos the library draws, as single-path SVGs. Each list here names the marks one
source supplies, `Name slug` per line with the brand's proper name in double quotes where the
source does not carry a title: `simple-icons.txt` for Simple Icons, fetched by
`scripts/Get-SimpleIconsPack.ps1`, and `bootstrap-icons.txt` for Bootstrap Icons, fetched by
`scripts/Get-BootstrapIconsPack.ps1`. A fetch script writes every entry of its list from a
pinned release as `{Name}.svg`, keeping the brand's title as the mark's accessible name, and
removes any mark no list names. The `Brand` enum names every mark here, and
`scripts/Build-BrandPack.ps1` compiles them into the asset the library embeds, failing when
the two disagree.

Adding a brand is one line in the list of the source that carries it, a fetch, and one enum
member. A mark one source withdraws moves to a list of another source that carries it, and is
deleted only when none does.

## Credits

The artwork in `simple-icons.txt` is from [Simple Icons](https://simpleicons.org), released
under [CC0 1.0](https://github.com/simple-icons/simple-icons/blob/develop/LICENSE.md). The
artwork in `bootstrap-icons.txt` is from [Bootstrap Icons](https://icons.getbootstrap.com),
released under the [MIT license](https://github.com/twbs/icons/blob/main/LICENSE), reproduced
in the repository's third-party notices.

Neither license covers the marks the drawings depict. Each logo remains the trademark of its
owner and is subject to that owner's brand guidelines; the accepted use is linking to a
presence on that brand's platform. Simple Icons removes marks at a brand's request, so a slug
that stops resolving there means that brand withdrew from that set.

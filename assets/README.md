# Forma's logo

Forma's own logo, wordmark and favicon, each in the variants listed below. The folders beside
them hold the sources of the library's packs: `Brands` the marks of other brands, `Glyphs` the
icon drawings.

| File | What it is |
|---|---|
| `logo-black-gray-border.svg` | The O with the fox, on its own |
| `wordmark-black.svg`, `wordmark-black-gray-border.svg` | The name, its O holding the fox; the second with a gray outline round the O |
| `favicon-black.svg`, `favicon-black-gray-border.svg` | The O alone, at favicon size; with a gold and with a gray outline |
| `logo-blue.svg`, `wordmark-blue.svg`, `wordmark-blue-gold-border.svg`, `favicon-blue.svg` | The blue, fixed-color set |

## Theming the black set

The black files read four CSS variables. Set any of them where the logo is drawn, and the
matching part takes the color; a variable left unset keeps the file's own color.

| Variable | Controls |
|---|---|
| `--forma-logo-wordmark` | F, R, M, A |
| `--forma-logo-background` | The O's fill, and the fox's mouth |
| `--forma-logo-fox` | The fox |
| `--forma-logo-border` | The O's outline, in the files that have one |

```css
.header-logo {
    --forma-logo-wordmark: var(--text-primary-color);
    --forma-logo-background: var(--backdrop-raised-bg-color);
    --forma-logo-fox: var(--primary-color);
    --forma-logo-border: var(--border-color);
}
```

The variables reach the drawing only where the SVG is part of the page, as inline markup.
Loaded through an `img` tag, a file draws in its own colors. A file repeats its element ids, so
inline at most one of them in a page and load any other as an image.

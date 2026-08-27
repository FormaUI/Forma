# Forma glyph artwork

The specification for Forma's own icon set: the artwork `FormaIcon` draws unless a host registers
an `IIconProvider` of its own. Every glyph in the `Glyph` enum is a stroke drawing authored as SVG on a
shared grid, from which the build derives its filled counterpart, so the set reads as one family.

This document is the brief for whoever draws the glyphs — a person or a generator. Everything it
asks for is checkable, and a drawing that breaks a rule is redrawn, not accepted.

## Deliverable

One stroke drawing per glyph, named exactly after the enum member; the filled variant is derived
from it, and a drawn `{Name}.alt.svg` appears only where the derived one is wrong (see Variants):

```
src/Forma.UI.Blazor/Assets/Glyphs/AddColumnLeft.svg
src/Forma.UI.Blazor/Assets/Glyphs/Airplane.svg
…
```

A file holds a single `<svg>` element and nothing else — no XML prolog, no comments, no `<defs>`,
no `<g>`, no `id`, `class` or `style` attributes, no transforms. The whole file is:

```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
  <path d="…" />
  <path d="…" />
</svg>
```

A filled file has the same shape with the root attributes swapped:

```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32" fill="currentColor" fill-rule="evenodd" stroke="none">
  <path d="…" />
</svg>
```

Only `<path>` elements are allowed inside. No `<circle>`, `<rect>`, `<line>` or `<polyline>` —
write each one as path data, so the compile step and the renderer deal with one thing.

Two attributes may appear on an individual `<path>` and nothing else:

| Attribute | When |
|---|---|
| `fill="currentColor"` together with `stroke="none"` on a stroke file | A solid dot or wedge that a stroke cannot draw — the bullet in `BulletList`, the dots in `More`, the triangle of `Play`. Use sparingly. |
| `opacity="0.4"` | A secondary element that must recede — the back frame in `Images`, the second figure in `Users`. |

The build script (`scripts/Build-GlyphPack.ps1`) turns the folder into the asset the built-in
provider reads and rejects any file that breaks the rules above, or a glyph with no drawing.

## The grid

The stroke is one sixteenth of the box, the proportion the common open icon sets share, so the set
sits beside any of them without a visible jump.

- **ViewBox `0 0 32 32`.** Every coordinate is in this space.
- **Integers only.** Every coordinate and radius is a whole number, so a 2-unit stroke centered on
  it lands on whole pixels at 32 and 16 and on half pixels at 24.
- **Live area 3 to 29.** Nothing touches the outer 3 units on any side. A glyph that needs to read
  as large (a full-bleed arrow, a square frame) may reach 2 but never 1.
- **Stroke 2, round caps, round joins.** Set once on the `<svg>`; never overridden on a path.
- **Every corner of a box is an arc: radius 3 on a body** (a page, a frame, a tray, a card), **radius 1 on a
  part under 6 units** (a lid, a key cap, a node, a clasp). A square corner appears only in a bracket
  (`Crop`, `Maximize`, `Select`) or a page fold. Circles are circles; a rounded square is not a substitute.
- **Gaps of at least 3 between parallel strokes**, so at 16px they stay two lines rather than one
  smudge. Three-line stacks (`List`, `AlignLeft`) sit at y = 9, 16, 23.
- **Optical center, not geometric.** A triangle or an arrow is shifted so its visual mass sits in
  the middle; a `Play` triangle sits about 1 unit right of center.

## Variants

Every glyph is drawn once, as the stroke drawing; the build derives the other `GlyphVariant` from
it, so the two can never drift apart in style:

| Variant | Source | Drawing |
|---|---|---|
| `Default` | `{Name}.svg` | The stroke drawing: stroke 2, `fill="none"`. |
| `Alternate` | derived by `scripts/GlyphPack.cs` | The stroke drawing's silhouette: each body — a closed contour of some size — filled together with its own round-capped outline, every other mark kept as its outline, and whatever of those marks lies inside a body cut out of it. A drawing with no body draws its strokes at 3 instead. |

A `{Name}.alt.svg` in the filled form (`fill="currentColor" fill-rule="evenodd" stroke="none"`)
overrides the derived variant for one glyph. Supply one only where derivation gives the wrong
picture — a body drawn as an open path (`Archive`, `Edit`, `Notifications`), or a mark that must stay
solid inside its body (`RadioButton`, `Record`) — and draw it to the rules below.

The filled drawing is the stroke drawing's silhouette, not a re-imagining of it:

- **The outer edge is the stroke drawing's outer edge.** Take the stroke's outside boundary as the
  silhouette; the shape sits in the same place at the same size, so the two variants swap without
  a shift.
- **Interior features become knock-outs.** What was an inner stroke is cut out of the fill with
  `fill-rule="evenodd"`: a filled `CheckCircle` is a solid disc with the tick cut through it, a
  filled `Folder` is a solid folder whose tab is read from the silhouette, a filled `Image` is a
  solid frame with the dot and the mountains cut out. A knock-out keeps the stroke's 2-unit width
  and the same 3-unit gaps, so it still reads at 16px.
- **No strokes in a filled file.** A detail that has no area — the hands of `Clock`, the slash of
  `EyeSlash`, the text lines of `FileText` — is drawn as a knock-out 2 units wide, never as a
  stroke over the fill.
- **A glyph with nothing to fill keeps its strokes, heavier.** Arrows, chevrons, `Plus`, `Minus`,
  `Close`, `Check`, `Code` and the letterforms (`Bold`, `Italic`, `Heading`, `Font`…) have no
  body; the build draws their strokes at 3 instead.
- **A glyph that is already solid stays as it is.** `More`, `DragHandle` and the other all-dot
  glyphs look the same in both variants.
- **Opacity survives.** A receding element keeps its `opacity="0.4"` in the filled variant.

## The vocabulary

A set is coherent when the same part is the same everywhere. These parts are drawn once and reused;
a glyph that needs one copies it, it does not redraw it.

| Part | Geometry |
|---|---|
| **Arrowhead** | Two strokes meeting at the tip at 90°, each reaching 7 back and 7 across: for a right-pointing tip at (x, y): `M x-7 y-7 L x y L x-7 y+7`. "Scaled to n" replaces the 7 with n. |
| **Chevron** | An arrowhead with no shaft, scaled to 5 (`ChevronDown`, `ChevronRight`…). |
| **Page** | A rectangle from (8, 4) to (24, 28) with a folded corner: the fold cuts 7 units off the top-right, and a second path draws the fold flap. |
| **Folder** | Body from (4, 9) to (28, 27) with radius 3; a tab from (4, 9) rising to (13, 5) and across to (17, 9). Open variants swing the front face. |
| **Frame** | A rectangle from (4, 5) to (28, 27), radius 3 (`Image`, `Video`, `Table`, `CreditCard`). |
| **Circle** | Center (16, 16), radius 12, for badge-style glyphs (`CheckCircle`, `CloseCircle`, `PlayCircle`, `Information`, `Prohibited`, `Clock`). The inner mark stays inside radius 7. |
| **Square** | (5, 5) to (27, 27), radius 3, for `CheckSquare`, `PlusSquare`, `MinusSquare`. |
| **Stack** | Three horizontal lines, x 5 to 27, at y 9, 16, 23 (`AlignJustify`); the other align variants shorten the middle or both ends, and `List` prefixes each line with a tick. |
| **Slash** | One stroke from (5, 27) to (27, 5), laid over another glyph (`EyeSlash`, `Unpin`, `Mute`). The underlying glyph is drawn whole; do not cut it around the slash. A glyph the slash would tangle with instead says removed another way: `RemoveLink` breaks the chain, `RemoveAnchor` wears a cross badge. |
| **Plus** | Two strokes of length 8 crossing at a point: `M x-4 y L x+4 y M x y-4 L x y+4`; overlaid at the lower-right of a glyph it is centered at (23, 23). |
| **Person** | A head circle radius 5 at (16, 11) and shoulders as an arc from (7, 28) to (25, 28) rising to y 20. |
| **Tray** | A lipped tray from (4, 19) to (28, 27), radius 3 at its four corners, open along the top between x 10 and 22 (`Downloads`); a dipped tray of the same box whose rim dips from (10, 18) to (12, 21) across to (20, 21) and up to (22, 18) (`Empty`, `Inbox`, `Outbox`); a bracket tray, the bottom edge alone from (5, 23) round to (27, 23) (`Download`, `Upload`); and an open box from (5, 16) round the bottom to (27, 16) (`Extract`). |
| **Envelope** | A body from (4, 7) to (28, 25), radius 3; a flap as a chevron from (4, 9) down to (16, 18) up to (28, 9) (`Unread`, `JunkMail`). `Read` lifts the flap open. |
| **Badge** | A circle radius 5 centered at (25, 24) carrying one mark scaled to fit inside it; the glyph it sits on stops 2 units short of it, skirting it with an arc where an edge would otherwise cross it (`JunkMail`, `RemoveAnchor`). |
| **Pencil** | The `Pen` pencil: a body 4 wide on the diagonal with a flat cap line and a pointed tip; drawn at two-thirds size when it sits beside something (`Draft`, `Edit`). |
| **Chain slot** | A rounded slot 6 wide with radius-3 ends (`Link`, `HorizontalLink`). A broken chain pulls its two slots apart and marks the gap with two 3-unit ticks across the axis (`RemoveLink`, `RemoveHorizontalLink`). |

## The glyphs

One line per member of `Glyph`, in enum order. "Composition" says which parts to use and how; a
glyph with no parts named is drawn from scratch on the grid.

| Glyph | Composition |
|---|---|
| `AddColumnLeft` | Two tall columns (rounded rects 7 wide from y 5 to 27) at x 12–19 and 21–28; a Plus centered at (7, 16). |
| `AddColumnRight` | Mirror of `AddColumnLeft`. |
| `AddRowAbove` | Two wide rows (rounded rects 7 tall from x 5 to 27) at y 12–19 and 21–28; a Plus centered at (16, 7). |
| `AddRowBelow` | Mirror of `AddRowAbove` vertically. |
| `Airplane` | A plane seen from above, nose up: fuselage on the vertical center line, wings swept back, tailplane near the bottom. One closed path. |
| `AlignCenter` | Stack; top and bottom lines full, middle line x 9–23, all centered. |
| `AlignJustify` | Stack, all three full width. |
| `AlignLeft` | Stack; middle line x 5–20, left-anchored. |
| `AlignRight` | Stack; middle line x 12–27, right-anchored. |
| `Anchor` | Ring radius 3 at (16, 7); a shaft down the center to y 27; a crossbar at y 13 from x 11 to 21; arms as two arcs of radius 11 from (5, 19) down through (16, 27) up to (27, 19); flukes as ticks from (3, 19) to (7, 19) and (25, 19) to (29, 19). |
| `Archive` | A lid as a rounded rect from (4, 6) to (28, 12); a box below it from (6, 12) down to (6, 27) across to (26, 27) up to (26, 12); a handle slot from (13, 18) to (19, 18). |
| `ArrowDown` | Shaft from (16, 5) to (16, 27); Arrowhead pointing down at (16, 27). |
| `ArrowLeft` | Shaft from (27, 16) to (5, 16); Arrowhead pointing left at (5, 16). |
| `ArrowRight` | Shaft from (5, 16) to (27, 16); Arrowhead pointing right at (27, 16). |
| `ArrowUp` | Shaft from (16, 27) to (16, 5); Arrowhead pointing up at (16, 5). |
| `AtSign` | A circle radius 5 at (16, 16); a tail from (21, 12) down to (21, 19) rounding into a hook to (27, 19) and up to (27, 16); an outer arc of radius 11 around (16, 16) from (27, 16) over the top and left round to (23, 25). |
| `Audio` | A beamed pair of notes: two note heads (filled circles radius 3) at (9, 23) and (23, 20), stems up to y 8 and y 5, a beam joining the stem tops. |
| `Auto` | A bolt as one closed zigzag: (18, 3) down-left to (8, 18), across to (15, 18), down-left to (12, 29), up-right to (24, 13), across to (17, 13), back up to (18, 3). |
| `Bold` | A capital B built from strokes: a vertical at x 11 from y 7 to 25, two bowls to the right, the lower one larger. |
| `Bookmark` | A ribbon: (8, 4) down to (8, 28), up to the notch at (16, 22), up to (24, 28), up to (24, 4), closing across the top. |
| `Bug` | An oval body from (11, 11) to (21, 27); a head arc across the top; three legs each side at y 15, 19, 23 reaching x 5 and x 27; two short antennae. The filled form keeps every leg and antenna as its own solid bar. |
| `BulletList` | Three lines x 12–27 at y 9, 16, 23; three filled dots radius 2 at x 7. |
| `Cast` | Frame open at its lower-left; three concentric arcs of radius 4, 8 and 12 centered at (4, 27) in that corner, the smallest a filled dot. |
| `Category` | A tag: a pentagon from (4, 8) to (18, 8), out to the point (28, 16), back to (18, 24), to (4, 24), closing; a filled dot radius 2 at (9, 16) as the eyelet. |
| `Check` | A tick: `M 7 17 L 13 23 L 25 9`. |
| `CheckCircle` | Circle; the tick scaled to fit inside radius 7. |
| `CheckSquare` | Square; the same inner tick. |
| `ChevronDown` | Chevron pointing down, tip at (16, 20). |
| `ChevronLeft` | Chevron pointing left, tip at (12, 16). |
| `ChevronRight` | Chevron pointing right, tip at (20, 16). |
| `ChevronUp` | Chevron pointing up, tip at (16, 12). |
| `ClearFormatting` | A capital T (bar y 7 from x 7 to 20, stem at x 13 down to y 25) with a Slash from (17, 27) to (28, 16) crossing its lower right. |
| `Clock` | Circle; hands from center to (16, 9) and to (21, 19). |
| `Close` | Two diagonals: (8, 8)–(24, 24) and (24, 8)–(8, 24). |
| `CloseCircle` | Circle; the diagonals scaled inside radius 5. |
| `CloudUpload` | A cloud outline whose base sits at y 21 from x 7 to 25; an ArrowUp of shaft (16, 27)–(16, 15) rising through the base, arrowhead at (16, 15) scaled to 4. |
| `Code` | Two chevrons pointing outward: left tip at (5, 16), right tip at (27, 16), each scaled to 7. |
| `Collapse` | Two arrows on the diagonal pointing inward: one from (4, 4) to (13, 13) with arrowhead scaled to 5 at (13, 13), one from (28, 28) to (19, 19) with arrowhead scaled to 5 at (19, 19). |
| `Columns` | Frame; one vertical divider at x 16. |
| `Comment` | A speech bubble: a rounded rect from (4, 5) to (28, 23) with a tail dropping from (9, 23) to (7, 28) back to (15, 23). |
| `Compress` | Two arrows pressing toward a line: an ArrowDown with shaft (16, 4)–(16, 12) and an ArrowUp with shaft (16, 28)–(16, 20), arrowheads scaled to 4; the line from (7, 16) to (25, 16). |
| `Contacts` | A book: a rounded rect from (7, 4) to (27, 28) with two binding ticks on its spine from (4, 10) to (7, 10) and (4, 22) to (7, 22); Person reduced to head radius 3 at (17, 13) and shoulders from (11, 23) to (23, 23) rising to y 18. |
| `Copy` | Two overlapping rounded squares: back one (5, 5)–(20, 20), front one (12, 12)–(27, 27); the back one is interrupted where the front covers it. |
| `CreditCard` | Frame; a magnetic stripe as a line from (4, 13) to (28, 13). |
| `Crop` | Two overlapping L marks: one from (9, 4) down to (9, 23) across to (28, 23); the other from (4, 9) across to (23, 9) down to (23, 28). |
| `Crosshair` | Circle radius 9; four ticks from radius 9 to radius 13 at the compass points. |
| `Cut` | Two ring handles radius 4 at (9, 24) and (23, 24); blades from each ring crossing at (16, 15) up to (9, 4) and (23, 4). |
| `Database` | A cylinder: a top ellipse centered at (16, 8) with radii 10 and 4; sides at x 6 and x 26 from y 8 to y 24; a bottom arc joining them; a band arc across the middle at y 16. |
| `Debug` | `Bug` drawn with a divided back: the oval body from (10, 10) to (22, 28) with a center line down it, a head arc across the top, two legs each side, and two short antennae. |
| `Delete` | A waste bin: a lid line from (5, 8) to (27, 8) with a handle rising from (12, 8) to a radius-1 top from (13, 5) to (19, 5) and down to (20, 8); a body from (8, 8) down to (9, 24), round radius-3 corners across to (23, 24), up to (24, 8); two inner lines at x 14 and x 18 from y 13 to 22. |
| `DetailsView` | Frame; horizontal dividers at y 13 and y 20; below the first, vertical dividers at x 12 and x 20, so the frame reads as a grid of cells rather than a table with a header. |
| `DollarSign` | An S from strokes through (16, 5), (9, 11), (16, 16), (23, 21) and (16, 27) within x 9 to 23; a vertical bar through it from (16, 3) to (16, 5) and from (16, 27) to (16, 29). |
| `Download` | An ArrowDown with shaft from (16, 5) to (16, 20), arrowhead at (16, 20); the bracket Tray beneath it. |
| `Downloads` | The lipped Tray; an ArrowDown shaft (16, 4)–(16, 17) with arrowhead scaled to 5 at (16, 17). |
| `Draft` | Three lines from x 4 at y 9, 16, 23 reaching x 14, 12 and 10; the Pencil at two-thirds size from (13, 28) to (28, 17). |
| `DragHandle` | Six filled dots radius 2 at x 12 and 20, y 8, 16, 24. |
| `Edit` | Page; over its lower-right, the `Pen` pencil reduced to run from (14, 27) to (28, 13), so the glyph reads as editing a document. |
| `Emoji` | Circle; two filled dots radius 1 at (12, 13) and (20, 13); a smile arc from (11, 19) to (21, 19) dipping to y 22. |
| `Empty` | The dipped Tray alone. |
| `Error` | Circle; an exclamation mark: a stem from (16, 10) to (16, 18) and a filled dot radius 1 at (16, 22). |
| `EuroSign` | A C as an arc of radius 10 from (24, 8) round the left to (24, 24); two bars at y 13 and y 19 from x 5 to 19. |
| `Events` | Frame reduced to (4, 6)–(28, 28) with a header line at y 12 and two binding ticks rising from (10, 6) and (22, 6) to y 3; three filled dots radius 1 at (11, 20), (16, 20), (21, 20). |
| `Exit` | A door frame: an open bracket from (15, 4) to (5, 4) down to (5, 28) to (15, 28); an ArrowRight from (12, 16) to (28, 16), arrowhead at (28, 16) scaled to 5. |
| `Expand` | Two arrows on the diagonal pointing outward: one from (13, 13) to (4, 4) with arrowhead scaled to 5 at (4, 4), one from (19, 19) to (28, 28) with arrowhead scaled to 5 at (28, 28). |
| `Extensions` | A jigsaw piece: a square from (6, 10) to (26, 28) with a round knob of radius 3 rising from its top edge centered at (16, 10) and a round socket of radius 3 cut into its left edge centered at (6, 19). |
| `Extract` | The open box Tray; an ArrowUp with shaft (16, 20)–(16, 5) and arrowhead scaled to 5 at (16, 5). |
| `Eye` | An almond from (3, 16) to (29, 16) with upper and lower arcs reaching y 8 and y 24; a pupil circle radius 4 at (16, 16). |
| `EyeSlash` | `Eye` with a Slash. |
| `FastForward` | Two right-pointing filled triangles: (5, 8)–(5, 24)–(16, 16) and (16, 8)–(16, 24)–(27, 16). |
| `File` | Page. |
| `FileArchive` | Page; a zipper column: three short horizontal ticks at x 13–17, y 11, 15, 19, and a small square from (13, 21) to (17, 25). |
| `FileAttachment` | Page; a paper clip inside it as a Chain slot on the diagonal from (10, 21) to (21, 16), with the clips inner wire from (12, 20) to (18, 14). |
| `FileCode` | Page; inside it a small `Code` pair at tips (11, 20) and (21, 20), scaled to 4. |
| `FileText` | Page; three text lines at y 16, 20, 24 from x 12 to 20. |
| `Filter` | A funnel: (4, 7) to (28, 7), sides converging to (19, 17) and (13, 17), then down to y 27 and y 24. |
| `FindAndReplace` | Three lines x 5–27 at y 8, x 5–17 at y 16, x 5–13 at y 24; a Search reduced to radius 5 at (22, 21) with a handle to (28, 27). |
| `Flag` | A pole from (7, 28) to (7, 4); a waving flag from the pole at y 5 to x 25 and back at y 17. |
| `Folder` | Folder. |
| `FolderAdd` | Folder; a Plus centered at (16, 19). |
| `FolderOpen` | Folder with the front face swung open: the front rectangle's lower edge stays, its top edge is a line from (7, 15) to (29, 15) and the right side leans in to (25, 27). |
| `Font` | A capital A from strokes: apex at (16, 7), feet at (7, 25) and (25, 25), crossbar at y 19. |
| `FontSize` | A large A as in `Font` but from x 4 to 20 with apex (12, 7); a small A from x 20 to 28 with apex (24, 16). |
| `ForkKnife` | A fork on the left (handle x 9 from y 28 to 13, three tines up to y 4); a knife on the right (blade as a slim leaf from (23, 4) to (23, 17), handle down to y 28). |
| `Forward` | A path from (6, 27) up to (6, 16) curving through (10, 12) across to (26, 12); an Arrowhead scaled to 6 at (26, 12) pointing right. |
| `GridView` | Four rounded squares of side 9 at (4, 4), (19, 4), (4, 19), (19, 19). |
| `Group` | Three rhombi seen edge-on: the top one a closed diamond with vertices (16, 4), (28, 10), (16, 16), (4, 10); below it two chevrons from (4, 16) to (16, 22) to (28, 16) and from (4, 22) to (16, 28) to (28, 22). |
| `HashtagSign` | Two slanted strokes from (12, 5) to (9, 27) and from (23, 5) to (20, 27); two bars from (6, 12) to (27, 12) and from (5, 20) to (26, 20). |
| `Heading` | A capital H: verticals at x 8 and 24 from y 7 to 25, crossbar at y 16. |
| `Heart` | A heart whose lobes peak at y 8 and whose point is at (16, 27); one closed path. |
| `Help` | Circle; a question mark whose hook runs from (12, 12) over the top through (16, 9) to (20, 12) and down to (16, 17); a filled dot radius 1 at (16, 22). |
| `Highlight` | A marker pen at 45° like `Edit` but with a flat chisel tip 5 wide; a short underline stroke from (5, 28) to (13, 28). |
| `History` | A circular arrow: an arc from (7, 11) around the right side to (7, 21), arrowhead at (7, 11) pointing down-left; hands from center to (16, 11) and (20, 17). |
| `HorizontalLink` | Two Chain slots on the horizontal axis, from x 3 to 14 and from x 18 to 29 at y 13–19, joined by a bar from (14, 16) to (18, 16). |
| `Hourglass` | Two triangles meeting at (16, 16): top from (8, 4) to (24, 4), bottom from (8, 28) to (24, 28); caps at y 4 and y 28. |
| `Image` | Frame; a filled dot radius 2 at (11, 12); a mountain range: (4, 23) up to (12, 15) to (17, 20) to (21, 16) to (28, 23). |
| `Images` | `Image` reduced to a frame from (9, 8) to (28, 27) with its content; behind it at `opacity 0.4` a second frame from (4, 4) to (23, 23). |
| `Inbox` | The dipped Tray; an ArrowDown with shaft (16, 5)–(16, 15) and arrowhead scaled to 4 at (16, 15), pointing into the tray. |
| `Incognito` | A hat: a brim from (4, 15) to (28, 15) with a crown rising from (9, 15) and (23, 15) to a rounded top at y 5; below, two lens circles radius 4 at (10, 23) and (22, 23) joined by a bridge from (14, 23) to (18, 23). |
| `Indent` | Three lines x 5–27 at y 7 and y 25, and x 16–27 at y 16; an ArrowRight from (5, 16) to (12, 16) with arrowhead scaled to 4. |
| `Information` | Circle; a filled dot radius 1 at (16, 11); a stem from (16, 15) to (16, 23). |
| `Italic` | A slanted capital I: bar at y 7 from x 13 to 24, bar at y 25 from x 8 to 19, stem from (19, 7) to (13, 25). |
| `JunkMail` | The Envelope skirting a Badge that carries a Slash from (22, 27) to (28, 21). |
| `Keyboard` | Frame reduced to (4, 8)–(28, 24); nine key dots radius 1 in three rows; a space bar line at y 20 from x 11 to 21. |
| `Layout` | Frame; a horizontal divider at y 12; below it a vertical divider at x 12. |
| `LayoutBottom` | Frame; a horizontal divider at y 21; the lower pane filled at opacity 0.4. |
| `LayoutLeft` | Frame; a vertical divider at x 12; the left pane filled at opacity 0.4. |
| `LayoutRight` | Frame; a vertical divider at x 20; the right pane filled at opacity 0.4. |
| `LayoutTop` | Frame; a horizontal divider at y 11; the upper pane filled at opacity 0.4. |
| `Lightbulb` | A bulb: a circle radius 7 at (16, 12) opening at the bottom into a neck down to y 23; a base of two short lines at y 25 and y 28 from x 13 to 19. |
| `LineHeight` | A vertical double arrow on the left: shaft (7, 5)–(7, 27) with arrowheads at both ends scaled to 3; three lines at y 9, 16, 23 from x 13 to 27. |
| `Link` | Two chain links on the diagonal, each a rounded slot 6 wide: the first from (5, 27) to (12, 20), the second from (20, 12) to (27, 5), leaving a gap between them that a bar from (12, 20) to (20, 12) crosses. |
| `List` | Three rows at y 9, 16, 23, each a short tick from x 5 to 7 and a line from x 11 to 27. |
| `Lock` | A body as a rounded rect from (6, 14) to (26, 28); a shackle arc from (10, 14) up over (16, 4) down to (22, 14); a filled dot radius 2 at (16, 21). |
| `MagicWand` | A wand from (5, 27) to (18, 14); a four-point sparkle centered at (23, 9) with points 6 out and waist 2 in, one closed path. |
| `Maintenance` | The `Wrench` at two-thirds — a ring head radius 5 at (23, 9) with the same jaw, its handle a line from (20, 12) to (5, 27) — crossed by a screwdriver on the other diagonal: a handle as a rounded rect from (5, 5) to (12, 12) and a shaft from (11, 11) to (27, 27). |
| `Manual` | An open palm: a rounded body from (8, 16) to (24, 28); four fingers rising from it as parallel rounded strokes at x 10, 14, 18, 22 reaching y 5, 3, 4, 8; a thumb from (8, 20) out to (4, 15). |
| `Maximize` | Four corner brackets: each an L of 7 by 7 at the four corners of the live area. |
| `Message` | A bubble: a rounded rect from (5, 5) to (27, 24) whose lower-left corner is drawn out to a point at (5, 28); two text lines inside at y 12 and y 17 from x 11 to 21. |
| `Microphone` | A capsule as a rounded rect 8 wide from (12, 4) to (20, 18) with fully round ends; a cradle arc from (8, 14) down around to (24, 14) passing through (16, 22); a stem from (16, 22) to (16, 27); a base line from (11, 27) to (21, 27). |
| `Minimize` | A single line from (7, 16) to (25, 16). |
| `Minus` | A line from (7, 16) to (25, 16). Identical to `Minimize`; the compile step permits it. |
| `MinusSquare` | Square; a line from (11, 16) to (21, 16). |
| `More` | Three filled dots radius 2 at (7, 16), (16, 16), (25, 16). |
| `MoreVertical` | `More` rotated: dots at (16, 7), (16, 16), (16, 25). |
| `Mute` | The `SoundOutput` speaker body without its waves, with a Slash. |
| `NavigationMenu` | Three full-width lines x 5–27 at y 8, 16, 24 — spaced one unit wider than `List`, so the two read as different things side by side. |
| `Network` | A top node as a square side 6 at (13, 4); two bottom nodes as squares side 6 at (4, 22) and (22, 22); a trunk from (16, 10) down to (16, 16); a bar at y 16 from x 7 to 25; drops from (7, 16) and (25, 16) down to y 22. |
| `Notifications` | A bell: a dome from (7, 22) up over (16, 6) down to (25, 22), flaring to a rim from (5, 22) to (27, 22); a clapper arc from (13, 26) to (19, 26). |
| `NumberedList` | Three lines x 13–27 at y 8, 16, 24; a small "1" (stroke) at x 7, y 5–11, a "2" at y 13–19, a "3" at y 21–27, each within 4 wide. |
| `Open` | Square open at its top-right; an arrow from (15, 17) to (27, 5) with the Arrowhead scaled to 5 at (27, 5), pointing up-right. |
| `Outbox` | The dipped Tray; an ArrowUp with shaft (16, 15)–(16, 5) and arrowhead scaled to 4 at (16, 5), pointing out of the tray. |
| `Outdent` | Mirror of `Indent`: the short middle line at x 16–27 and the arrow pointing left from (12, 16) to (5, 16). |
| `Package` | A cube in three-quarter view: a hexagonal outline with top vertex (16, 4) and bottom (16, 28); interior edges from (16, 16) to the top-left, top-right and bottom; a tape line across the top-left face. |
| `Palette` | A painter's palette: a rounded kidney shape filling the live area with a thumb hole at the lower-right; four filled dots radius 2 as paint wells. |
| `Paragraph` | A pilcrow: a filled bowl on the left (a half-circle from (9, 5) to (9, 16) bulging left), two stems at x 15 and x 20 from y 5 to 27, a top bar from x 9 to 25. |
| `Passwords` | Frame reduced to (4, 10)–(28, 22); three asterisks inside it, each three strokes through a center, at x 10, 16, 22. |
| `Paste` | A clipboard: a rounded rect (7, 7)–(25, 28); a clip at the top from (12, 4) to (20, 4) with sides down to y 9; two lines inside at y 16 and 21. |
| `PawPrint` | A pad (a rounded triangle) centered at (16, 20); four toe ovals above it at (8, 12), (13, 7), (19, 7), (24, 12). |
| `Pen` | A pencil at 45°: a rounded body from (5, 27) to (24, 8) as two parallel lines 4 apart, a tip at the lower-left, a cap line near the upper-right. |
| `Pin` | An upright push pin: a head plate as a rounded rect from (11, 4) to (21, 8); a body from x 13 to 19 down to y 16; a flange as a rounded rect from (8, 16) to (24, 20); a needle from (16, 20) to (16, 28). |
| `Plan` | Three columns of rounded rects 6 wide: at x 4, 13 and 22, the first from y 4 to 20, the second y 4 to 28, the third y 4 to 14; a header bar across the top from (4, 4) to (28, 4). |
| `Play` | A right-pointing filled triangle (8, 5)–(8, 27)–(27, 16). |
| `PlayCircle` | Circle; a right-pointing triangle (13, 11)–(13, 21)–(21, 16). |
| `Plus` | Plus with arms of 9: (16, 7)–(16, 25) and (7, 16)–(25, 16). |
| `PlusSquare` | Square; a Plus of arms 5 at (16, 16). |
| `Poll` | Three bars as rounded rects 6 wide rising from a baseline at y 28: at x 4 to y 16, x 13 to y 6, x 22 to y 20; the baseline from (4, 28) to (28, 28). |
| `PoundSign` | A hook as an arc of radius 5 from (22, 8) over the top to (12, 8) continuing down to (12, 20), a foot from (12, 20) to (8, 26) across to (24, 26); a bar from (8, 16) to (18, 16). |
| `Print` | A body as a rounded rect from (4, 12) to (28, 24); a sheet rising out of the top from (9, 12) up to (9, 4) across to (23, 4) down to (23, 12); a sheet emerging at the bottom from (9, 24) down to (9, 28) across to (23, 28) up to (23, 24); a filled dot radius 1 at (24, 16). |
| `Prohibited` | Circle; a Slash inside it from (8, 24) to (24, 8). |
| `Properties` | Three lines x 5–27 at y 9, 16, 23; on each a filled knob circle radius 3, at x 20, 11 and 17 respectively. |
| `Publish` | An upright rocket: a body whose nose is at (16, 3) and whose sides curve out to a base from (12, 20) to (20, 20); a porthole circle radius 2 at (16, 12); a fin each side from the body at y 15 out to (7, 21) and (25, 21); a flame from (14, 21) down to (16, 28) and back to (18, 21). |
| `Quote` | Two closing quotation marks: each a filled ball radius 2 with a tail curling down-left, at (11, 12) and (21, 12). |
| `RadioButton` | Circle radius 12; a filled circle radius 5 at the center. |
| `Read` | An envelope with its flap open: a body from (4, 14) down to (4, 27) across to (28, 27) up to (28, 14); the flap as a chevron from (4, 14) up to (16, 5) down to (28, 14); the fold as a chevron from (4, 14) down to (16, 22) up to (28, 14). |
| `Record` | Circle radius 12; a filled circle radius 6 at the center. |
| `Redo` | An arc from (7, 20) curving up and right to (25, 13); an arrowhead at (25, 13) pointing right, scaled to 5. |
| `Refresh` | Two half-rings of radius 10 around (16, 16): the upper from (7, 13) over the top to (25, 13), the lower from (25, 19) under the bottom to (7, 19); each ends in an Arrowhead scaled to 4 pointing on around the ring. |
| `RemoveAnchor` | `Anchor` with its right arm and fluke left off, wearing a Badge that carries a Close scaled to fit. |
| `RemoveHorizontalLink` | The Chain slots of `HorizontalLink` pulled apart to x 3–12 and x 20–29, with the break ticks at (15, 10)–(15, 13) and (17, 19)–(17, 22). |
| `RemoveLink` | The Chain slots of `Link` pulled apart to run from (5, 27) to (11, 21) and from (21, 11) to (27, 5), with the break ticks at (12, 12)–(14, 14) and (18, 18)–(20, 20). |
| `Reply` | Mirror of `Forward`: the path from (26, 27) up to (26, 16) curving through (22, 12) across to (6, 12); an Arrowhead scaled to 6 at (6, 12) pointing left. |
| `Review` | Two lens circles radius 5 at (9, 18) and (23, 18) joined by a bridge from (14, 18) to (18, 18); a temple rising from each outer edge, from (4, 17) to (5, 12) and from (28, 17) to (27, 12). |
| `Rotate` | One clockwise arc of radius 10 around (16, 16) from the top (16, 6) around the right and bottom to (7, 19), ending in an Arrowhead scaled to 4 pointing up at (7, 19). |
| `Rows` | Frame; one horizontal divider at y 16. |
| `Run` | Circle; a filled right-pointing triangle (13, 10)–(13, 22)–(22, 16). |
| `Save` | A floppy disk: a rounded square (4, 4)–(28, 28) with the top-right corner cut at 45°; a label slot from (9, 4) to (9, 12) across to (20, 12) up to (20, 4); a lower drawer from (8, 28) up to (8, 19) across to (24, 19) down to (24, 28). |
| `Search` | Circle radius 9 at (13, 13); a handle from (20, 20) to (28, 28). |
| `Select` | The dashed selection frame of `SelectAll`, empty. |
| `SelectAll` | A dashed selection frame: the Square drawn as four corner brackets of 6 by 6 plus a 4-long dash centered on each side; a smaller solid square from (11, 11) to (21, 21) inside it. |
| `SelectNone` | The same dashed selection frame as `SelectAll`, empty, with a Slash. |
| `Send` | A paper plane: a triangle with its nose at (28, 4) and tail corners at (4, 14) and (13, 28); a fold line from (28, 4) to (13, 19). |
| `Session` | Person reduced to head radius 4 at (16, 13) and shoulders from (9, 26) to (23, 26) rising to y 20; four corner brackets 6 by 6 at the corners of the live area. |
| `Settings` | A gear: a ring radius 4 at the center; an outer outline with eight teeth reaching radius 13; one closed path. |
| `Share` | Three nodes as circles radius 3 at (7, 16), (24, 7) and (24, 25); a line from the left node to each right node. |
| `ShekelSign` | Two interlocking brackets: one from (5, 26) up to (5, 6) across to (17, 6) rounding down to (20, 9) and on to (20, 20); the other from (27, 6) down to (27, 26) across to (15, 26) rounding up to (12, 23) and on to (12, 12). |
| `ShoppingCart` | A basket trapezoid from (8, 9) to (28, 9) narrowing to (24, 21) and (11, 21); a handle from (4, 5) to (8, 9); two filled wheels radius 2 at (12, 27) and (23, 27). |
| `SignIn` | A door frame: an open bracket from (17, 4) to (27, 4) down to (27, 28) to (17, 28); an ArrowRight from (4, 16) to (19, 16), arrowhead at (19, 16) scaled to 5. |
| `Snooze` | The `Notifications` bell reduced to fit the left two-thirds of the live area; a small letter Z of strokes within (20, 4)–(28, 11). |
| `SoccerBall` | Circle; a central pentagon of radius 5; five spokes from its vertices to the ring. |
| `Sort` | An ArrowDown with shaft (11, 5)–(11, 27) and an ArrowUp with shaft (21, 27)–(21, 5), arrowheads scaled to 5. |
| `SortAscending` | An up-pointing filled triangle (8, 20)–(24, 20)–(16, 9). |
| `SortDescending` | A down-pointing filled triangle (8, 12)–(24, 12)–(16, 23). |
| `SoundOutput` | A speaker: a box from (4, 12) to (10, 12) down to (10, 20) to (4, 20); a cone from (10, 12) out to (17, 6) down to (17, 26) back to (10, 20); two wave arcs to the right centered at (17, 16) with radius 5 and 9. |
| `Speaker` | A cabinet as a rounded rect from (7, 3) to (25, 29); a small driver circle radius 2 at (16, 9); a large driver circle radius 5 at (16, 20). |
| `SpellCheck` | Letters "ab" as strokes in the upper-left, within (4, 5)–(20, 17); a `Check` scaled to fit (13, 17)–(28, 27). |
| `Split` | Frame; a vertical divider at x 16; two small Arrowheads pointing away from the divider, tips at (10, 16) and (22, 16), scaled to 3. |
| `Star` | A five-point star centered at (16, 16) with outer radius 12 and inner radius 5, one point up; one closed path. |
| `Storage` | Two drives as rounded rects from (4, 5) to (28, 14) and (4, 18) to (28, 27); a filled dot radius 1 at (23, 10) and another at (23, 23) as their lights. |
| `Strikethrough` | A capital S from strokes centered at (16, 16) spanning y 7 to 25; a line from (5, 16) to (27, 16) across it. |
| `Subscript` | A lowercase x within (4, 8)–(17, 21); a small "2" within (20, 17)–(28, 27). |
| `Superscript` | A lowercase x within (4, 11)–(17, 24); a small "2" within (20, 5)–(28, 15). |
| `Swap` | An ArrowRight at y 11 from x 5 to 27 and an ArrowLeft at y 21 from x 27 to 5, arrowheads scaled to 5. |
| `Sync` | Two arcs forming a broken ring of radius 10 around (16, 16), gaps at the upper-right and lower-left, each arc ending in an Arrowhead scaled to 4 turning counter-clockwise. |
| `Table` | Frame; a horizontal divider at y 13 and vertical dividers at x 12 and 20 below it. |
| `TaskList` | Three rows at y 8, 16, 24: a square of side 7 at x 4 and a line from x 15 to 27; the top two squares carry a tick, the third is empty. |
| `Terminal` | Frame; a chevron pointing right with tip at (13, 16) scaled to 4; a line from (17, 21) to (24, 21). |
| `Text` | Frame; four text lines inside at y 11, 15, 19, 23 from x 9 to 23, the last ending at x 17. |
| `TextDirectionLtr` | Three lines at y 8, 16, 24 from x 5 to 20; an ArrowRight from (21, 16) to (28, 16) with arrowhead scaled to 4. |
| `TextDirectionRtl` | Mirror of `TextDirectionLtr`: lines from x 12 to 27, arrow pointing left from (11, 16) to (4, 16). |
| `ThumbsDown` | Mirror of `ThumbsUp` top to bottom: the cuff from (4, 5) to (9, 18), the thumb reaching down to y 27. |
| `ThumbsUp` | A cuff as a rounded rect from (4, 14) to (9, 27); a hand as one outline from (9, 15) up the thumb to (14, 5), rounding over to (17, 8), down to (16, 13), across the fingers to (25, 13), rounding to (27, 16), down to (25, 25), rounding to (22, 27) and back along the palm to (9, 27). |
| `Tools` | A box as a rounded rect from (4, 11) to (28, 27); a handle from (11, 11) up to (11, 7) across to (21, 7) down to (21, 11); a latch line across the box at y 18 from x 4 to 28 broken by a clasp from (14, 16) to (18, 16) down to (18, 20) to (14, 20). |
| `Translate` | A Latin A from strokes in the upper-left within (4, 4)–(15, 15); a page-like frame from (14, 14) to (28, 28) holding a horizontal stroke at y 19 from x 17 to 25 and a vertical at x 21 from y 17 to 25. |
| `TreeStructure` | A root node square side 5 at (4, 5); a trunk down from (7, 11) to (7, 24); two branches at y 16 and y 24 to x 17, each ending in a square side 5 at (17, 13) and (17, 21). |
| `Truck` | A cab from (4, 9) to (19, 9) down to (19, 23); a cargo box from (19, 13) to (28, 13) down to (28, 23); a baseline at y 23; two wheels as circles radius 3 at (9, 24) and (23, 24). |
| `Underline` | A U from strokes: (9, 5) down to y 17, curving through (16, 24) to (23, 17) up to y 5; an underline from (7, 28) to (25, 28). |
| `Undo` | Mirror of `Redo`: the arc from (25, 20) to (7, 13), arrowhead at (7, 13) pointing left. |
| `Unlock` | The `Lock` body and dot; the shackle rises from (10, 14) over (16, 4) and ends free at (22, 9). |
| `Unmute` | The `SoundOutput` speaker body with a single wave arc of radius 7 centered at (17, 16). |
| `Unpin` | `Pin` with a Slash. |
| `Unread` | The Envelope, closed. |
| `Upload` | Mirror of `Download` vertically: the arrow points up with shaft (16, 20)–(16, 5); the bracket Tray beneath it. |
| `User` | Person. |
| `Users` | A Person shifted left to center x 12; behind at `opacity 0.4` a second head radius 4 at (23, 11) and a partial shoulder arc from (21, 20) to (28, 28). |
| `Video` | Frame reduced to (4, 8)–(21, 24); a lens flap: (21, 13) to (28, 9) down to (28, 23) back to (21, 19). |
| `Warning` | A triangle with rounded corners, apex (16, 4), base from (4, 27) to (28, 27); a stem from (16, 12) to (16, 19); a filled dot radius 1 at (16, 23). |
| `WindowMaximize` | Four corner brackets 6 by 6 at the corners of the live area, with a full Frame between them at opacity 0.4. |
| `WindowMinimize` | An ArrowDown with shaft (16, 4)–(16, 20), arrowhead at (16, 20); a line from (6, 26) to (26, 26). |
| `Wrench` | An open-end wrench as one outline: a ring head radius 8 at (22, 10) with a parallel-sided jaw 4 wide cut from its center toward the upper right, open between (25, 4) and (29, 8); a handle 6 wide from the ring down the diagonal to a rounded end at (7, 25). |
| `ZoomIn` | `Search`; a Plus of arms 3 centered at (13, 13). |
| `ZoomOut` | `Search`; a line from (9, 13) to (17, 13). |

## What not to do

- Don't add perspective, shading, gradients, or a second color; the set is monochrome line art.
- Don't draw a glyph as a filled silhouette when a stroke will read; fills are for dots, wedges and
  the alternate variants only.
- Don't invent a new arrowhead, chevron, page or folder; copy the part from the vocabulary.
- Don't put text as `<text>`; letters (`Bold`, `Heading`, `Font`, `Subscript`…) are drawn as strokes.
- Don't leave a glyph out because it seems hard; every member of `Glyph` gets a file.

## Review

The Showcase's glyph page renders both variants of every glyph at 16, 20, 24 and 48 screen pixels.
A glyph is done when both variants read correctly at 16px,
sit on the same optical center as their neighbors, share their parts with the rest of the set, and
swap between each other without moving.

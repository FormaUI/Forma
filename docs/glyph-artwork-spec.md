# Forma Glyph Artwork Specification

The specification for Forma's own icon set: the artwork `FormaIcon` draws for a `Glyph` unless a host
registers an `IIconProvider` of its own. Every glyph in the `Glyph` enum is a stroke drawing authored as SVG on a
shared grid, from which the build derives its filled counterpart, so the set reads as one family.

This document is the brief for whoever draws the glyphs — a person or a generator. Everything it
asks for is checkable, and a drawing that breaks a rule is redrawn, not accepted.

## Deliverable

One stroke drawing per glyph per grid, named exactly after the enum member and filed under the
grid's folder; the filled variant is derived from it (see Variants):

```
assets/Glyphs/32/AddColumnLeft.svg
assets/Glyphs/32/AirplaneRight.svg
…
assets/Glyphs/24/AddColumnLeft.svg
…
```

The 32 grid draws every glyph. Another grid may draw a subset while it is being filled in; a glyph
it lacks falls back to the 32 drawing.

A file holds a single `<svg>` element and nothing else — no XML prolog, no comments, no `<defs>`,
no `<g>`, no `id`, `class` or `style` attributes, no transforms. The whole file is:

```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
  <path d="…" />
  <path d="…" />
</svg>
```

Only `<path>` elements are allowed inside. No `<circle>`, `<rect>`, `<line>` or `<polyline>` —
write each one as path data, so the compile step and the renderer deal with one thing.

Two attributes may appear on an individual `<path>` and nothing else:

| Attribute | When |
|---|---|
| `fill-rule="evenodd"` on such a solid path | A solid silhouette whose traced outline carries holes, as the `Hand…` family's do. |
| `fill="currentColor"` together with `stroke="none"` on a stroke file | A solid dot or wedge that a stroke cannot draw — the bullet in `ListBullet`, the dots in `More`, the triangle of `TriangleRight`. Use sparingly. |
| `opacity="0.4"` | A secondary element that must recede — the back frame in `Images`, the second figure in `Users`. |

The build script (`scripts/Build-GlyphPack.ps1`) turns the folder into the asset the built-in
provider reads and rejects any file that breaks the rules above, or a glyph with no drawing.

### Brand marks

A brand logo is never drawn by hand: it is a trademark, and its owner's guidelines forbid a
redrawing. `assets/Brands/` holds the official marks as fetched from a pinned release of an
icon set that carries them, one list and one `scripts/Get-{Source}Pack.ps1` script per source,
and none of the rules above apply to them. Every mark there has a member of the `Brand` enum
named for the brand; `scripts/Build-BrandPack.ps1` compiles the marks, at their own precision
and with each brand's title as its accessible name, into the asset the library embeds, and
fails when the enum and the marks on hand disagree in either direction. A mark one source
withdraws is taken from another source that carries it, and its brand leaves the enum only
when none does.

## Naming

A glyph is named for its recognizable visual form: `ArrowLeft`, not `Back`; `TriangleRight`, not
`Play`. Where the literal name would be obscure, cumbersome, or less recognizable than the
conventional UI meaning of the form, the glyph takes that meaning instead: `Search`, not
`MagnifyingGlass`; `Delete`, not `WasteBin`. A glyph is never named for an application's command or
feature unless it was drawn specifically and exclusively for it — a bespoke `Checkout` glyph used
only for checkout.

A drawing is made for left-to-right reading, and a glyph is never drawn twice for direction. A
glyph whose form points along the reading direction — an arrow or a chevron pointing left or
right, `Undo` and `Redo`, `Reply` and `Forward`, an arrow leaving a frame, indentation and the
marks a list starts with — is declared `GlyphDirectionSensitive("rtl")` on its enum member and
turned over at run time where text runs that way. A glyph named for a side (`AlignLeft`, `LayoutLeft`,
`AddColumnLeft`) keeps that side in both directions, as do a physical object (`Search`, `Pen`), a
clockwise motion (`Refresh`, `History`), a media control (`TriangleRight`, `FastForward`), and
anything carrying letters or digits (`ListNumbered`, `CircleHelp`), whose form would read wrong
turned over.

## The grids

A stroke is crisp only where its edges fall on pixel boundaries, so a drawing is sharp at the
sizes that are whole multiples of its grid and soft in between. The set is therefore drawn twice:
on a 32-unit grid, where the stroke is one sixteenth of the box (the proportion of Phosphor), for
32px and 64px; and on a 24-unit grid, where it is one twelfth (the proportion of Lucide and
Material), for 24px and 48px. The stroke is 2 units on both, so the smaller grid reads a little
bolder, which is what a small icon needs. `IconSize` names the sizes and the provider picks the grid.

The rules are the same on both grids; the 24 numbers are the 32 numbers at three quarters, snapped
to the grid. Every value below is in grid units.

| Rule | 32 grid | 24 grid |
|---|---|---|
| **ViewBox.** Every coordinate is in this space. | `0 0 32 32` | `0 0 24 24` |
| **Integers only.** Every coordinate and radius is a whole number, so a 2-unit stroke centered on it lands on whole pixels at the grid's sizes. A solid silhouette has no stroke to land, so on the 32 grid alone it may sit on half units, which keeps a crease as narrow as a unit. | — | — |
| **Live area.** Nothing touches the outer margin on any side; a glyph that needs to read as large (a full-bleed arrow, a square frame) may take one unit of it but never reach the edge. | 3 to 29, may reach 2 | 2 to 22, may reach 1 |
| **Fill the live area.** A glyph's larger dimension spans it. Three forms stay smaller on purpose, at the proportions the common sets draw them: a chevron is an arrowhead scaled down, a letterform is shorter than the box, and `Close` is narrower than `Plus` and `Minus`. | chevron 8; letterform 20 tall (y 6 to 26); `Close` 7 to 25; `Plus` and `Minus` 5 to 27 | chevron 6; letterform 15 tall (y 5 to 20); `Close` 5 to 19; `Plus` and `Minus` 4 to 20 |
| **Stroke 2, round caps, round joins.** Set once on the `<svg>`; never overridden on a path. | 2 | 2 |
| **Every corner of a box is an arc:** one radius on a body (a page, a frame, a tray, a card), a smaller one on a small part (a lid, a key cap, a node, a clasp). A square corner appears only in a bracket (`Crop`, `Maximize`, `Select`) or a page fold. Circles are circles; a rounded square is not a substitute. | body 3; part under 6 units 1 | body 2; part under 5 units 1 |
| **Gaps between parallel strokes**, so at the grid's smallest size they stay two lines rather than one smudge. Three-line stacks (`List`, `AlignLeft`) sit at the stated rows. | at least 3; stacks at y 9, 16, 23 | at least 2; stacks at y 7, 12, 17 |
| **Strokes never cross.** Where one element passes through another's outline — a Slash laid over a glyph, a Badge on its corner, an arrow rising out of a cloud or a tray — the outline breaks rather than being crossed: it stops clear of the crossing stroke's edge on either side, so the crossing stroke meets white and never a join. A part left inside the break, such as `EyeSlash`'s pupil, is dropped. | clear by 2 | clear by 2 |
| **Optical center, not geometric.** A triangle or an arrow is shifted so its visual mass sits in the middle; a `TriangleRight` sits about a unit right of center. | — | — |

## Variants

Every glyph is drawn once, as the stroke drawing; the build derives the other `GlyphVariant` from
it, so the two can never drift apart in style:

| Variant | Source | Drawing |
|---|---|---|
| `Default` | `{Name}.svg` | The stroke drawing: stroke 2, `fill="none"`. |
| `Alternate` | derived by `scripts/GlyphPack.cs` | The stroke drawing's silhouette: each body — a closed contour of some size — filled together with its own round-capped outline, every other mark kept as its outline, and whatever of those marks lies inside a body cut out of it. A drawing with no body draws its strokes at 3 instead. |

The derivation follows these rules, which a stroke drawing is judged against when its filled
variant comes out wrong — the fix is to the stroke drawing, since nothing else is drawn:

- **The outer edge is the stroke drawing's outer edge.** Take the stroke's outside boundary as the
  silhouette; the shape sits in the same place at the same size, so the two variants swap without
  a shift.
- **Interior features become knock-outs.** What was an inner stroke is cut out of the fill with
  `fill-rule="evenodd"`: a filled `CircleCheck` is a solid disc with the tick cut through it, a
  filled `Folder` is a solid folder whose tab is read from the silhouette, a filled `Image` is a
  solid frame with the dot and the mountains cut out. A knock-out keeps the stroke's 2-unit width
  and the same 3-unit gaps, so it still reads at 16px.
- **No strokes in the filled variant.** A detail that has no area — the hands of `Clock`, the slash of
  `EyeSlash`, the text lines of `FileText` — is drawn as a knock-out 2 units wide, never as a
  stroke over the fill.
- **A glyph with nothing to fill keeps its strokes, heavier.** Arrows, chevrons, `Plus`, `Minus`,
  `Cross`, `Check`, `Code` and the letterforms (`Bold`, `Italic`, `Heading`, `Font`…) have no
  body; the build draws their strokes at 3 instead.
- **A glyph that is already solid stays as it is.** `More`, `DragHandle` and the other all-dot
  glyphs look the same in both variants, and so do the silhouettes — `AirplaneRight`, `Accessibility`'s
  figure, the `Hand…` family — whose form is the filled shape itself.
- **Opacity survives.** A receding element keeps its `opacity="0.4"` in the filled variant.

## The vocabulary

A set is coherent when the same part is the same everywhere. These parts are drawn once and reused;
a glyph that needs one copies it, it does not redraw it.

| Part | On the 32 grid | On the 24 grid |
|---|---|---|
| **Arrowhead** | Two strokes meeting at the tip at 90°, each reaching 7 back and 7 across: for a right-pointing tip at (x, y): `M x-7 y-7 L x y L x-7 y+7`. "Scaled to n" replaces the 7 with n. | The same, reaching 5. |
| **Chevron** | An arrowhead with no shaft, scaled to 8, so it spans half the box (`ChevronDown`, `ChevronRight`…). | Scaled to 6. |
| **Page** | A rectangle from (8, 4) to (24, 28) with a folded corner: the fold cuts 7 units off the top-right, and a second path draws the fold flap. | From (6, 3) to (18, 21); the fold cuts 5. |
| **Folder** | Body from (4, 9) to (28, 27) with radius 3; a tab from (4, 9) rising to (13, 5) and across to (17, 9). Open variants swing the front face. | Body from (3, 7) to (21, 20), radius 2; a tab from (3, 7) rising to (10, 4) and across to (13, 7). |
| **Frame** | A rectangle from (4, 5) to (28, 27), radius 3 (`Image`, `Video`, `Table`, `CreditCard`). | From (3, 4) to (21, 20), radius 2. |
| **Circle** | Center (16, 16), radius 12, for badge-style glyphs (`CircleCheck`, `CircleCross`, `CirclePlay`, `CircleInfo`, `Prohibited`, `Clock`). The inner mark stays inside radius 7. | Center (12, 12), radius 9; the inner mark inside radius 5. |
| **Square** | (5, 5) to (27, 27), radius 3, for `CheckSquare`, `ExpandPlus`, `CollapseMinus`. | (4, 4) to (20, 20), radius 2. |
| **Stack** | Three horizontal lines, x 5 to 27, at y 9, 16, 23 (`AlignJustify`); the other align variants shorten the middle or both ends, and `List` prefixes each line with a tick. | x 4 to 20, at y 7, 12, 17. |
| **Slash** | One stroke from (5, 27) to (27, 5), laid over another glyph (`EyeSlash`, `Unpin`, `Mute`), whose outline breaks around it. A glyph the slash would tangle with instead says removed another way: `RemoveLink` breaks the chain, `RemoveAnchor` wears a cross badge. | From (4, 20) to (20, 4). |
| **Triangle** | A solid triangle whose corners are rounded: each corner is a quadratic curve controlled by the sharp vertex, joining the edges 4 units short of it (`TriangleUp`, `TriangleDown`, `TriangleRight`, `FastForward`); a small one, in `Indent` and `Outdent`, joins 2 short. | Joining 3 short; a small one 2. |
| **Plus** | Two strokes of length 8 crossing at a point: `M x-4 y L x+4 y M x y-4 L x y+4`; overlaid at the lower-right of a glyph it is centered at (23, 23). | Length 6: `M x-3 y L x+3 y M x y-3 L x y+3`; at the lower-right, centered at (17, 17). |
| **Person** | A head circle radius 5 at (16, 11) and shoulders as an arc from (7, 28) to (25, 28) rising to y 20. | Head radius 4 at (12, 8); shoulders from (5, 21) to (19, 21) rising to y 15. |
| **Tray** | A lipped tray from (4, 19) to (28, 27), radius 3 at its four corners, open along the top between x 10 and 22 (`Downloads`); a dipped tray of the same box whose rim dips from (10, 18) to (12, 21) across to (20, 21) and up to (22, 18) (`MailInbox`, `MailOutbox`); a bracket tray, the bottom edge alone from (5, 23) round to (27, 23) (`Download`, `Upload`); and an open box from (5, 16) round the bottom to (27, 16) (`Extract`). | Lipped from (3, 14) to (21, 20), radius 2, open between x 8 and 16; the dipped rim from (8, 13) to (9, 16) across to (15, 16) and up to (16, 13); the bracket from (4, 17) round to (20, 17); the open box from (4, 12) round to (20, 12). |
| **Envelope** | A body from (4, 7) to (28, 25), radius 3; a flap as a chevron from (4, 9) down to (16, 18) up to (28, 9) (`Unread`). `Read` lifts the flap open. | Body from (3, 5) to (21, 19), radius 2; the flap from (3, 7) down to (12, 13) up to (21, 7). |
| **Mailbox** | A box from (3, 7) to (29, 26): a domed left end of radius 6 from (3, 13) up to (9, 7), a lid across to (24, 7) rounding down through radius 5 to (29, 12), and radius-3 corners at the base; a door on the left, its edge curving from (9, 7) down through radius 6 to (15, 13) and on to (15, 23), rounding to (12, 26), with a handle from (7, 14) to (10, 14). Whatever the box holds sits in its open right half, centered on x 22 (`MailEmpty`, `MailInbox`, `MailOutbox`, `MailDraft`, `MailJunk`). | The box from (2, 5) to (22, 20) with radius-2 corners, its dome radius 5 and lid corner radius 4; the door's edge from (7, 5) to (10, 10) and down to (10, 18); no handle, there being no room for one clear of both wall and door; the contents centered on x 16. |
| **Badge** | A circle radius 5 centered at (25, 24) carrying one mark scaled to fit inside it; the glyph it sits on breaks around it, skirting it with an arc where an edge would otherwise cross it (`RemoveAnchor`). A badge worn on a glyph's upper right instead sits at (24, 9) with radius 6 (`CartPlus`, `CartCheck`, `CartCross`). | Radius 4 at (19, 18); upper right, radius 5 at (18, 6). |
| **Cloud** | A flat base from (6, 22) to (26, 22) with three lobes over it, of radius 6 from (6, 22) to (9, 11), radius 8 across to (24, 11) reaching y 6, and radius 6 down to (26, 22) (`Cloud`, `CloudUpload`). | Base from (5, 17) to (20, 17); lobes of radius 5 to (7, 8), 6 across to (18, 8), and 5 down. |
| **Pencil** | The `Pen` pencil, solid, in three pieces a unit apart on the diagonal: a body 7 wide from (6.5, 20.5)–(11.5, 25.5) to (20.5, 6.5)–(25.5, 11.5), a pointed tip from that body down to (4, 28), and a cap beyond it with a radius-4 rounded end reaching (28.5, 8.5)–(23.5, 3.5). Beside something it is drawn at three quarters — the 24 grid's pencil placed in the lower right (`Draft`, `Edit`). | A body 5.66 wide from (5, 15)–(9, 19) to (15, 5)–(19, 9), tip to (3, 21), cap to (17, 3)–(21, 7); beside something, a body 4.24 wide from (10, 17)–(13, 20) to (17, 10)–(20, 13). |
| **Chain link** | A capsule of radius 4, so its hole is 6 wide: room for the other link's band with 2 clear on each side. Two links interlock with the centers of their facing ends 3 apart, each band passing through the other's hole, and at each of the two crossings the under link breaks 4 units either side of the over link (`Link`, `HorizontalLink`). A broken chain keeps only the outer half of each link, pulled apart to either side of the Slash (`RemoveLink`, `RemoveHorizontalLink`). | The same on the diagonal; radius 3 on the horizontal axis, with the facing ends 2 apart. |

The composition table below describes each glyph on the 32 grid; its 24 drawing is the same
composition at three quarters, snapped to that grid's rules.

## The glyphs

One line per member of `Glyph`, in enum order. "Composition" says which parts to use and how; a
glyph with no parts named is drawn from scratch on the grid.

| Glyph | Composition |
|---|---|
| `Accessibility` | A figure in a wheelchair: the wheel as a circle radius 7 at (13, 21); the figure a filled silhouette sitting on the wheel's top, with a filled head radius 3 at (23, 4): a torso leaning back from its seat at y 15 to the shoulder at (19, 5)–(22, 8), an arm 3 wide reaching back along y 5 to an elbow at x 10 and dropping to the rim at (6, 15)–(9, 15), and a leg 3 wide out along y 12 to a rounded knee at (28, 15), its calf dropping back toward the wheel to a foot at (23, 24)–(26, 25). |
| `AddColumnLeft` | A two-column table: a Frame from (13, 4) to (29, 28) with a divider at x 21; a Plus centered at (7, 16) to its left. |
| `AddColumnRight` | A two-column table: a Frame from (3, 4) to (19, 28) with a divider at x 11; a Plus centered at (25, 16) to its right. |
| `AddRowAbove` | A two-row table: a Frame from (4, 13) to (28, 29) with a divider at y 21; a Plus centered at (16, 7) above it. |
| `AddRowBelow` | A two-row table: a Frame from (4, 3) to (28, 19) with a divider at y 11; a Plus centered at (16, 25) below it. |
| `AirplaneDown` | `AirplaneUp` turned top to bottom. |
| `AirplaneLeft` | `AirplaneRight` mirrored. |
| `AirplaneRight` | A plane seen from above, nose right, as one filled silhouette: a fuselage 4 tall from the tail at x 4 to a rounded nose at (29, 16); wings swept back from their roots at x 13–19 to rounded tips at (8, 4)–(12, 5) and (8, 28)–(12, 27); a tail fin either side from (7, 14) out to (4, 12) and from (7, 18) out to (4, 20), notched at (6, 16). |
| `AirplaneUp` | `AirplaneRight` turned to fly up the page: nose at (16, 3), wing tips at (4, 22) and (28, 22), tail fins at (12, 28) and (20, 28). |
| `AlarmClock` | A face as a circle radius 9 at (16, 18) with hands from (16, 13) down to the center and out to (20, 18); two bells as ticks from (6, 8) to (9, 5) and from (26, 8) to (23, 5). |
| `AlignCenter` | Stack; top and bottom lines full, middle line x 9–23, all centered. |
| `AlignJustify` | Stack, all three full width. |
| `AlignLeft` | Stack; middle line x 5–20, left-anchored. |
| `AlignRight` | Stack; middle line x 12–27, right-anchored. |
| `Anchor` | Ring radius 3 at (16, 7); a shaft down the center to y 27; a crossbar at y 13 from x 11 to 21; arms as two arcs of radius 11 from (5, 19) down through (16, 27) up to (27, 19); flukes as ticks from (3, 19) to (7, 19) and (25, 19) to (29, 19). |
| `Apps` | Nine filled dots of radius 3 at x and y 6, 16 and 26. |
| `Archive` | A lid as a rounded rect from (4, 6) to (28, 12); a box below it from (6, 12) down to (6, 27) across to (26, 27) up to (26, 12); a handle slot from (13, 18) to (19, 18). |
| `ArrowDown` | Shaft from (16, 5) to (16, 27); Arrowhead pointing down at (16, 27). |
| `ArrowLeft` | Shaft from (27, 16) to (5, 16); Arrowhead pointing left at (5, 16). |
| `ArrowRight` | Shaft from (5, 16) to (27, 16); Arrowhead pointing right at (27, 16). |
| `ArrowTurnDownRight` | A shaft from (8, 5) down to y 18, rounding through radius 3 to run right to (26, 21) with an arrowhead scaled to 6 there. |
| `ArrowUp` | Shaft from (16, 27) to (16, 5); Arrowhead pointing up at (16, 5). |
| `AtSign` | A circle radius 5 at (16, 16); a tail from (21, 12) down to (21, 19) rounding into a hook to (27, 19) and up to (27, 16); an outer arc of radius 11 around (16, 16) from (27, 16) over the top and left round to (23, 25). |
| `Attachment` | A paper clip on the diagonal, one wire on four parallel lines 4 apart: from (26, 18) down the outer line to (18, 26), round a radius-8 bend to (6, 14), up the far line to (14, 6), round a radius-6 loop to (22, 14), down the middle line to (17, 19), round a radius-3 bend to (13, 15), and back up the inner line to (18, 10), stopping at the loop's center. |
| `Audio` | A beamed pair of notes: two note heads (filled circles radius 3) at (9, 23) and (23, 20), stems up to y 8 and y 5, a beam joining the stem tops. |
| `Auto` | A bolt as one closed zigzag: (18, 3) down-left to (8, 18), across to (15, 18), down-left to (12, 29), up-right to (24, 13), across to (17, 13), back up to (18, 3). |
| `Barcode` | Seven bars from y 7 to 25, evenly spaced 4 apart from x 4 to 28. |
| `Battery` | A body from (3, 9) to (26, 23) with radius-2 corners; a terminal on its right from (26, 13) out to x 29 and back at y 19, with radius-1 corners. |
| `BatteryFull` | `Battery` with a filled bar from (7, 13) to (22, 19), radius 1, inside the body. |
| `Bell` | A bell: a dome from (7, 22) up to (16, 5) and down to (25, 22), closed along the bottom; a rim from (4, 22) to (28, 22); a clapper as a radius-3 arc under it from (13, 27) to (19, 27). |
| `BellSlash` | `Bell` under a Slash, broken 2 clear of it on either side: the dome runs from (7, 18) over the top to (20, 6) and resumes from (24, 14) down to (25, 22); the rim runs from (16, 22) to (28, 22); the clapper stays. |
| `Bluetooth` | A stem from (16, 3) to (16, 29); two chevrons out to x 24 from the stem's ends and back to it at y 15 and 17; two ticks from (8, 9) to (12, 12) and from (8, 23) to (12, 20), stopping clear of the stem. |
| `Bold` | A capital B built from strokes: a vertical at x 11 from y 7 to 25, two bowls to the right, the lower one larger. |
| `Bookmark` | A ribbon: (8, 4) down to (8, 28), up to the notch at (16, 22), up to (24, 28), up to (24, 4), closing across the top. |
| `Borders` | A square from (5, 5) to (27, 27) divided by a vertical and a horizontal line through its center. |
| `Brightness` | A disc as a circle radius 5 at the center with its right half filled; eight rays 3 long from radius 9 to 12 on the axes and about as long on the diagonals, from (7, 7) to (9, 9) and their mirrors. |
| `Broadcast` | A filled dot radius 3 at (16, 15) on a mast down to (16, 29); two waves either side as arcs of radius 7 and 13 about the dot, each spanning from 45° above to 45° below the horizontal. |
| `Bug` | An oval body from (11, 11) to (21, 27); a head arc across the top; three legs each side at y 15, 19, 23 reaching x 5 and x 27; two short antennae. The filled form keeps every leg and antenna as its own solid bar. |
| `Calculator` | A body from (6, 3) to (26, 29) with radius-3 corners; a display from (10, 6) to (22, 11) with radius-1 corners; nine keys as filled dots radius 1 at x 11, 16, 21 and y 15, 20, 25. |
| `Camera` | A body from (4, 9) to (28, 27) with radius-3 corners, its top edge rising over a hood from (10, 9) up to (12, 6) across to (20, 6) and down to (22, 9); a lens as a circle radius 4 at (16, 18). |
| `Cart` | A handle from (3, 5) to (6, 5) running down to (10, 20), curling through a radius-2 arc to (10, 24) and continuing as the undercarriage bar to (25, 24); the basket above it as a rim from (7, 8) to (28, 8), a far side down to (25, 19) and a floor back to (10, 19); two filled wheels radius 2 at (12, 27) and (24, 27), tangent to the bar. |
| `CartCheck` | `Cart` wearing a Badge of radius 6 at (24, 9) that carries a Check from (21, 9) through (23, 11) to (27, 7); the rim stops at (15, 8) and the far side, inside the badge's clearance, is left out. |
| `CartCross` | `CartCheck` with the badge carrying a cross from (22, 7) to (26, 11) and from (26, 7) to (22, 11). |
| `CartPlus` | `CartCheck` with the badge carrying a Plus of length 4 at (24, 9). |
| `Cast` | Frame open at its lower-left; three concentric arcs of radius 4, 8 and 12 centered at (4, 27) in that corner, the smallest a filled dot. |
| `Category` | A tag: a pentagon from (4, 8) to (18, 8), out to the point (28, 16), back to (18, 24), to (4, 24), closing; a filled dot radius 2 at (9, 16) as the eyelet. |
| `Cent` | A c as an arc of radius 8 about the center from (22, 10) round the left to (22, 22); a bar through it drawn only outside, from (16, 3) to (16, 5) and from (16, 27) to (16, 29). |
| `ChangeCase` | A capital A with legs from (2, 27) up to (10, 7) and down to (18, 27) and a bar at y 19; a small a as a ring radius 4 at (25, 23) with a stem at x 29 from y 19 to 27. |
| `Check` | A tick from (5, 17) down to (12, 24) and up to (27, 8). |
| `CheckSquare` | Square; the same inner tick. |
| `ChevronDown` | Chevron pointing down, tip at (16, 20). |
| `ChevronLeft` | Chevron pointing left, tip at (12, 16). |
| `ChevronRight` | Chevron pointing right, tip at (20, 16). |
| `ChevronUp` | Chevron pointing up, tip at (16, 12). |
| `Chip` | A body from (8, 8) to (24, 24) with radius-2 corners holding a square from (13, 13) to (19, 19); three pins 4 long on each side at 11, 16 and 21. |
| `CircleCheck` | Circle; the tick scaled to fit inside radius 7. |
| `CircleCross` | Circle; the diagonals of `Cross` scaled inside radius 5. |
| `CircleError` | Circle; an exclamation mark: a stem from (16, 10) to (16, 18) and a filled dot radius 1 at (16, 22). |
| `CircleHelp` | Circle; a question mark whose hook runs from (12, 12) over the top through (16, 9) to (20, 12) and down to (16, 17); a filled dot radius 1 at (16, 22). |
| `CircleInfo` | Circle; a filled dot radius 1 at (16, 11); a stem from (16, 15) to (16, 23). |
| `CirclePlay` | Circle; a right-pointing triangle (13, 11)–(13, 21)–(21, 16). |
| `CircleRecord` | Circle radius 12; a filled circle radius 6 at the center. |
| `CircleRun` | Circle; a filled right-pointing triangle (13, 10)–(13, 22)–(22, 16). |
| `ClearFormatting` | A capital T (bar y 7 from x 7 to 20, stem at x 13 down to y 25) with a Slash from (17, 27) to (28, 16) crossing its lower right. |
| `Clock` | Circle; hands from center to (16, 9) and to (21, 19). |
| `Cloud` | Cloud. |
| `CloudUpload` | Cloud; an ArrowUp with shaft (16, 28)–(16, 13) and arrowhead scaled to 5 at (16, 13) rising through the base, which breaks around the shaft, running from (6, 22) to (12, 22) and from (20, 22) to (26, 22). |
| `Code` | Two chevrons: one pointing left with its tip at (3, 16), one pointing right with its tip at (29, 16). |
| `CodeBranch` | Nodes as rings radius 3 at (10, 7), (10, 25) and (22, 9); a line between the two left nodes; a branch from (10, 18) curving up and right to the bottom of the right node at (22, 12). |
| `CodeCommit` | A ring radius 6 at the center on a line from (4, 16) to (28, 16), the line stopping at the ring. |
| `CodeCompare` | Two arrows: one from (10, 26) up to y 12, rounding radius 4 to run right to (17, 8) with a head there and a head pointing down at its foot; the other from (22, 6) down to y 20, rounding to run left to (15, 24) with a head there and a head pointing up at its top. |
| `CodeFork` | Nodes as rings radius 3 at (8, 7), (24, 7) and (16, 25); lines from the top nodes down to y 12, rounding radius 4 into a bar at y 16 from x 12 to 20, and a line from its middle down to the bottom node. |
| `CodeMerge` | Nodes as rings radius 3 at (10, 7), (10, 25) and (22, 16); a line between the two left nodes; a branch leaving the top node and curving down and right into the left of the right node at (19, 16). |
| `CodePullRequest` | Nodes as rings radius 3 at (8, 7), (8, 25) and (24, 25) with a line between the two left nodes; from the right node a line up to y 12, rounding radius 4 to run left to (17, 8) with an arrowhead there. |
| `Coin` | Circle; on its face an S 8 tall from (19, 12) back to (14, 12), round to (14, 16), across to (18, 16), round to (18, 20) and back to (13, 20). |
| `Coins` | A front coin as a circle radius 8 at (13, 19); a back coin as a circle radius 8 at (19, 13) drawn only where it shows, as an arc from (12, 10) over the top and right to (21, 21). |
| `Collapse` | Two arrows on the diagonal pointing inward: one from (4, 4) to (13, 13) with arrowhead scaled to 5 at (13, 13), one from (28, 28) to (19, 19) with arrowhead scaled to 5 at (19, 19). |
| `CollapseMinus` | Square; a line from (11, 16) to (21, 16). |
| `ColorPicker` | `Eyedropper` fitted into the upper right, spanning (8, 3) to (29, 24); a solid drop in the free corner, its point at (5, 22) over a radius-2.5 bowl at (5, 26.5). |
| `Columns` | Frame; one vertical divider at x 16. |
| `Comment` | A speech bubble: a rounded rect from (4, 5) to (28, 23) with a tail dropping from (9, 23) to (7, 28) back to (15, 23). |
| `Compress` | Two arrows pressing toward a line: an ArrowDown with shaft (16, 4)–(16, 12) and an ArrowUp with shaft (16, 28)–(16, 20), arrowheads scaled to 4; the line from (7, 16) to (25, 16). |
| `Contacts` | A book: a rounded rect from (7, 4) to (27, 28) with two binding ticks on its spine from (4, 10) to (7, 10) and (4, 22) to (7, 22); Person reduced to head radius 3 at (17, 13) and shoulders from (11, 23) to (23, 23) rising to y 18. |
| `Copy` | Two overlapping rounded squares: back one (5, 5)–(20, 20), front one (12, 12)–(27, 27); the back one is interrupted where the front covers it. |
| `Count` | A 1 with its flag from (3, 11) to (6, 8) and stem down to (6, 22); a comma from (9, 22) to (8, 25); a 2 from x 12 to 18 whose bowl is a half circle radius 3 at y 11 sweeping down to a base at y 22; three filled dots radius 1 at x 21, 25, 29 on the baseline y 22. |
| `CountBox` | A solid box from (3, 8) to (29, 24) with radius-3 corners, its three cells centered at x 8.5, 16 and 23.5 and its digits knocked out between y 11 and 21: a 0 as a stadium ring 5 wide with a 2-wide hole, a 1 as a 2-wide stem with a flag to the left, and in the third cell the bottom of a 2 (a diagonal into a base) above the top of a 3 (a bar into a hook), the digit caught mid-roll. |
| `CountDividedBox` | `CountBox` with a 1-unit divider knocked out at x 12 and x 20 from y 10 to 22, on the cells' shared edges. |
| `CreditCard` | Frame; a magnetic stripe as a line from (4, 13) to (28, 13); two embossed dashes at y 21 from x 8 to 12 and from x 15 to 18. |
| `Crop` | Two overlapping L marks: one from (9, 4) down to (9, 23) across to (28, 23); the other from (4, 9) across to (23, 9) down to (23, 28). |
| `Cross` | Two diagonals: (8, 8)–(24, 24) and (24, 8)–(8, 24). |
| `Crosshair` | Circle radius 9; four ticks from radius 9 to radius 13 at the compass points. |
| `Crown` | Fitted from the reference drawing: a crown as one outline from (6, 10) down to a dip at (11, 16), up to the peak at (16, 5), down to (21, 16), up to (26, 10), then down the sides to a base from (25, 26) to (7, 26); three finials as solid dots radius 2 at (6, 8), (16, 3) and (26, 8); a band as a curve from (8, 21) sagging through y 22.5 to (24, 21); a cushion line beneath from (7, 29) to (25, 29). |
| `Cursor` | A pointer as one outline: from (7, 4) down to (7, 25), out to (12, 20), down to (16, 28), across to (20, 26), back up to (16, 18), out to (23, 18) and closing. |
| `Cut` | Two ring handles radius 4 at (9, 24) and (23, 24); blades from each ring crossing at (16, 15) up to (9, 4) and (23, 4). |
| `Dashboard` | Four tiles of radius 2 in two columns, x 4 to 13 and 19 to 28: on the left a tall tile from y 4 to 18 over a short one from 21 to 28; on the right a short tile from y 4 to 11 over a tall one from 14 to 28. |
| `Data` | Folder; a filled five-pointed star on its body, top point (16, 13), outer points (22, 17), (20, 24), (12, 24) and (10, 17). |
| `Database` | A cylinder: a top ellipse centered at (16, 8) with radii 10 and 4; sides at x 6 and x 26 from y 8 to y 24; a bottom arc joining them; a band arc across the middle at y 16. |
| `Debug` | `Bug` drawn with a divided back: the oval body from (10, 10) to (22, 28) with a center line down it, a head arc across the top, two legs each side, and two short antennae. |
| `Delete` | A waste bin: a lid line from (5, 8) to (27, 8) with a handle rising from (12, 8) to a radius-1 top from (13, 5) to (19, 5) and down to (20, 8); a body from (8, 8) down to (9, 24), round radius-3 corners across to (23, 24), up to (24, 8); two inner lines at x 14 and x 18 from y 13 to 22. |
| `Desktop` | A monitor from (4, 5) to (28, 21) with radius-2 corners on a stem from (16, 21) to (16, 26) and a foot from (10, 26) to (22, 26). |
| `DetailsView` | Frame; horizontal dividers at y 13 and y 20; below the first, vertical dividers at x 12 and x 20, so the frame reads as a grid of cells rather than a table with a header. |
| `Devices` | A laptop and a phone: the laptop's screen a rounded rect of radius 2 from (3, 7) open on its right side, its top and bottom edges ending at x 17 where the phone breaks them, with a base line from (3, 25) to (17, 25); the phone a rounded rect of radius 2 from (21, 11) to (29, 28). |
| `DollarSign` | An S from strokes through (16, 5), (9, 11), (16, 16), (23, 21) and (16, 27) within x 9 to 23; a vertical bar through it from (16, 3) to (16, 5) and from (16, 27) to (16, 29). |
| `Download` | An ArrowDown with shaft from (16, 5) to (16, 20), arrowhead at (16, 20); the bracket Tray beneath it. |
| `Downloads` | The lipped Tray; an ArrowDown shaft (16, 4)–(16, 17) with arrowhead scaled to 5 at (16, 17). |
| `Draft` | Three lines from x 4 at y 9, 16 and 23 reaching x 14, 10 and 7; the Pencil at three quarters in the lower right, stopping 2 clear of the lines. |
| `DragHandle` | Six filled dots radius 2 at x 12 and 20, y 8, 16, 24. |
| `Earth` | Circle; inside it the continents as solid silhouettes fitted from traced artwork, the Americas down the left and Europe and Africa on the right, kept within 10 of the center. |
| `Edit` | A box from (4, 8) to (24, 28) with radius-3 corners, open at its top right between (14, 8) and (24, 18); the Pencil at three quarters lying across that opening, its tip at (11, 21) inside the box and its cap reaching (29, 7). |
| `Emoji` | Circle; two filled dots radius 1 at (12, 13) and (20, 13); a smile arc from (11, 19) to (21, 19) dipping to y 22. |
| `Empty` | A ring of radius 12 under a Slash, broken around it: one arc from (27, 11) over the right and bottom to (11, 27), the other from (5, 21) over the left and top to (21, 5). |
| `Eraser` | A solid eraser fitted from the reference drawing: a block on the diagonal with radius-4 rounded ends from (3.5, 17.5) up to (24, 3) and down to (16.5, 26), a slit knocked out across it from (11, 12) to (21.5, 19) as its sleeve edge; a solid ground line with round ends from (5, 26.5) to (27.5, 29). |
| `EuroSign` | A C as an arc of radius 10 from (24, 8) round the left to (24, 24); two bars at y 13 and y 19 from x 5 to 19. |
| `Events` | Frame reduced to (4, 6)–(28, 28) with a header line at y 12 and two binding ticks rising from (10, 6) and (22, 6) to y 3; three filled dots radius 1 at (11, 20), (16, 20), (21, 20). |
| `Exit` | A door frame: an open bracket from (15, 4) to (5, 4) down to (5, 28) to (15, 28); an ArrowRight from (12, 16) to (28, 16), arrowhead at (28, 16) scaled to 5. |
| `Expand` | Two arrows on the diagonal pointing outward: one from (13, 13) to (4, 4) with arrowhead scaled to 5 at (4, 4), one from (19, 19) to (28, 28) with arrowhead scaled to 5 at (28, 28). |
| `ExpandPlus` | Square; a Plus of arms 5 at (16, 16). |
| `Extensions` | A puzzle piece: a body from (6, 9) to (28, 28), radius 3, with a nub of radius 3 on its top edge between x 14 and 20 and a notch of radius 3 in its left edge between y 14 and 20. |
| `Extract` | The open box Tray; an ArrowUp with shaft (16, 20)–(16, 5) and arrowhead scaled to 5 at (16, 5). |
| `Eye` | A lens: two arcs of radius 15 from (3, 16) to (29, 16) and back, meeting at the corners and reaching y 8 and 24 at the middle; a pupil circle radius 4 at (16, 16). |
| `Eyedropper` | A solid dropper on the diagonal, fitted from the reference drawing to fill the live area: a rounded bulb at the upper right joined to a crossbar collar with round-capped ends, and a separate tube from (15.5, 10) tapering to a rounded tip at (3, 26.5). |
| `EyeSlash` | The `Eye` outline under a Slash, broken 2 units clear of the slash on either side: the upper lid runs from (3, 16) to (17, 9) and resumes at (26, 12); the lower from (29, 16) to (15, 23) and resumes at (6, 20). The pupil lies within the break and is left out. |
| `FastForward` | Two Triangles pointing right, (3, 8)–(3, 24)–(14, 16) and (13, 8)–(13, 24)–(24, 16), against a bar 2 wide from (27, 8) to (29, 24) with radius-1 ends. |
| `File` | Page. |
| `FileArchive` | Page; two dotted lines inside it, each three filled dots radius 1 at x 12, 16 and 20, at y 17 and 22. |
| `FileCode` | Page; inside it a small `Code` pair at tips (11, 20) and (21, 20), scaled to 3, so their inner ends stand 4 apart. |
| `FileText` | Page; three text lines at y 16, 20, 24 from x 12 to 20. |
| `Filter` | A funnel: (4, 7) to (28, 7), sides converging to (19, 17) and (13, 17), then down to y 27 and y 24. |
| `FindAndReplace` | Three lines x 5–27 at y 8, x 5–17 at y 16, x 5–13 at y 24; a Search reduced to radius 5 at (22, 21) with a handle to (28, 27). |
| `Flag` | A pole from (7, 28) to (7, 4); a waving flag from the pole at y 5 to x 25 and back at y 17. |
| `Flashlight` | A torch on the diagonal as solid pieces a unit apart: a trapezoidal head from a neck 8 wide at (15, 11)–(21, 17) to a front 13 wide at (17.5, 5.5)–(26.5, 14.5); a straight body 8 wide from (14, 12)–(20, 18) back to (5, 21)–(11, 27) carrying a ring button, a radius-2 hole with a radius-1 dot, at (12.5, 19.5); a cap from (4, 22)–(10, 28) with a radius-4 rounded end; three rays from the head, (22, 2)–(22, 4), (26, 6)–(28, 4) and (28, 10)–(30, 10). |
| `Folder` | Folder. |
| `FolderAdd` | Folder; a Plus centered at (16, 19). |
| `FolderOpen` | Folder with the front face swung open: the front rectangle's lower edge stays, its top edge is a line from (7, 15) to (29, 15) and the right side leans in to (25, 27). |
| `FolderTree` | Three nodes as rounded rects 7 by 5, radius 1: a root at (4, 4) and two children at (21, 14) and (21, 23); a stem from (7, 9) down to (7, 25) with branches across to x 21 at y 16 and y 25. |
| `Font` | A capital A: legs from (5, 27) and (27, 27) meeting at (16, 5), crossbar from (9, 19) to (23, 19). |
| `FontColor` | A letter A with legs from (6, 22) up to (16, 4) and down to (26, 22) and a bar at y 15; a bar from (4, 28) to (28, 28) under it. |
| `FontSize` | A large A as in `Font` but from x 4 to 20 with apex (12, 7); a small A from x 20 to 28 with apex (24, 16). |
| `FontSizeDecrease` | The A of `FontSizeIncrease` beside an ArrowDown from (27, 8) to (27, 20) with a head 3 wide. |
| `FontSizeIncrease` | A letter A with legs from (4, 27) up to (13, 5) and down to (22, 27) and a bar at y 19; an ArrowUp from (27, 20) to (27, 8) with a head 3 wide. |
| `ForkKnife` | A fork on the left: three tines at x 5, 8 and 11 from y 4 down to a bowl that closes at y 14, and a handle from (8, 14) to (8, 28). A knife on the right: a handle from (25, 4) to (25, 28) and a blade curving out from its top to (20, 12), down to (20, 16) and back to the handle, drawn solid. |
| `Forward` | A line from (4, 28) up to y 16, turning through a corner of radius 4 to run right along y 11 to (28, 11); an arrowhead at (28, 11) pointing right. |
| `GenerativeAI` | Two four-pointed stars whose sides curve in to the center: a large one centered at (14, 18) reaching 11 to each point, and a small one centered at (25, 8) reaching 4. |
| `Glasses` | Two lenses as rounded rectangles from (3, 12) to (14, 22) and (18, 12) to (29, 22) with radius-3 corners, joined by a bridge from (14, 17) to (18, 17); a temple rising outward from each top outer corner, from (4, 12) to (2, 9) and from (28, 12) to (30, 9). |
| `Globe` | Circle; a meridian as a lens from (16, 4) to (16, 28) bulging 7 either side of center; parallels from (4, 16) to (28, 16) and from (6, 10) to (26, 10) and (6, 22) to (26, 22). |
| `Graduation` | A mortarboard: a rhombus with corners (16, 6), (29, 12), (16, 18) and (3, 12); the cap below it from (8, 15) down to (8, 19), round an elliptical bottom of radii 8 and 4 to (24, 19) and up to (24, 15); a tassel from (29, 12) down to (29, 21). |
| `GridView` | Four rounded squares of side 9 at (4, 4), (19, 4), (4, 19), (19, 19). |
| `Group` | Three rhombi seen edge-on: the top one a closed diamond with vertices (16, 4), (28, 10), (16, 16), (4, 10); below it two chevrons from (4, 16) to (16, 22) to (28, 16) and from (4, 22) to (16, 28) to (28, 22). |
| `Hand…` | The fourteen hands are solid silhouettes, each traced from the reference sheet and fitted to fill the live area: `HandRaisedLeft` and `HandRaisedRight` (an open palm, thumb to the named side), `HandPointDown` (a fist with the index extended), `HandPointUp` (`HandPointDown` turned a half turn), `HandPointLeft` and `HandPointRight`, `HandThumbUp` and `HandThumbDown` (the latter the former turned top to bottom), `HandPeace`, `HandCall`, `HandPen`, `Handshake`, and `HandWave` and `HandClap`, which carry their motion and impact marks as separate solid strokes. |
| `HashtagSign` | Two slanted strokes from (12, 5) to (9, 27) and from (23, 5) to (20, 27); two bars from (6, 12) to (27, 12) and from (5, 20) to (26, 20). |
| `Heading` | A capital H: verticals at x 8 and 24 from y 7 to 25, crossbar at y 16. |
| `Heart` | A heart from its notch at (16, 9) out to round lobes at (4, 11) and (28, 11), meeting at the point (16, 28). |
| `Highlight` | A marker, solid, in three pieces a unit apart on the diagonal: a body 8.5 wide from (7, 19)–(13, 25) to (19, 7)–(25, 13), a chisel tip narrowing from it to a flat end at (4.5, 24.5)–(7.5, 27.5), and a cap from (20, 6)–(26, 12) to (23, 3)–(29, 9). |
| `History` | A circular arrow: an arc from (7, 11) around the right side to (7, 21), arrowhead at (7, 11) pointing down-left; hands from center to (16, 11) and (20, 17). |
| `Home` | A house: a roof peaking at (16, 5) with eaves at (5, 14) and (27, 14), walls down to a base from y 25 with radius-3 corners at y 28; a door from (13, 28) up to (13, 20) across to (19, 20) and down. |
| `HorizontalLink` | Two Chain links interlocked on the horizontal axis, y 12 to 20: the left with end centers (7, 16) and (14, 16), the right with (17, 16) and (24, 16); the right link passes over at the top, the left at the bottom. |
| `Hourglass` | Caps from x 6 to 26 at y 4 and y 28; two bulbs as sides that drop straight from each cap to y 9, curve in to a neck 4 wide at y 16 and curve back out to y 23, then drop straight to the other cap. |
| `HourglassProgress` | `Hourglass` with sand: a solid wedge from (11, 9) to (21, 9) down to a point at (16, 14) in the top bulb, two falling grains as dots radius 1 at (16, 19) and (16, 22), and a solid mound from (11, 25) over (16, 22) to (21, 25) down to y 27 in the bottom bulb. |
| `Image` | Frame; a filled dot radius 2 at (11, 12); a mountain range: (4, 23) up to (12, 15) to (17, 20) to (21, 16) to (28, 23). |
| `Images` | `Image` reduced to a frame from (9, 8) to (28, 27) with its content; behind it at `opacity 0.4` a second frame from (4, 4) to (23, 23). |
| `Incognito` | A hat: a brim from (4, 15) to (28, 15) with a crown rising from (9, 15) and (23, 15) to a rounded top at y 5; below, two lens circles radius 4 at (10, 23) and (22, 23) joined by a bridge from (14, 23) to (18, 23). |
| `Indent` | Four lines at y 7, 13, 19 and 25, the outer two from x 5 to 27 and the inner two from x 15 to 27; a Triangle pointing right, (5, 12)–(5, 20)–(11, 16), before the inner lines. |
| `Inspect` | `Search` with an eye in its lens: two arcs of radius 7 from (7, 13) to (19, 13) and back, and a solid pupil radius 1.5 at (13, 13). |
| `Italic` | A slanted capital I: bar at y 7 from x 13 to 24, bar at y 25 from x 8 to 19, stem from (19, 7) to (13, 25). |
| `Key` | A key as a solid silhouette: a bow as a disc radius 6 at (22, 10) with a radius-2 hole; a shaft 4 wide down the diagonal from the bow to (3.5, 25.5)–(6.5, 28.5); two teeth on its underside, 3.5 long, at the tip and 5.66 further up. |
| `KeyAlt` | A line from (4, 8) to (11, 8) stepping down the diagonal to (21, 24) and on to (28, 24); a second line from (20, 8) to (28, 8). |
| `KeyBackspace` | A tag from (11, 6) to (26, 6) with radius-3 corners at the right down to (11, 26), pointing left to (3, 16); a Cross of length 6 centered at (18, 16). |
| `Keyboard` | A body from (3, 7) to (29, 25) with radius-3 corners; keys as solid 2-unit squares, five in a row at y 11 from x 7 to 23 every 4 and four staggered below at y 15 from x 9 to 21; a space bar line at y 21 from x 11 to 21. |
| `KeyboardLeft` | `Keyboard` narrowed to a body from (11, 8) to (29, 24), its keys three squares at y 11 from x 15 to 23 and two at y 15 from x 17 to 21 and its space bar from (16, 20) to (24, 20); a solid triangle standing clear to its left, pointing left from (8, 12)–(8, 20) to (3, 16). |
| `KeyboardRight` | `KeyboardLeft` mirrored: the body from (3, 8) to (21, 24) with its keys at x 7 to 15 and x 9 to 13, the space bar from (8, 20) to (16, 20), and the triangle pointing right from (24, 12)–(24, 20) to (29, 16). |
| `KeyCapsLock` | The `KeyShift` arrow reduced to a tip at (16, 3), shoulders at (27, 14) and (5, 14) and a stem to y 20; a bar from (11, 24) to (21, 27) below it. |
| `KeyCmd` | A square from (11, 11) to (21, 21) whose sides run on past each corner into a radius-4 loop, the loops centered at (7, 7), (25, 7), (25, 25) and (7, 25). |
| `KeyCtrl` | A chevron scaled to 10 pointing up, its tip at (16, 10). |
| `KeyShift` | A hollow arrow: tip at (16, 4), shoulders at (28, 16) and (4, 16), a stem 12 wide from y 16 down to y 26. |
| `KeyWin` | Four panes 9 square with radius-1 corners, at (5, 5), (18, 5), (5, 18) and (18, 18). |
| `Laptop` | A screen from (5, 6) to (27, 21) with radius-2 top corners; a base from (3, 25) to (29, 25). |
| `Lasso` | A loop as a rounded oval from (4, 12) to (28, 12) between y 4 and 20; a rope tail as a curve from (10, 19) swinging out and back to (10, 28). |
| `Layout` | Frame; a horizontal divider at y 12; below it a vertical divider at x 12. |
| `LayoutBottom` | Frame; a horizontal divider at y 21; the lower pane filled at opacity 0.4. |
| `LayoutLeft` | Frame; a vertical divider at x 12; the left pane filled at opacity 0.4. |
| `LayoutRight` | Frame; a vertical divider at x 20; the right pane filled at opacity 0.4. |
| `LayoutTop` | Frame; a horizontal divider at y 11; the upper pane filled at opacity 0.4. |
| `Lightbulb` | A bulb: a dome of radius 9 at (16, 12) whose sides fall to (11, 23) and (21, 23), closed along the bottom; a filament inside as an arch from (13, 19) up over radius 3 to (19, 19); two base lines from (11, 26) to (21, 26) and from (12, 29) to (20, 29). |
| `LineHeight` | A vertical line from (6, 4) to (6, 28) with chevrons scaled to 3 at both ends; three lines from x 13 to 29 at y 9, 16, 23. |
| `LineSpacing` | Three lines at y 8, 16, 24 from x 13 to 28; a two-headed vertical arrow at x 6 from y 6 to 26 with heads 3 wide. |
| `Link` | Two Chain links interlocked on the diagonal, their sides on the lines x + y = 26 and 38: the lower with end centers (9, 23) and (15, 17), the upper with (17, 15) and (23, 9); the upper link passes over at the lower-left crossing, the lower at the upper-right one. |
| `List` | Three rows at y 9, 16, 23, each a short tick from x 5 to 7 and a line from x 11 to 27. |
| `ListBullet` | Three lines x 12–27 at y 9, 16, 23; three filled dots radius 2 at x 7. |
| `ListMultilevel` | Three lines ending at x 27 at y 8, 16, 24, starting at x 11, 15, 19, each with a filled bullet radius 2 five to its left. |
| `ListNumbered` | Two rows: the digits 1 and 2 as strokes 10 tall within x 4 to 11, at y 3–13 and 18–28; a line from x 15 to 28 beside each, at y 8 and 23. Two rows, not three, because three digits cannot keep the gap. |
| `LocationArrow` | A navigation arrow as one outline: (27, 5), (5, 14), (15, 17), (18, 27). |
| `LocationArrowCircle` | Circle; `LocationArrow` reduced to its tip at (21, 11), left point at (10, 15), notch at (15, 17) and bottom point at (17, 22). |
| `Lock` | A body as a rounded rect from (6, 14) to (26, 28); a shackle arc from (10, 14) up over (16, 4) down to (22, 14); a keyhole as a filled dot radius 2 at (16, 20) with a stem down to (16, 24). |
| `LockOpen` | The `Lock` body and keyhole; the shackle, lifted, rises from (10, 14) over (16, 3) and ends free in the air at (22, 9), 3 clear of the body. |
| `MagicWand` | A solid wand 4 wide with rounded ends on the diagonal from (4, 28) to (17, 15); three four-pointed stars as in `GenerativeAI`, a large one centered at (23, 9) reaching 6 and two small ones at (9, 8) and (27, 21) reaching 3. |
| `MailDraft` | Mailbox; a small solid Pencil in its open half, reduced to its tip and body: the tip from (17.5, 21) to (18, 17.5)–(21, 20.5), the body 4 wide from (19, 16.5)–(22, 19.5) to (24, 11.5)–(27, 14.5), centered in the open half. |
| `MailEmpty` | Mailbox, with nothing in its open half. |
| `MailInbox` | Mailbox; an ArrowDown in its open half, shaft (22, 10)–(22, 22) with arrowhead scaled to 4 at (22, 22). |
| `MailJunk` | Mailbox; a Prohibited sign in its open half, a circle radius 4 at (23, 16) with a Slash inside it from (20, 19) to (26, 13). |
| `MailOutbox` | Mailbox; an ArrowUp in its open half, shaft (22, 22)–(22, 10) with arrowhead scaled to 4 at (22, 10). |
| `Maintenance` | The `Settings` gear centered at (15, 15) behind a wrench whose ring, radius 6, sits on the gear's center with its jaw open toward the upper left and its handle, 6 wide, running down the diagonal to a radius-3 end at (25, 25); the gear breaks around the handle, its outline stopping at (27, 17) and resuming at (17, 27), and its hub, under the ring, is left out. |
| `MathDivide` | A bar from (5, 16) to (27, 16) between filled dots radius 2 at (16, 9) and (16, 23). |
| `MathEquals` | Two bars from x 5 to 27 at y 12 and 20. |
| `MathGreaterThan` | A chevron from (8, 6) out to (24, 16) and back to (8, 26). |
| `MathGreaterThanOrEqual` | A chevron from (8, 5) out to (24, 13) and back to (8, 21) over a bar from (8, 27) to (24, 27). |
| `MathInfinity` | Two loops as one curve through the center, each reaching x 4 and 28 and y 8 to 24. |
| `MathLessThan` | A chevron from (24, 6) in to (8, 16) and back to (24, 26). |
| `MathLessThanOrEqual` | A chevron from (24, 5) in to (8, 13) and back to (24, 21) over a bar from (8, 27) to (24, 27). |
| `MathMultiply` | Two diagonals: (7, 7)–(25, 25) and (25, 7)–(7, 25). |
| `MathNotEqual` | `Equals` with a stroke from (20, 5) to (12, 27) through it. |
| `MathPercent` | A stroke from (6, 26) to (26, 6) between rings radius 4 at (9, 9) and (23, 23). |
| `MathPi` | A bar from (4, 9) to (28, 9) with legs at x 9 and 23 down to y 26. |
| `MathPlusMinus` | A Plus of arms 6 centered at (16, 11) over a bar from (10, 25) to (22, 25). |
| `MathSigma` | A capital sigma: from (26, 6) across to (6, 6), in to (16, 16), out to (6, 26) and across to (26, 26). |
| `MathSquareRoot` | A root sign: from (4, 18) down to (9, 26), up to (16, 6) and across to (28, 6). |
| `Maximize` | Four corner brackets: each an L of 7 by 7 at the four corners of the live area. |
| `Medal` | A disc as a circle radius 8 at (16, 21) bearing a solid five-pointed star of outer radius 4; a ribbon as two straps, each a closed band from (6, 3) across to (11, 3), down to (16, 11) and back to (12, 14), mirrored on the right. |
| `Message` | A bubble: a rounded rect from (5, 5) to (27, 24) whose lower-left corner is drawn out to a point at (5, 28); two text lines inside at y 12 and y 17 from x 11 to 21. |
| `Microphone` | A capsule from (11, 3) to (21, 19) with radius-5 ends; a cradle as a U from (7, 14) down through (16, 23) up to (25, 14); a stand from (16, 23) to (16, 29) on a foot from (10, 29) to (22, 29). |
| `Minimize` | `Maximize` turned inward: four L brackets of 7 by 7 whose corners sit at (11, 11), (21, 11), (11, 21) and (21, 21), each with its arms reaching out toward its corner of the live area. |
| `Minus` | A line from (5, 16) to (27, 16). |
| `Mobile` | A handset from (9, 3) to (23, 29) with radius-3 corners; a home button as a filled dot radius 1 at (16, 25). |
| `More` | Three solid dots radius 3 at (6, 16), (16, 16) and (26, 16). |
| `MoreVertical` | Three solid dots radius 3 at (16, 6), (16, 16) and (16, 26). |
| `Mouse` | A body from (9, 4) to (23, 28) with radius-7 ends; a scroll wheel as a line down the middle from (16, 9) to (16, 13). |
| `Move` | Two shafts crossing at the center, from (16, 4) to (16, 28) and (4, 16) to (28, 16), each end an arrowhead scaled to 4 so neighboring heads stay clear of each other. |
| `Mute` | The `SoundOutput` speaker body without its waves, under a Slash, its outline broken 2 units clear of the slash on either side: the cones edge stops at (17, 9) and resumes at (17, 21), and the boxs bottom edge stops at (6, 20). |
| `NavigationMenu` | Three full-width lines x 5–27 at y 8, 16, 24 — spaced one unit wider than `List`, so the two read as different things side by side. |
| `Network` | A top node as a square side 6 at (13, 4); two bottom nodes as squares side 6 at (4, 22) and (22, 22); a trunk from (16, 10) down to (16, 16); a bar at y 16 from x 7 to 25; drops from (7, 16) and (25, 16) down to y 22. |
| `NeuralNetwork` | A solid silhouette fitted from traced artwork: three layers of ring nodes — two on the left, three in the middle, two on the right — with every node joined to every node in the next layer, the connections crossing as the network's own form. |
| `Open` | Square open at its top-right, its edges stopping at (14, 5) and (27, 18); an arrow from (16, 16) to (27, 5) with a corner-bracket arrowhead of 7-unit arms at (27, 5), pointing up-right. |
| `Outdent` | The lines of `Indent` with the Triangle pointing left, (11, 12)–(11, 20)–(5, 16). |
| `Package` | A cube in three-quarter view: a hexagonal outline with top vertex (16, 4) and bottom (16, 28); interior edges from (16, 16) to the top-left, top-right and bottom; a tape line across the top-left face. |
| `Paintbrush` | A handle from (16, 3) down to a ferrule from (11, 15) to (21, 19); bristles below it flaring to (9, 27) and (23, 27) with radius-2 corners. |
| `PaintBucket` | A can from (6, 12) down to a radius-3 base at y 28 and (26, 12); its rim a flat oval on y 12 bulging 1 above and below; a handle as a radius-9 arc from (7, 12) over the top to (25, 12); a solid drip of paint hanging from the rim, from (12, 15) to (20, 15) narrowing to a radius-1.5 drop at (16, 23). |
| `PaintBucketPour` | A solid tipped bucket fitted from the reference drawing: a rounded body on the diagonal from (3, 17) to (25, 16), its paint level a line across at y 18 that opens the upper half, a handle as a round-capped bar from (6, 5.5) to (15.5, 12) crossing the rim; a solid drop beside it, its point at (24.5, 17.5) over a radius-5 bowl at (24.5, 25.5). |
| `Palette` | A painter's palette: a rounded body from (2, 16) up over the top at (16, 4) and round to (29, 14), whose lower right folds in through an S-curve — down to (20, 22) and back out to (23, 19) — into a small cup ending at the base (16, 28); four solid wells radius 2 at (10, 12), (15, 9), (21, 10) and (10, 19). |
| `Paragraph` | A pilcrow: a filled bowl on the left (a half-circle from (9, 5) to (9, 16) bulging left), two stems at x 15 and x 20 from y 5 to 27, a top bar from x 9 to 25. |
| `ParagraphDirectionLtr` | A three-quarter `Paragraph` on the left — bowl from (6, 6) to (6, 14), bar to x 18, stems at x 10 and 14 down to y 24 — beside an ArrowRight from (21, 16) to (28, 16) with a head 4 wide. |
| `ParagraphDirectionRtl` | Mirror of `ParagraphDirectionLtr`: the pilcrow on the right, its bowl bulging right, beside an arrow pointing left from (11, 16) to (4, 16). |
| `PasswordField` | Frame reduced to (4, 10)–(28, 22); three filled dots radius 2 inside it at x 9, 16 and 23, masking the characters. |
| `Paste` | A clipboard: a rounded rect (7, 7)–(25, 28); a clip at the top from (12, 4) to (20, 4) with sides down to y 9; two lines inside at y 16 and 21. |
| `PawPrint` | Fitted from the reference drawing: a pad as a rounded heart from its top at (16, 15) out to lobes at (7, 25) and (25, 25) and down to a notch at (16, 27); four toes as outlined ovals about 4 by 6, the inner pair upright at (12, 7) and (20, 7), the outer pair leaning outward at (6, 12) and (26, 12). |
| `Pen` | Pencil. |
| `PenNib` | A solid nib pointing down: a flat top from (8, 4) to (24, 4), sides bulging out to (28, 15) and (4, 15) and tapering to the tip at (16, 29); a breather hole radius 2.5 at (16, 15) and a slit 1 wide from the hole down to (16, 27), both knocked out. |
| `Phone` | A handset on the diagonal as one outline: an earpiece block from (4, 4) with a radius-3 corner, its lip from (11, 4) to (14, 11) and in to (11, 13); a bar 6 wide down to the mouthpiece, whose lip runs from (19, 21) to (21, 18) out to (28, 21), with a radius-3 corner at (28, 28); the back edge from (18, 28) up to (4, 14). Every other corner is softened by a quadratic curve joining its edges 2 units short of the vertex. |
| `PhoneSlash` | `Phone` under a Slash, its bar broken 2 clear of the slash on either side: the earpiece's edges stop at (12, 14) and (8, 18), the mouthpiece's resume at (18, 20) and (14, 24). |
| `Pin` | A pushpin: a cap line from (9, 4) to (23, 4); a body whose sides run from (12, 4) and (20, 4) down to (11, 14) and (21, 14), then flare through quadratic curves to a base from (7, 20) to (25, 20); a needle from (16, 20) to (16, 28). |
| `Plan` | A board: the Frame with three columns inside it as bars at x 10, 16 and 22, each starting at y 10 and descending to y 22, 18 and 14. |
| `Plus` | Two lines: (16, 5)–(16, 27) and (5, 16)–(27, 16). |
| `Poll` | A chart: the Frame with three bars inside it at x 10, 16 and 22, each rising from y 22 to y 16, 10 and 18. |
| `PoundSign` | A hook as an arc of radius 5 from (22, 8) over the top to (12, 8) continuing down to (12, 20), a foot from (12, 20) to (8, 26) across to (24, 26); a bar from (8, 16) to (18, 16). |
| `Power` | An arc of radius 10 about the center open at the top, from (10, 8) round the bottom to (22, 8); a stem from (16, 4) to (16, 15). |
| `Preview` | Page; an eye on it as a lens from (11, 19) to (21, 19) reaching y 15 and 23, with a solid pupil radius 1.5 at (16, 19). |
| `Print` | A body as a rounded rect from (4, 12) to (28, 24); a sheet rising out of the top from (9, 12) up to (9, 4) across to (23, 4) down to (23, 12); a sheet emerging at the bottom from (9, 24) down to (9, 28) across to (23, 28) up to (23, 24); a filled dot radius 1 at (24, 16). |
| `Prohibited` | Circle; a Slash inside it from (8, 24) to (24, 8). |
| `Properties` | Three lines x 5–27 at y 9, 16, 23; on each a filled knob circle radius 3, at x 20, 11 and 17 respectively. |
| `Publish` | An upright rocket: a body whose nose is at (16, 3), whose sides curve out to their widest at y 18 and taper to a base from (12, 23) to (20, 23); a porthole circle radius 2 at (16, 13); a fin each side from the body at y 15 out and down to (7, 19) and (25, 19), dropping to y 24 and turning back in to (12, 22) and (20, 22); an exhaust from (14, 23) down to a point at (16, 29) and back up to (18, 23). |
| `Quote` | An opening and a closing quotation mark on the diagonal: the opening mark a solid dot radius 4 at (9, 14) with a tail curving from its upper-left up to (11, 3); the closing mark the same rotated a half turn, a dot at (23, 18) with a tail curving from its lower-right down to (21, 29). |
| `RadioButton` | Circle radius 12; a filled circle radius 5 at the center. |
| `Ranking` | A podium as one outline: steps from (4, 16), (12, 8) and (20, 20) down to a base at y 28, with dividers at x 12 and 20. |
| `Read` | An envelope with its flap open: a body from (4, 14) down to (4, 27) across to (28, 27) up to (28, 14); the flap as a chevron from (4, 14) up to (16, 5) down to (28, 14); the fold as a chevron from (4, 14) down to (16, 22) up to (28, 14). |
| `Redo` | A curve from (5, 23) rising and sweeping right to (27, 13); an arrowhead scaled to 6 at (27, 13) pointing right. |
| `Refresh` | One arc of radius 12 about the center, open at the upper right: from (25, 23) clockwise round the bottom, left and top to (25, 9), ending in the same corner-bracket arrowhead as `Sync`. |
| `RemoveAnchor` | `Anchor` with its right arm and fluke left off, wearing a Badge that carries a Close scaled to fit. |
| `RemoveHorizontalLink` | Two broken Chain links either side of a Slash: the left keeps its outer end at (6, 16), its top side to (10, 12) and a tail curling to (13, 13); the right is the same turned about the center. |
| `RemoveLink` | Two broken Chain links either side of a Slash mirrored onto the other diagonal, from (5, 5) to (27, 27), because the chain itself lies along the Slash's own: the lower link keeps its outer end at (6, 26), its sides to (13, 25) and (7, 19), and a tail curling to (12, 18); the upper is the same turned about the center. |
| `Reply` | A line from (28, 28) up to y 16, turning through a corner of radius 4 to run left along y 11 to (4, 11); an arrowhead at (4, 11) pointing left. |
| `Rewind` | `FastForward` mirrored: two Triangles pointing left, (29, 8)–(29, 24)–(18, 16) and (19, 8)–(19, 24)–(8, 16), against a bar 2 wide from (3, 8) to (5, 24) with radius-1 ends. |
| `Rotate` | One clockwise arc of radius 10 around (16, 16) from the top (16, 6) around the right and bottom to (7, 19), ending in an Arrowhead scaled to 4 pointing up at (7, 19). |
| `Rows` | Frame; one horizontal divider at y 16. |
| `Ruler` | A bar on the diagonal with corners (4, 22), (22, 4), (28, 10) and (10, 28); four scale marks from its upper edge inward, from (7, 19), (11, 15), (15, 11) and (19, 7), each 2 across and 2 down. |
| `Rules` | A life ring: Circle with an inner circle of radius 5, and four spokes on the diagonals joining them, from (8, 8) to (12, 12) and its three reflections. |
| `Save` | A floppy disk: a rounded square (4, 4)–(28, 28) with the top-right corner cut at 45°; a label slot from (9, 4) to (9, 12) across to (20, 12) up to (20, 4); a lower drawer from (8, 28) up to (8, 19) across to (24, 19) down to (24, 28). |
| `Search` | Circle radius 9 at (13, 13); a handle from (20, 20) to (28, 28). |
| `Security` | A shield: a point at (16, 3), shoulders at (5, 7) and (27, 7), straight sides to y 15, then curving to a tip at (16, 29); a Check inside it from (11, 16) through (15, 20) to (22, 12). |
| `Select` | The dashed selection frame of `SelectAll`, empty. |
| `SelectAll` | A dashed selection frame: the Square drawn as four corner brackets of 6 by 6 plus a 4-long dash centered on each side; a smaller solid square from (11, 11) to (21, 21) inside it. |
| `SelectNone` | The same dashed selection frame as `SelectAll`, empty, with a Slash; the frame breaks around it, so the lower-left and upper-right corner brackets it would pass through are dropped. |
| `Send` | A paper plane: a triangle with its nose at (28, 4) and tail corners at (4, 14) and (13, 28); a fold line from (28, 4) to (13, 19). |
| `Session` | Person reduced to head radius 4 at (16, 13) and shoulders from (9, 26) to (23, 26) rising to y 20; four corner brackets 6 by 6 at the corners of the live area. |
| `Settings` | A gear: a ring radius 4 at the center; an outer outline with eight teeth reaching radius 13; one closed path. |
| `Shading` | A tilted can as a square on its corner with vertices (12, 5), (22, 15), (12, 25) and (2, 15); a drip as a drop from (28, 18) to (28, 26). |
| `Share` | Three nodes as circles radius 3 at (7, 16), (24, 7) and (24, 25); a line from the left node to each right node. |
| `ShareBox` | An open box from (4, 16) to (28, 28) with radius-3 corners, its top edge stopping at x 10 and 22; an ArrowUp with shaft (16, 20)–(16, 4) rising through the gap, arrowhead scaled to 6. |
| `ShekelSign` | Two interlocking brackets: one from (5, 26) up to (5, 6) across to (17, 6) rounding down to (20, 9) and on to (20, 20); the other from (27, 6) down to (27, 26) across to (15, 26) rounding up to (12, 23) and on to (12, 12). |
| `Signal` | Four filled bars 4 wide with round ends, at x 3, 10, 17 and 24, rising from y 28 to y 20, 15, 10 and 4. |
| `SignIn` | A door frame: an open bracket from (17, 4) to (27, 4) down to (27, 28) to (17, 28); an ArrowRight from (4, 16) to (19, 16), arrowhead at (19, 16) scaled to 5. |
| `Sitemap` | A box from (12, 4) to (20, 10) over three boxes 6 wide from y 22 to 28 at x 3, 13 and 23, all with radius-1 corners; a stem from the top box down to a bar at y 15 from x 6 to 26, and drops from it into each lower box. |
| `Smudge` | `HandPointDown` turned an eighth of a turn so its finger points down and to the left, refitted to the live area. |
| `Snooze` | The `Bell` reduced to fit the left two-thirds of the live area; a small letter Z of strokes within (20, 4)–(28, 11). |
| `SoccerBall` | Fitted from the reference drawing: a ring of radius 13; a solid central pentagon with its top at (16, 11) and base from (13, 20) to (19, 20); a spoke from each vertex out to the ring; between neighboring spokes, the panel edges as a V of two lines meeting at the rim, so the ring reads as stitched panels. |
| `Sort` | An ArrowDown with shaft (11, 5)–(11, 27) and an ArrowUp with shaft (21, 27)–(21, 5), arrowheads scaled to 5. |
| `SortAlphaAscending` | An A with legs from (4, 13) up to (8, 4) and down to (12, 13) and a bar at y 10, over a Z from (4, 19) across to (12, 19), down to (4, 28) and across to (12, 28); an ArrowDown at x 24 from y 4 to 28 with a head 4 wide. |
| `SortAlphaDescending` | The Z of `SortAlphaAscending` on top at y 4–13 and its A below at y 19–28; an ArrowUp at x 24 from y 28 to 4 with a head 4 wide. |
| `SortNumericAscending` | A 0 as a stadium from x 4 to 12 and y 4 to 13 with radius-4 ends, over a 9 whose bowl is a full circle radius 3 at (8, 22) and whose tail drops from the bowl's right at (11, 22) to (11, 25) and rounds through radius 3 to (8, 28); an ArrowDown at x 24 from y 4 to 28 with a head 4 wide. |
| `SortNumericDescending` | The 9 of `SortNumericAscending` on top with its bowl at (8, 7) and tail rounding to (8, 13), over its 0 at y 19–28; an ArrowUp at x 24 from y 28 to 4 with a head 4 wide. |
| `SoundOutput` | The `Unmute` speaker with two sound waves: arcs from (21, 11) to (21, 21) of radius 6 and from (24, 7) to (24, 25) of radius 12. |
| `Speaker` | A cabinet as a rounded rect from (7, 3) to (25, 29); a small driver circle radius 2 at (16, 9); a large driver circle radius 5 at (16, 20). |
| `SpellCheck` | The letters ab: an a as a cup from (4, 10) to (10, 18) with a bar at y 14, a b with its stem from (14, 4) to (14, 18) and a bowl of radius 4 on its lower half; a tick from (13, 23) down to (18, 28) and up to (28, 18). |
| `Split` | Frame; a vertical divider at x 16; two small Arrowheads pointing away from the divider, tips at (10, 16) and (22, 16), scaled to 3. |
| `Stamp` | A handle as a loop from (13, 15) up to a radius-3 top at y 7 and down to (19, 15); a base from (4, 22) to (28, 22) rising to y 15 with radius-2 shoulders; a ground line from (8, 27) to (24, 27). |
| `Star` | A five-pointed star: top point (16, 3), outer points (29, 12), (25, 27), (7, 27) and (3, 12), inner points (20, 12), (22, 18), (16, 21), (10, 18) and (12, 12). |
| `Stopwatch` | A case as a circle radius 11 at (16, 18); four ticks 1 long at the quarters, 7 from the center; a hand from (16, 14) down to the center and on to (20, 21); a crown as a stem from the case up to (16, 3) under a cap from (12, 3) to (20, 3); a start lever from (24, 10) out to (27, 7). |
| `StopwatchDigital` | The case, crown and lever of `Stopwatch`, the face showing a 5: a bar from (20, 12) to (14, 12), down to (14, 18), across to (17, 18), then round a bowl of radius 3 ending at (13, 22). |
| `Strikethrough` | A letter S of strokes from (23, 9) down to (9, 22); a line from (4, 16) to (28, 16) through it. |
| `Subscript` | A letter x as two crossed lines from (4, 9) to (16, 21); a small 2 of strokes within (20, 17)–(28, 28). |
| `Superscript` | A letter x as two crossed lines from (4, 12) to (16, 24); a small 2 of strokes within (20, 5)–(28, 16). |
| `Swap` | An ArrowRight at y 11 from x 5 to 27 and an ArrowLeft at y 21 from x 27 to 5, arrowheads scaled to 5. |
| `Sync` | Two arcs of radius 12 about the center: one from (4, 16) over the top to (25, 9), one from (28, 16) under the bottom to (7, 23); at the end of each an arrowhead as a corner bracket of two 5-unit arms. |
| `Table` | Frame; a horizontal divider at y 13 and vertical dividers at x 12 and 20 below it. |
| `Tablet` | A body from (5, 3) to (27, 29) with radius-3 corners; a home button as a filled dot radius 1 at (16, 25). |
| `TaskList` | Two rows: a box 10 by 10 with radius 3 at x 4 to 14, at y 4 and 18, the first ticked and the second empty; a line from x 18 to 28 beside each at y 9 and 23. |
| `Terminal` | Frame; a chevron pointing right with tip at (13, 16) scaled to 4; a line from (17, 21) to (24, 21). |
| `Text` | Frame; four text lines inside at y 11, 15, 19, 23 from x 9 to 23, the last ending at x 17. |
| `TextDirectionLtr` | Three lines at y 8, 16, 24 from x 5 to 20; an ArrowRight from (21, 16) to (28, 16) with arrowhead scaled to 4. |
| `TextDirectionRtl` | Mirror of `TextDirectionLtr`: lines from x 12 to 27, arrow pointing left from (11, 16) to (4, 16). |
| `TextGlow` | The A of `FontSizeIncrease` with a four-pointed star centered at (26, 7) reaching 4 to each point. |
| `Toolbox` | A box as a rounded rect from (4, 11) to (28, 27); a handle from (11, 11) up to (11, 7) across to (21, 7) down to (21, 11); a latch line across the box at y 18 from x 4 to 28 broken by a clasp from (14, 16) to (18, 16) down to (18, 20) to (14, 20). |
| `Transform` | Four solid handles 4 square at the corners of the live area; a frame between them as lines from x 11 to 21 at y 6 and 26 and from y 11 to 21 at x 6 and 26; inside, an arrow from (12, 20) up to (20, 12) with an arrowhead scaled to 5 pointing up-right. |
| `Translation` | The Han character 文 in the upper left — a stem from (9, 3) down to a bar from (3, 7) to (15, 7), over two strokes that start close under it at (12, 10) and (6, 10) and sweep apart to (3, 20) and (16, 20), crossing high — and a Latin A in the lower right, legs from (18, 29) up to (24, 14) and down to (30, 29) with a bar at y 24 from x 20 to 28. |
| `TriangleDown` | A Triangle pointing down: base from (5, 10) to (27, 10), apex (16, 24). |
| `TriangleLeft` | `TriangleRight` mirrored: (24, 5)–(24, 27)–(5, 16). |
| `TriangleRight` | A Triangle pointing right: (8, 5)–(8, 27)–(27, 16). |
| `TriangleUp` | A Triangle pointing up: base from (5, 22) to (27, 22), apex (16, 8). |
| `Truck` | A cargo box from (3, 8) to (18, 22) with radius-2 top corners, its floor running only from (18, 22) to (14, 22); a cab roof from (18, 12) to (22, 12) sloping down to (28, 18); two wheels as circles radius 3 at (8, 24) and (24, 24), the body's edges stopping clear of them. |
| `Underline` | A U from strokes: (9, 5) down to y 17, curving through (16, 24) to (23, 17) up to y 5; an underline from (7, 28) to (25, 28). |
| `Undo` | A curve from (27, 23) rising and sweeping left to (5, 13); an arrowhead scaled to 6 at (5, 13) pointing left. |
| `Unmute` | A speaker: a box from (4, 12) to (10, 20) with a cone out to x 17 spanning y 6 to 26; one sound wave, an arc of radius 10 from (22, 8) to (22, 24). |
| `Unpin` | The `Pin` under a Slash, its outline broken 2 units clear of the slash on either side: the right side stops at (20, 7) and resumes at (22, 15), the base runs from (17, 20), and the needle from (16, 22). |
| `Unread` | The Envelope, closed. |
| `Upload` | Mirror of `Download` vertically: the arrow points up with shaft (16, 20)–(16, 5); the bracket Tray beneath it. |
| `User` | Person. |
| `Users` | A Person shifted left to center x 12; behind at `opacity 0.4` a second head radius 4 at (23, 11) and a partial shoulder arc from (21, 20) to (28, 28). |
| `Video` | Frame reduced to (4, 8)–(21, 24); a lens flap: (21, 13) to (28, 9) down to (28, 23) back to (21, 19). |
| `Wallet` | A body from (3, 10) to (29, 27) with radius-3 corners under a fold from (6, 10) up to y 7, across at y 5 with radius-2 corners to x 26 and down to the body; a pocket entering from the right edge at y 15, rounding radius 3 to leave at y 21; a clasp as a filled dot radius 1 at (24, 18). |
| `Warning` | A triangle with corners of radius 2: apex (16, 4), base along y 27 from x 3 to 29; a bar from (16, 11) to (16, 19) and a solid dot radius 1 at (16, 23). |
| `Warp` | A spiral from the center: semicircles of radius 2, 5, 8 and 11 alternating above and below y 16, ending at (4, 16). |
| `Wifi` | A filled dot radius 2 at (16, 25) under two arcs about it of radius 7 and 14, each spanning 45° either side of vertical. |
| `WindowMaximize` | Four corner brackets 6 by 6 at the corners of the live area, with a full Frame between them at opacity 0.4. |
| `WindowMinimize` | An ArrowDown with shaft (16, 4)–(16, 20), arrowhead at (16, 20); a line from (6, 26) to (26, 26). |
| `Wrench` | An open-end wrench as one outline: a ring head radius 8 at (22, 10) with a parallel-sided jaw 4 wide cut from its center toward the upper right, open between (25, 4) and (29, 8); a handle 6 wide from the ring down the diagonal to a rounded end at (7, 25). |
| `ZodiacAquarius` | Two zigzags of six strokes from x 4 to 28, the upper between y 9 and 13, the lower between y 19 and 23. |
| `ZodiacAries` | Two horns as arcs of radius 8 from (4, 16) up to (16, 8) and down to (28, 16); a stem from (16, 8) to (16, 28). |
| `ZodiacCancer` | Two filled-looking rings radius 3 at (9, 14) and (23, 18), each trailing an arc of radius 10: from (12, 14) up and over to (27, 9), and from (20, 18) down and under to (5, 23). |
| `ZodiacCapricorn` | A peak from (4, 18) up to (9, 8) and down to (15, 18), curving on through (24, 14) down to (24, 22) and round a radius-4 loop to (16, 22). |
| `ZodiacGemini` | Two pillars at x 11 and 21 from y 6 to 26 between bars from x 6 to 26 at y 6 and 26. |
| `ZodiacLeo` | A ring radius 4 at (8, 20); from its right a curve of radius 8 rising over to (24, 12), down to (24, 23) and hooking out to (28, 25). |
| `ZodiacLibra` | A bar from (4, 27) to (28, 27) under an arch: from (4, 20) to (9, 20), up to (9, 16), over radius 7 to (23, 16), down to (23, 20) and on to (28, 20). |
| `ZodiacPisces` | Two arcs of radius 12 bowed toward each other, from (8, 6) to (8, 26) and from (24, 6) to (24, 26), with a bar from (4, 16) to (28, 16) through them. |
| `ZodiacSagittarius` | An arrow from (6, 26) to (26, 6) with a corner-bracket head from (16, 6) to (26, 6) to (26, 16), crossed near its tail by a bar from (9, 17) to (15, 23). |
| `ZodiacScorpio` | The arches of `Virgo` whose last leg turns through radius 3 at (22, 23) into an arrow along y 26 to (28, 26), with a head from (25, 23) to (28, 26) to (25, 29). |
| `ZodiacTaurus` | A circle radius 7 at (16, 20) under horns as arcs of radius 10 from (6, 5) down to (16, 13) and up to (26, 5). |
| `ZodiacVirgo` | Three arches 6 wide with radius-3 tops from x 4, 10 and 16 at y 11, their legs down to y 26, the last leg curving out through (22, 27) to (28, 25). |
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

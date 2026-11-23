# Forma Charts Design

## Scope

`FormaLineChart` and `FormaBarChart`, in `Forma.Blazor.Charts`, and the types a host builds their data from.

## Purpose

Figures over time on an administrator's page: page load per hour, readers online, failures per day, storage growth.
A chart answers "how did this move over the last hour, day, 30 days or 13 months", and its table answers the same
question for a reader who can't see it.

## Decision summary

- **Its own assembly.** The charts ship as `Forma.Blazor.Charts`, a product assembly built on `Forma.Blazor` that
  borrows its namespace, `Forma.Blazor.Components.Charts`, as the data view does. A host that draws no chart references
  nothing of it. The charts need no services, so the assembly has no registrar.
- **A time axis of fixed ranges, or a span.** A chart covers a `ChartRange`: an hour of one-minute periods, a day of
  one-hour periods, thirty days of one-day periods, or thirteen months of one-month periods, ending with the period
  holding `End`, now by the host's `TimeProvider` where `End` is null. Or it covers a span from `Start` to `End`, its
  first and last points' times where either is null, such as an account's whole life, each point standing at its own
  time rather than in a period. A bar chart draws periods alone, since a bar stands for a period.
- **A span labels round steps of the calendar.** Its axis labels the finest step of the zone's calendar, from minutes to
  years, that lays four labels across it, and finer steps nested in it, months within years or hours within days, as
  the chart widens, each label written in the unit of the coarsest step it falls on.
- **The host's clock decides the periods.** Periods start on the minute, hour, day or month of the time zone the host's
  `TimeProvider` names as local, as the explorer's date groups do, so a day is the reader's day and a period crossing a
  change of the zone's offset is as long as that day really is. Labels are written in the culture the chart renders in.
- **Series are data.** `Series` holds one to four `ChartSeries`, each a name and its `ChartPoint`s, a time and a value.
  A point stands in the period that holds its time, the last one listed where several fall in one period, and a point
  outside the range is not drawn.
- **A gap is a gap.** A period with no point, or with a point whose value is null, draws nothing: a line breaks
  there, a bar is missing, a band stops, and the table and readout say there is no data. A gap is never a zero.
- **One axis.** Every series shares one value scale, from zero or the lowest value beneath it to the highest value or
  the threshold above it, rounded out to three or four even steps. A line chart whose values stand far from zero, such
  as an account's balance, leaves zero out with `IncludeZero`, fitting the scale to its values; a bar grows from zero,
  so a bar chart's scale always reaches it. `ValueFormat`, a .NET numeric format string, writes the axis, the readout
  and the table in the culture.
- **A threshold and a band.** Either chart takes a `ChartThreshold`, a value and its label, drawn as a labelled line
  across the plot. The line chart also takes a `ChartBand` between two of its series, such as a median and a 95th
  percentile, filled in the upper series' hue at a wash, and broken where either series has a gap.
- **A point can record an event.** A `ChartPoint` carries a `ChartMark`, a named kind of event such as a deposit or a
  fee, and a `Note`. A line chart draws a marked point in its mark's shape, a ring, a hollow square, a plus or a
  cross, larger and apart from the series' filled markers, and keys up to four marks in the legend; the readout and
  the table give a point's mark and note beside its value. A series `IsFilled` washes the area beneath its line down
  to the baseline.
- **Identity never rests on color.** A series' color is its place in `Series`, so a host keeps a series in its place
  to keep its color. A line also carries its own dash, solid, long dash, dot and dash-dot in that order, and its own
  marker, circle, square, triangle and diamond, drawn at a point standing alone between gaps, at the line's last
  point and at the period being read. Bars stand in each period in the legend's order. Two or more series carry a
  legend whose keys draw the dash, the marker or the bar, and forced colors and print add a texture to each bar.
- **Reading the values.** Hovering a period, or moving through the periods from the keyboard, shows its readout: the
  period in full, then each series' value before its name, keyed by its mark, or no data. The keyboard reaches the
  periods through one native range control, so the arrows, Home and End move between periods in the page's direction
  and a screen reader hears each readout as the control's value. The figure's table holds every period's values and
  is what assistive technology reads; the drawing is hidden from it. `ShowTable` puts the table behind a toggle
  beneath the chart, for every reader to open.
- **Fits the width it's given.** The plot is laid out in fractions of its width, so a chart fits a phone's column,
  a dialog or a dashboard's tile without measuring it, and renders the same statically. The value axis labels its
  steps inside the plot, above each gridline, so no column is spent on it. The time axis labels the periods it can
  fit, and more as its container widens, by container queries whose widths come from the labels they show.
- **Direction.** The time axis runs from the inline start, so in a right-to-left culture time runs right to left.
  The text is laid out by logical properties, and the drawing of lines and bands is mirrored as a whole.
- **Drawn from tokens.** Each series' color is a token, `--chart-series-1-color` to `--chart-series-4-color`,
  validated as a set against the paper in light and against a dark surface for the dark values a theme sets; the
  grid, the baseline and the threshold take tokens beside them, and every text takes the text tokens, never a
  series' color.
- **Static rendering.** A statically rendered chart draws the same figure, its hover readouts and its table; only the
  keyboard's range control, which needs a renderer to move the readout, is left out.

## The parts of a chart

| Part | What it is |
| --- | --- |
| Caption | The chart's `Title`, which names the figure and its table |
| Legend | One key per series, for two or more |
| Plot | The gridlines with their value labels, the threshold, the band and the fills, the marks, and one column per period or moment |
| Readout | The period's values, shown over its column on hover or as the keyboard reaches it |
| Time axis | The period labels beneath the plot |
| Table | A row per period or moment and a column per series, visually hidden or behind a toggle |

## Marks

| Mark | Spec |
| --- | --- |
| Line | 2px, round joins and caps, its series' dash, unscaled as the plot stretches |
| Marker | 8px, its series' shape, ringed in the surface color |
| Mark | 12px, its mark's shape in its series' color, a ring and a hollow square open on the surface |
| Fill | The series' hue at a lighter wash than the band's, down to the baseline |
| Bar | At most 24px wide, a 4px rounded data end, square at the baseline, 2px apart from its neighbor |
| Band | The upper series' hue at a wash |
| Threshold | A 1px dashed rule in the secondary ink, its label at the inline end |
| Gridline | A 1px solid hairline one step off the surface |

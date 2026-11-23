# Forma Charts Implementation Plan

The steps that build [the charts design](charts-design.md), in order, each landing with its tests and its gate.

## Steps

1. ✅ **The assembly and its data.** `Forma.Blazor.Charts` with its README, the public types a host builds a chart's
   data from, and the internal layout that turns a range, an end and the host's time zone into periods, places each
   point in its period, and scales the values. The layout is internal, so its tests read what a chart's table holds;
   they cover the periods in each range, across a change of a zone's offset and in a zone whose offset isn't whole
   hours, the placing of points, and the scale's steps.
2. ✅ **The line chart.** `FormaLineChart`: the caption, the legend, the plot with its gridlines, threshold, band, lines
   and markers, each period's column and readout, the keyboard's range control, the time axis and the table, with its
   stylesheet, its locale sections in each language pack, bUnit tests and a Showcase page.
3. ✅ **The bar chart.** `FormaBarChart` on the same parts, its bars grouped in each period.
4. ✅ **Every runtime.** A check page for each chart in the check hosts, browser checks of the readout from the pointer
   and from the keyboard, the direction and a phone's width in every interactive runtime, static rendering and the
   inventory; the tokens in the token stylesheet and the theme contract; the catalog, the roadmap and the changelog.

## Open

- **Review.** Both charts are built and in review, as the catalog marks them; they move to implemented once the
  maintainer approves them, and this plan retires then.

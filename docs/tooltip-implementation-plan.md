# Forma Tooltip Implementation Plan

The steps that build [the tooltip design](tooltip-design.md), in order, each landing with its tests and its gate.

## Steps

1. ✅ **The tooltip.** `FormaTooltip` with its behavior script on the popover's anchored placement, its stylesheet and
   tokens, bUnit and script tests, a check page in every render mode and a Showcase page.
   - The script is one document-wide `Tooltip.js` the browser interop's initializer installs on every page, in a Web
     App and in a web view, so a component calls into no script; the popover's anchored placement gained `side` and a
     centered `align` for it.
   - The initializer loads it on demand, so a host that can't serve a module it reaches still starts Blazor.
2. ✅ **The contract.** `IDisableable` and the rendering of a reason in the bases that can carry it, the input base among
   them, and in `FormaButton`, with what composes it.
   - A `DisabledReasons` helper holds what every implementer draws: whether it shows its reason, its tooltip's id and
     its description with that id added. The input base offers `ShowsDisabledReason` and the tooltip itself.
   - A button showing its reason drops a host's `title`, so one tooltip speaks.
3. **Every component that can be disabled.** Each component taking `Disabled`, and each record carrying `IsDisabled`,
   brought to the contract: the toolbar and its items, the menu items, the tabs, the check box, the toggle switch, the
   slider, the selects, the combo box, the pickers, the composer's bars and the rest, each with its tests.
   - ✅ The inputs: the text box, the number and currency inputs, the combo box, the select and multi-select, the date
     box, date, date range, date-time and time pickers and the time box, the check box, the toggle switch and the slider.
   - ✅ The menus and bars: the drop-down menu, which takes `Disabled` itself now, the overflow menu, the menu items, the
     toolbar and its items, the status strip, and the sortable list's handles.
   - ✅ The composer, its bars, its surface and its presets; its context carries the reason to its editors.
   - ✅ The records: a select's, multi-select's and combo box's options, and a share menu's actions.
   - Open: the frame, the chrome and the window, whose `Disabled` makes a whole region inert, with no control of its own
     to stay reachable; and the explorer's catalog items, which the data view draws as rows.
4. ✅ **Every runtime.** Browser checks of a disabled control's reason from the pointer, the keyboard and a tap, in both
   directions, in every render mode; the catalog, the changelog and the features status.
   - The pointer, the keyboard and a tap on a button, a text box, a select, a check box and a slider, in every runtime
     and on a static page, and a tooltip hanging centered on its control in a right-to-left region.

## Open

- Step 3's containers and catalog items, which wait on a decision of where their reason shows.

# Forma Tooltip Implementation Plan

The steps that build [the tooltip design](tooltip-design.md), in order, each landing with its tests and its gate.

## Steps

1. **The tooltip.** `FormaTooltip` with its behavior script on the popover's anchored placement, its stylesheet and
   tokens, bUnit and script tests, a check page in every render mode and a Showcase page.
2. **The contract.** `IDisableable` and the rendering of a reason in the bases that can carry it, the input base among
   them, and in `FormaButton`, with what composes it.
3. **Every component that can be disabled.** Each component taking `Disabled`, and each record carrying `IsDisabled`,
   brought to the contract: the toolbar and its items, the menu items, the tabs, the check box, the toggle switch, the
   slider, the selects, the combo box, the pickers, the composer's bars and the rest, each with its tests.
4. **Every runtime.** Browser checks of a disabled control's reason from the pointer, the keyboard and a tap, in both
   directions, in every render mode; the catalog, the changelog and the features status.

## Open

- Every step.

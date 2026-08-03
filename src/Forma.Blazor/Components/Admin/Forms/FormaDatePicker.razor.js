/**
 * FormaDatePicker — popup placement
 *
 * The calendar is a popover, so it renders in the top layer and no scrolling or
 * clipping ancestor can cut it off. Nothing in the top layer keeps a layout
 * relationship to what opened it, so its position is measured and applied here.
 */

/** Distance between the trigger and the calendar, matching the CSS gap it replaces. */
const GAP = 4;

/**
 * @param {HTMLElement} popup
 * @param {HTMLElement} anchor
 */
export function open(popup, anchor) {
    close(anchor);

    if (popup.showPopover && !popup.matches(':popover-open')) {
        popup.showPopover();
    }

    // Scroll is captured because the trigger may sit in any scrolling ancestor, and
    // those scroll events do not bubble.
    const controller = new AbortController();
    const place = () => position(popup, anchor);

    addEventListener('scroll', place, { capture: true, passive: true, signal: controller.signal });
    addEventListener('resize', place, { passive: true, signal: controller.signal });

    anchor.formaDatePickerPlacement = controller;
    place();
}

/**
 * @param {HTMLElement} anchor
 */
export function close(anchor) {
    if (!anchor || !anchor.formaDatePickerPlacement) {
        return;
    }

    anchor.formaDatePickerPlacement.abort();
    delete anchor.formaDatePickerPlacement;
}

/**
 * @param {HTMLElement} popup
 * @param {HTMLElement} anchor
 */
function position(popup, anchor) {
    const trigger = anchor.getBoundingClientRect();
    const width = popup.offsetWidth;
    const height = popup.offsetHeight;

    // Mirrored, not different: the calendar hangs from the trigger's reading-start
    // edge in both directions, so RTL is the mirror image rather than the same side.
    const isRightToLeft = getComputedStyle(anchor).direction === 'rtl';
    const preferredInlineStart = isRightToLeft ? trigger.right - width : trigger.left;
    const inlineStart = Math.max(GAP, Math.min(preferredInlineStart, innerWidth - width - GAP));

    // Below unless that would run past the viewport and there is room above.
    const below = trigger.bottom + GAP;
    const above = trigger.top - height - GAP;
    const blockStart = below + height <= innerHeight || above < GAP ? below : above;

    popup.style.left = `${Math.round(inlineStart)}px`;
    popup.style.top = `${Math.round(blockStart)}px`;
}

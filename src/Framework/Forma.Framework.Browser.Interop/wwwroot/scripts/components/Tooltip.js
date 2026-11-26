/**
 * Tooltip — showing a tooltip beside the control that names it in aria-describedby
 *
 * One set of listeners on the document serves every tooltip on the page, drawn statically or interactively. A pointer
 * resting on a control shows its tooltip after a short delay, the keyboard's focus at once, and a touch's tap on it at
 * once. The tooltip stays while the pointer is over the control or the tooltip, or the control holds the keyboard's focus;
 * Escape and a press elsewhere hide it, through the input router, which takes Escape for the tooltip ahead of a dialog
 * holding it. It hangs from the control in the top layer, above it where there is room. A press on a control disabled
 * with the reason its tooltip gives does nothing, on a page no renderer handles too.
 */

import { attach as attachAnchoredPopover } from '../behaviors/AnchoredPopover.js';
import { register } from '../../../input/scripts/InputRouter.js';

/** The tooltip element a control names in its description. */
const TooltipSelector = '.forma-tooltip[role="tooltip"]';

/** How long, in milliseconds, a pointer rests on a control before its tooltip shows. */
const HoverDelay = 500;

/** The keys that change a range's value. */
const RangeKeys = new Set(['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'PageUp', 'PageDown', 'Home', 'End']);

let isInstalled = false;

/** The tooltip showing, the control it describes, and what places it and takes Escape for it. */
let shown = null;

/** The control a resting pointer will show the tooltip of once the delay passes, and the delay's timer. */
let pending = null;

/** Starts showing tooltips on the page, once. */
export function install() {
    if (isInstalled) {
        return;
    }

    isInstalled = true;
    document.addEventListener('pointerover', handlePointerOver);
    document.addEventListener('pointerout', handlePointerOut);
    document.addEventListener('pointerdown', handlePointerDown, { capture: true });
    document.addEventListener('focusin', handleFocusIn);
    document.addEventListener('focusout', handleFocusOut);
    document.addEventListener('click', handleClick, { capture: true });
    document.addEventListener('keydown', handleKeyDown, { capture: true });

    // A control the keyboard reached before the script arrived, as on a page still loading, shows its tooltip as it arrives.
    const focused = document.activeElement;
    const found = findTooltip(focused);

    if (found !== null && found.control === focused && focused.matches(':focus-visible')) {
        show(found);
    }
}

/**
 * The control an element stands in that names a tooltip in its description, and the tooltip, or null where it names none.
 * @param {EventTarget | null} target
 * @returns {{ control: Element, tooltip: HTMLElement } | null}
 */
export function findTooltip(target) {
    const control = target instanceof Element ? target.closest('[aria-describedby]') : null;

    if (control === null) {
        return null;
    }

    for (const id of control.getAttribute('aria-describedby').split(/\s+/)) {
        const tooltip = id ? control.ownerDocument.getElementById(id) : null;

        if (tooltip?.matches(TooltipSelector)) {
            return { control, tooltip };
        }
    }

    return null;
}

function handlePointerOver(event) {
    if (event.pointerType === 'touch' || isOverShown(event.target)) {
        return;
    }

    const found = findTooltip(event.target);

    if (found === null || pending?.control === found.control) {
        return;
    }

    cancelPending();
    pending = { control: found.control, timer: setTimeout(() => show(found), HoverDelay) };
}

function handlePointerOut(event) {
    if (event.pointerType === 'touch') {
        return;
    }

    // Moving between the control and its tooltip keeps it, as WCAG 1.4.13 asks of content shown on hover.
    if (pending !== null && !pending.control.contains(event.relatedTarget)) {
        cancelPending();
    }

    if (shown !== null && !isOverShown(event.relatedTarget) && !shown.control.contains(document.activeElement)) {
        hide();
    }
}

// A tap shows the tapped control's tooltip; a tap elsewhere is a press away, which the router hides it for. A press on a range
// disabled with a reason moves nothing.
function handlePointerDown(event) {
    if (findDisabledRange(event.target) !== null) {
        event.preventDefault();
    }

    if (event.pointerType !== 'touch') {
        return;
    }

    const found = findTooltip(event.target);

    if (found !== null && shown?.control !== found.control) {
        show(found);
    }
}

// A control disabled with a reason stays reachable, so a press on it, a key's among them, does nothing where no renderer
// handles it, as on a statically rendered page: its form isn't submitted, and its link isn't followed.
function handleClick(event) {
    const found = findTooltip(event.target);

    if (found !== null && found.control.getAttribute('aria-disabled') === 'true') {
        event.preventDefault();
    }
}

// A range disabled with a reason stays reachable, so the keys that would change its value change nothing, Tab still moving on.
function handleKeyDown(event) {
    if (RangeKeys.has(event.key) && findDisabledRange(event.target) !== null) {
        event.preventDefault();
    }
}

/** The range an element stands in that is disabled with the reason its tooltip gives, or null. */
function findDisabledRange(target) {
    const found = findTooltip(target);

    return found !== null && found.control.matches('input[type="range"][aria-disabled="true"]') ? found.control : null;
}

function handleFocusIn(event) {
    const found = findTooltip(event.target);

    if (found !== null && found.control === event.target && event.target.matches(':focus-visible')) {
        cancelPending();
        show(found);
    }
}

function handleFocusOut(event) {
    if (shown !== null && event.target === shown.control && !shown.control.matches(':hover') && !shown.tooltip.matches(':hover')) {
        hide();
    }
}

function isOverShown(target) {
    return shown !== null && target instanceof Node && (shown.control.contains(target) || shown.tooltip.contains(target));
}

function cancelPending() {
    if (pending !== null) {
        clearTimeout(pending.timer);
        pending = null;
    }
}

function show({ control, tooltip }) {
    pending = null;

    if (shown?.tooltip === tooltip || !control.isConnected) {
        return;
    }

    hide();

    const placement = attachAnchoredPopover(tooltip, { anchor: control, side: 'above', align: 'center' });
    const registration = register(tooltip, { escape: 'dismiss', pressAway: 'pass-through', within: [control], onDismiss: () => hide() });

    // A render taking the control or the tooltip away, as a reason cleared does, takes the tooltip and its hold on Escape with it.
    const removal = new MutationObserver(() => {
        if (!control.isConnected || !tooltip.isConnected) {
            hide();
        }
    });
    removal.observe(control.ownerDocument.documentElement, { childList: true, subtree: true });

    shown = { control, tooltip, placement, registration, removal };
    placement.open();
    registration?.activate();
}

/** Hides the tooltip showing, if one is. */
export function hide() {
    cancelPending();

    if (shown === null) {
        return;
    }

    const { placement, registration, removal } = shown;
    shown = null;
    removal.disconnect();
    registration?.dispose();
    placement.dispose();
}

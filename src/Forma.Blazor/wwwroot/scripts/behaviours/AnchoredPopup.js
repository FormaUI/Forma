/**
 * AnchoredPopup — keeping a popup positioned against the element that opened it
 *
 * A popup in the top layer (via the popover attribute) is immune to every clipping and
 * scrolling ancestor, but the top layer keeps no layout relationship to whatever opened
 * it. The handle measures the anchor, applies the popup's position, and keeps the two
 * together for as long as the popup is open.
 */

import { Focus } from './Focus.js';

/** Distance between the anchor and the popup. */
const GAP = 4;

/**
 * @param {HTMLElement} target The popup.
 * @param {{
 *   anchor: HTMLElement,
 *   keepsFocus?: boolean,
 *   owner?: { invokeMethodAsync: (name: string, ...args: unknown[]) => Promise<unknown> }
 * }} options
 *   The element the popup hangs from; whether Tab moves between the popup's own controls instead of
 *   leaving it; and the owner told through HandleEscapeAsync when Escape is pressed while the popup is open.
 */
export function attach(target, options) {
    return new AnchoredPopup(target, options);
}

class AnchoredPopup {
    #popup;
    #anchor;
    #keepsFocus;
    #owner;
    #trigger = null;
    #placement = null;
    #resizeObserver = null;

    constructor(popup, { anchor, keepsFocus = false, owner = null }) {
        this.#popup = popup;
        this.#anchor = anchor;
        this.#keepsFocus = keepsFocus;
        this.#owner = owner;
    }

    /** Shows the popup and keeps it positioned against its anchor until closed; focus returns to the trigger on a close that asks for it. */
    open(trigger = null) {
        this.close();
        this.#trigger = trigger;

        if (this.#popup.showPopover && !this.#popup.matches(':popover-open')) {
            this.#popup.showPopover();
        }

        this.#placement = new AbortController();
        const place = () => this.#position();

        // Captured because the anchor may sit in any scrolling ancestor, and scroll events
        // from those do not bubble.
        addEventListener('scroll', place, { capture: true, passive: true, signal: this.#placement.signal });
        addEventListener('resize', place, { passive: true, signal: this.#placement.signal });

        // The popup's own content can grow after it opens — a progressively loaded picker, a
        // search result count, a category switch — so its size alone must trigger repositioning.
        this.#resizeObserver = new ResizeObserver(place);
        this.#resizeObserver.observe(this.#popup);

        if (this.#keepsFocus) {
            new Focus(this.#popup, { wrap: true }, this.#placement.signal);
        }

        // Escape closes the popup wherever focus is — on the trigger, inside the popup, or elsewhere on the
        // page — and is consumed here so a dialog hosting the popup does not close on the same press.
        if (this.#owner) {
            document.addEventListener('keydown', event => {
                if (event.key === 'Escape' && !event.defaultPrevented) {
                    event.preventDefault();
                    this.#owner.invokeMethodAsync('HandleEscapeAsync');
                }
            }, { signal: this.#placement.signal });
        }

        place();
    }

    /** Hides the popup and releases the listeners held for it, first returning focus to the trigger when asked and focus is inside the popup. Safe to call when none are held. */
    close(returnsFocus = false) {
        if (returnsFocus && this.#trigger && this.#popup.contains(document.activeElement)) {
            this.#trigger.focus();
        }

        this.#placement?.abort();
        this.#placement = null;
        this.#resizeObserver?.disconnect();
        this.#resizeObserver = null;

        if (this.#popup.hidePopover && this.#popup.matches(':popover-open')) {
            this.#popup.hidePopover();
        }
    }

    dispose() {
        this.close();
    }

    #position() {
        const anchor = this.#anchor.getBoundingClientRect();
        const width = this.#popup.offsetWidth;
        const height = this.#popup.offsetHeight;

        // Mirrored, not different: the popup hangs from the anchor's reading-start edge in
        // both directions, so RTL is the mirror image rather than the same side.
        const isRightToLeft = getComputedStyle(this.#anchor).direction === 'rtl';
        const preferredInlineStart = isRightToLeft ? anchor.right - width : anchor.left;
        const inlineStart = Math.max(GAP, Math.min(preferredInlineStart, innerWidth - width - GAP));

        const below = anchor.bottom + GAP;
        const above = anchor.top - height - GAP;
        const blockStart = below + height <= innerHeight || above < GAP ? below : above;

        this.#popup.style.left = `${Math.round(inlineStart)}px`;
        this.#popup.style.top = `${Math.round(blockStart)}px`;
    }
}

/**
 * AnchoredPopup — keeping a popup positioned against the element that opened it
 *
 * A popup in the top layer (via the popover attribute) is immune to every clipping and
 * scrolling ancestor, but the top layer keeps no layout relationship to whatever opened
 * it. The handle measures the anchor, applies the popup's position, and keeps the two
 * together for as long as the popup is open.
 */

import { FocusCycle } from './FocusCycle.js';

/** Distance between the anchor and the popup. */
const GAP = 4;

/**
 * @param {HTMLElement} target The popup.
 * @param {{ anchor: HTMLElement, keepsFocus?: boolean }} options
 *   The element the popup hangs from; whether Tab moves between the popup's own controls instead of leaving it.
 */
export function attach(target, options) {
    return new AnchoredPopup(target, options);
}

class AnchoredPopup {
    #popup;
    #anchor;
    #keepsFocus;
    #placement = null;
    #resizeObserver = null;

    constructor(popup, { anchor, keepsFocus = false }) {
        this.#popup = popup;
        this.#anchor = anchor;
        this.#keepsFocus = keepsFocus;
    }

    /** Shows the popup and keeps it positioned against its anchor until closed. */
    open() {
        this.close();

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
            new FocusCycle(this.#popup, this.#placement.signal);
        }

        place();
    }

    /** Releases the listeners held for the popup. Safe to call when none are held. */
    close() {
        this.#placement?.abort();
        this.#placement = null;
        this.#resizeObserver?.disconnect();
        this.#resizeObserver = null;
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

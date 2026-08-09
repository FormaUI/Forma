/**
 * Anchored popup placement
 *
 * A popup in the top layer (via the popover attribute) is immune to every clipping and
 * scrolling ancestor, but the top layer keeps no layout relationship to whatever opened
 * it. An instance owns one trigger/popup pair: it measures the trigger, applies the
 * popup's position, and keeps the two together for as long as the popup is open.
 */
export class AnchoredPopup {
    /** Distance between the trigger and the popup. */
    static #GAP = 4;

    #popup;
    #anchor;
    #placement;

    /**
     * @param {HTMLElement} popup
     * @param {HTMLElement} anchor
     */
    constructor(popup, anchor) {
        this.#popup = popup;
        this.#anchor = anchor;
    }

    /**
     * Builds a popup bound to one trigger. Returned plain: the caller requests an
     * IJSObjectReference, so .NET wraps it — wrapping it here too would hand back a
     * reference to the wrapper rather than to this instance.
     * @param {HTMLElement} popup
     * @param {HTMLElement} anchor
     */
    static create(popup, anchor) {
        return new AnchoredPopup(popup, anchor);
    }

    /** Shows the popup and keeps it positioned against its trigger until closed. */
    open() {
        this.close();

        if (this.#popup.showPopover && !this.#popup.matches(':popover-open')) {
            this.#popup.showPopover();
        }

        this.#placement = new AbortController();
        const place = () => this.#position();

        // Captured because the trigger may sit in any scrolling ancestor, and scroll
        // events from those do not bubble.
        addEventListener('scroll', place, { capture: true, passive: true, signal: this.#placement.signal });
        addEventListener('resize', place, { passive: true, signal: this.#placement.signal });

        place();
    }

    /** Releases the listeners held for the popup. Safe to call when none are held. */
    close() {
        this.#placement?.abort();
        this.#placement = undefined;
    }

    #position() {
        const trigger = this.#anchor.getBoundingClientRect();
        const width = this.#popup.offsetWidth;
        const height = this.#popup.offsetHeight;

        // Mirrored, not different: the popup hangs from the trigger's reading-start edge
        // in both directions, so RTL is the mirror image rather than the same side.
        const isRightToLeft = getComputedStyle(this.#anchor).direction === 'rtl';
        const preferredInlineStart = isRightToLeft ? trigger.right - width : trigger.left;
        const gap = AnchoredPopup.#GAP;
        const inlineStart = Math.max(gap, Math.min(preferredInlineStart, innerWidth - width - gap));

        const below = trigger.bottom + gap;
        const above = trigger.top - height - gap;
        const blockStart = below + height <= innerHeight || above < gap ? below : above;

        this.#popup.style.left = `${Math.round(inlineStart)}px`;
        this.#popup.style.top = `${Math.round(blockStart)}px`;
    }
}

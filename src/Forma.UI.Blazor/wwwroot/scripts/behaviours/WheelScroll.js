/**
 * WheelScroll — scrolling a sideways track with a wheel that only reports vertically
 *
 * A mouse wheel reports along the block axis alone, so its turn anywhere over the bar is
 * read as movement along the track's inline axis. Offsets are handled in both directions
 * because a right-to-left track counts scrollLeft downwards from zero.
 */

/** Pixels a line is taken to be, for the wheels that report in lines rather than pixels. */
const LineHeight = 16;

/**
 * @param {HTMLElement} target The element the wheel is answered over.
 * @param {{ track?: HTMLElement }} options The element that moves, when it is not the target itself.
 */
export function attach(target, { track = target } = {}) {
    return new WheelScroll(target, track);
}

class WheelScroll {
    #target;
    #track;
    #controller = new AbortController();

    constructor(target, track) {
        this.#target = target;
        this.#track = track;

        // A passive listener cannot call preventDefault, which keeps the turn from the page.
        target.addEventListener('wheel', event => this.#handle(event), { signal: this.#controller.signal, passive: false });
    }

    dispose() {
        this.#controller.abort();
    }

    #handle(event) {
        // A trackpad already reports sideways, and the browser scrolls the track from that without help.
        if (event.deltaX !== 0) {
            return;
        }

        // A popup or a scrolling panel opened from the bar is nearer to the pointer than the bar is.
        if (this.#nearerScroller(event.target)) {
            return;
        }

        const slack = this.#track.scrollWidth - this.#track.clientWidth;

        // A sub-pixel track width leaves a fractional remainder, so a pixel is the smallest real slack.
        if (slack <= 1) {
            return;
        }

        const isRightToLeft = getComputedStyle(this.#track).direction === 'rtl';
        const distance = event.deltaMode === WheelEvent.DOM_DELTA_LINE ? event.deltaY * LineHeight : event.deltaY;
        const offset = this.#track.scrollLeft;
        const wanted = offset + (isRightToLeft ? -distance : distance);
        const next = isRightToLeft ? Math.min(0, Math.max(-slack, wanted)) : Math.max(0, Math.min(slack, wanted));

        // The turn stays with the bar while the pointer is over it, at either end included, so reaching
        // the last command never throws the page into motion; a bar with nothing to scroll took no turn at all.
        event.preventDefault();

        if (Math.abs(next - offset) < 1) {
            return;
        }

        // Each notch lands at once; the track's smooth scrolling is for the arrows, and would lag a wheel.
        this.#track.scrollTo({ left: next, behavior: 'instant' });
    }

    /**
     * Whether the pointer is over something that only lives in the bar's markup: a popover, a
     * backdrop or anything else taken out of flow, or a panel that scrolls on its own.
     */
    #nearerScroller(from) {
        for (let node = from instanceof Element ? from : from?.parentElement; node && node !== this.#target; node = node.parentElement) {
            if (node.hasAttribute('popover')) {
                return true;
            }

            const { overflowY, position } = getComputedStyle(node);

            // A fixed element covers the page rather than a place in the row, so a turn over it is the page's.
            if (position === 'fixed') {
                return true;
            }

            if ((overflowY === 'auto' || overflowY === 'scroll') && node.scrollHeight > node.clientHeight) {
                return true;
            }
        }

        return false;
    }
}

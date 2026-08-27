/**
 * WheelScroll — scrolling a sideways track with a wheel that only reports vertically
 *
 * A mouse wheel reports along the block axis alone, so its turn is read here as movement
 * along the track's inline axis. Offsets are handled in both directions because a
 * right-to-left track counts scrollLeft downwards from zero.
 */

/** Pixels a line is taken to be, for the wheels that report in lines rather than pixels. */
const LineHeight = 16;

/** @param {HTMLElement} target */
export function attach(target) {
    return new WheelScroll(target);
}

class WheelScroll {
    #track;
    #controller = new AbortController();

    constructor(track) {
        this.#track = track;

        // A passive listener cannot call preventDefault, which keeps the turn from the page.
        track.addEventListener('wheel', event => this.#handle(event), { signal: this.#controller.signal, passive: false });
    }

    dispose() {
        this.#controller.abort();
    }

    #handle(event) {
        // A trackpad already reports sideways, and the browser scrolls the track from that without help.
        if (event.deltaX !== 0) {
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

        // At either end the track has nothing left to give, so the turn belongs to the page again.
        if (Math.abs(next - offset) < 1) {
            return;
        }

        event.preventDefault();
        this.#track.scrollLeft = next;
    }
}

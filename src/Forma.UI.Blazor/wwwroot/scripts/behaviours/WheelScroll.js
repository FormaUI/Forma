/**
 * WheelScroll — moving something sideways with a wheel that only reports vertically
 *
 * A mouse wheel reports along the block axis alone, so its turn anywhere over the target is
 * read as movement along the inline axis. Where the movement goes is the caller's: a track
 * scrolls by it, an owner is told its direction. Offsets are handled in both directions
 * because a right-to-left track counts scrollLeft downwards from zero.
 */

/** Pixels a line is taken to be, for the wheels that report in lines rather than pixels. */
const LineHeight = 16;

/** Milliseconds between events that still belong to one gesture, so a trackpad flick reports once. */
const GestureGap = 150;

/**
 * Pixels below which a delta is a trackpad's rather than a wheel notch's. A notch reports whole
 * lines, or in pixels no fewer than the 48 of three lines, while a trackpad reports the pointer's
 * own movement, a few pixels at a time.
 */
const NotchDistance = 40;

/**
 * @param {HTMLElement} target The element the wheel is answered over.
 * @param {{ track?: HTMLElement, owner?: { invokeMethodAsync: (name: string, ...args: unknown[]) => Promise<unknown> } }} options
 *   The element that scrolls by the movement, or the owner that is told its direction; the target scrolls when neither is given.
 */
export function attach(target, { track, owner } = {}) {
    return new WheelScroll(target, owner ? new DirectionSink(owner) : new TrackSink(track ?? target));
}

class WheelScroll {
    #target;
    #sink;
    #controller = new AbortController();

    constructor(target, sink) {
        this.#target = target;
        this.#sink = sink;

        // A passive listener cannot call preventDefault, which keeps the turn from the page.
        target.addEventListener('wheel', event => this.#handle(event), { signal: this.#controller.signal, passive: false });
    }

    dispose() {
        this.#controller.abort();
    }

    #handle(event) {
        // A popup or a scrolling panel opened from the target is nearer to the pointer than the target is.
        if (this.#nearerScroller(event.target)) {
            return;
        }

        this.#sink.move(event);
    }

    /**
     * Whether the pointer is over something that only lives in the target's markup: a popover, a
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

/** Scrolls a track by the movement until it reaches either end. */
class TrackSink {
    #track;

    constructor(track) {
        this.#track = track;
    }

    move(event) {
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

        // The turn stays with the target while the pointer is over it, at either end included, so reaching
        // the last item never throws the page into motion; a track with nothing to scroll took no turn at all.
        event.preventDefault();

        if (Math.abs(next - offset) < 1) {
            return;
        }

        // Each notch lands at once; the track's smooth scrolling is for the arrows, and would lag a wheel.
        this.#track.scrollTo({ left: next, behavior: 'instant' });
    }
}

/** Tells an owner which way the wheel moved: every notch of a wheel, and each gesture of a trackpad once. */
class DirectionSink {
    #owner;
    #lastAt = Number.NEGATIVE_INFINITY;
    #isGestureReported = false;

    constructor(owner) {
        this.#owner = owner;
    }

    move(event) {
        const delta = event.deltaX !== 0 ? event.deltaX : event.deltaY;

        if (delta === 0) {
            return;
        }

        // The turn is the owner's while the pointer is over the target, so the page under it stays still.
        event.preventDefault();

        const isNotch = event.deltaMode !== WheelEvent.DOM_DELTA_PIXEL || Math.abs(delta) >= NotchDistance;
        const isSameGesture = event.timeStamp - this.#lastAt < GestureGap;
        this.#lastAt = event.timeStamp;

        if (!isNotch && isSameGesture && this.#isGestureReported) {
            return;
        }

        this.#isGestureReported = !isNotch;
        this.#owner.invokeMethodAsync('HandleWheelAsync', Math.sign(delta));
    }
}

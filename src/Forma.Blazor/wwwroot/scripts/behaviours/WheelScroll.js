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

/** Milliseconds between events that still belong to one gesture. */
const GestureGap = 150;

/**
 * Pixels below which a delta is a trackpad's rather than a wheel notch's. A notch reports whole
 * lines, or in pixels no fewer than the 48 of three lines, while a trackpad reports the pointer's
 * own movement, a few pixels at a time.
 */
const NotchDistance = 40;

/**
 * @param {HTMLElement} target The element the wheel is answered over.
 * @param {{ track?: HTMLElement, owner?: { invokeMethodAsync: (name: string, ...args: unknown[]) => Promise<unknown> }, modifier?: string }} options
 *   The element that scrolls by the movement, or the owner that is told its direction; the target scrolls when neither is given.
 *   A modifier names the key the reader must hold for the wheel to be answered at all; without one every turn is.
 */
export function attach(target, { track, owner, modifier } = {}) {
    return new WheelScroll(target, owner ? new DirectionSink(owner) : new TrackSink(track ?? target), modifier);
}

class WheelScroll {
    #target;
    #sink;
    #modifier;
    #controller = new AbortController();

    constructor(target, sink, modifier) {
        this.#target = target;
        this.#sink = sink;
        this.#modifier = modifier ? `${modifier}Key` : null;

        // A passive listener cannot call preventDefault, which keeps the turn from the page.
        target.addEventListener('wheel', event => this.#handle(event), { signal: this.#controller.signal, passive: false });
    }

    dispose() {
        this.#controller.abort();
    }

    #handle(event) {
        // Without its key held the turn is not answered.
        if (this.#modifier && !event[this.#modifier]) {
            return;
        }

        // A popup or scrolling panel over the target takes the turn.
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

            // A fixed element takes the page's turn.
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
        // A sideways delta is left to the browser.
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

        // The turn is consumed at either end too.
        event.preventDefault();

        if (Math.abs(next - offset) < 1) {
            return;
        }

        // Each notch lands at once.
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

        // The turn is consumed.
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

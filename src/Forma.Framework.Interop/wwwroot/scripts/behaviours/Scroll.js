/**
 * Scroll — what a surface does with scrolling
 *
 * One handle covers whichever of these a surface asks for: watching how far its content can
 * still move along an axis, how far its frozen parts reach in from the start and how far the
 * frozen corner reaches across it; turning a wheel that reports vertically into movement along
 * a track, or telling an owner which way it turned; asking for the next page as a sentinel
 * comes into view; and keeping the arrow, Home and End keys from scrolling the page under a
 * list that owns them. Moving a container to a position it cannot express declaratively, to its
 * centre or one step along an axis, is exported beside it. Inline offsets are read through
 * Math.abs where a right-to-left container counts scrollLeft downwards from zero.
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

/** The keys a list owns, whose default would scroll the page. */
const NavigationKeys = new Set(['ArrowUp', 'ArrowDown', 'Home', 'End']);

/** @typedef {{ invokeMethodAsync: (name: string, ...args: unknown[]) => Promise<unknown> }} Owner */

/** Reports to an owner that may already have been disposed by the time the report lands. */
function tell(owner, method, ...args) {
    owner.invokeMethodAsync(method, ...args).catch(() => {});
}

/**
 * @param {HTMLElement} target The surface.
 * @param {{
 *   boundary?: { owner: Owner, axis?: 'inline' | 'block', frozen?: string | null },
 *   wheel?: { track?: HTMLElement, owner?: Owner, modifier?: string },
 *   sentinel?: { owner: Owner, root?: HTMLElement | null, rootMargin?: string },
 *   guard?: boolean
 * }} options
 *   What the handle does for the target: report through HandleOverflowAsync, in one object, how far it can still
 *   scroll along the axis, how far the parts matching the frozen selector reach in from its
 *   start, how far across the axis the frozen corner reaches and how much of it is in view; answer a wheel over it by
 *   moving the track, the target when none is named, or by telling the owner the direction
 *   through HandleWheelAsync, only with the modifier key held when one is named; treat the
 *   target as a sentinel and tell the owner through HandleReachedAsync as it comes into view of
 *   the root; and keep the navigation keys from scrolling the page.
 */
export function attach(target, options = {}) {
    return new Scroll(target, options);
}

/** @param {HTMLElement} container */
export function toCenter(container) {
    container.scrollLeft = (container.scrollWidth - container.clientWidth) / 2;
    container.scrollTop = (container.scrollHeight - container.clientHeight) / 2;
}

/**
 * Moves the container one step along the axis: to the next of the stops its content marks, drawn up to the
 * inset from the container's start, or by most of a page when the content marks none.
 * @param {HTMLElement} container
 * @param {number} direction -1 toward the start of the container, 1 toward its end.
 * @param {'inline' | 'block'} axis The axis to move along.
 * @param {{ selector: string, inset?: number } | null} stops The parts the content marks as stops, and how far in from the start a stop comes to rest.
 */
export function step(container, direction, axis = 'inline', stops = null) {
    const positions = stops ? stopPositions(container, axis, stops.selector) : [];

    if (positions.length === 0) {
        byPage(container, direction, axis);
        return;
    }

    const isBlock = axis === 'block';
    const offset = isBlock ? container.scrollTop : Math.abs(container.scrollLeft);
    const inset = stops.inset ?? 0;

    // The stop in place stands at the inset; the step goes to the nearest stop past it, and past the last stop to
    // the container's end, so the rest of the last stop can still be brought into view.
    const target = direction > 0
        ? positions.find(position => position - inset > offset + 1)
        : positions.findLast(position => position - inset < offset - 1);
    const end = isBlock ? container.scrollHeight - container.clientHeight : container.scrollWidth - container.clientWidth;
    const destination = target === undefined
        ? (direction > 0 ? end : 0)
        : Math.min(end, Math.max(0, target - inset));

    if (Math.abs(destination - offset) < 1) {
        return;
    }

    if (isBlock) {
        container.scrollTo({ top: destination, behavior: 'smooth' });
        return;
    }

    // A right-to-left container counts scrollLeft downwards from zero, so the destination mirrors with it.
    const isRightToLeft = getComputedStyle(container).direction === 'rtl';
    container.scrollTo({ left: isRightToLeft ? -destination : destination, behavior: 'smooth' });
}

// The distinct starts of the stops, measured along the axis from the content's start and sorted.
function stopPositions(container, axis, selector) {
    const box = container.getBoundingClientRect();
    const isBlock = axis === 'block';
    const isRightToLeft = getComputedStyle(container).direction === 'rtl';
    const offset = isBlock ? container.scrollTop : Math.abs(container.scrollLeft);
    const starts = new Set();

    for (const stop of container.querySelectorAll(selector)) {
        const rect = stop.getBoundingClientRect();
        const start = isBlock ? rect.top - box.top : isRightToLeft ? box.right - rect.right : rect.left - box.left;
        starts.add(Math.round(start + offset));
    }

    return [...starts].sort((first, second) => first - second);
}

function byPage(container, direction, axis) {
    if (axis === 'block') {
        container.scrollBy({ top: container.clientHeight * 0.8 * direction, behavior: 'smooth' });
        return;
    }

    // A right-to-left container counts scrollLeft downwards from zero, so the step mirrors with it.
    const isRightToLeft = getComputedStyle(container).direction === 'rtl';
    const distance = container.clientWidth * 0.8 * direction;

    container.scrollBy({ left: isRightToLeft ? -distance : distance, behavior: 'smooth' });
}

/** The parts a surface asked for, disposed together. */
export class Scroll {
    #parts = [];

    constructor(target, { boundary, wheel, sentinel, guard } = {}) {
        if (boundary) {
            this.#parts.push(new BoundaryWatch(target, boundary));
        }

        if (wheel) {
            this.#parts.push(new WheelTurn(target, wheel));
        }

        if (sentinel) {
            this.#parts.push(new Sentinel(target, sentinel));
        }

        if (guard) {
            this.#parts.push(new NavigationGuard(target));
        }
    }

    dispose() {
        for (const part of this.#parts) {
            part.dispose();
        }

        this.#parts = [];
    }
}

/** Reports how much of the target is still out of view along an axis, how far its frozen parts reach in and how far the frozen corner reaches across it. */
class BoundaryWatch {
    #track;
    #owner;
    #axis;
    #frozen;
    #controller = new AbortController();
    #resize;
    #mutations;
    #watched = [];
    #last = null;
    #isQueued = false;
    #frame = null;
    #isDisposed = false;

    constructor(track, { owner, axis = 'inline', frozen = null }) {
        this.#track = track;
        this.#owner = owner;
        this.#axis = axis;
        this.#frozen = frozen;

        track.addEventListener('scroll', () => this.#schedule(), { signal: this.#controller.signal, passive: true });

        this.#resize = new ResizeObserver(() => this.#schedule());
        this.#resize.observe(track);
        this.#watchChildren();

        // Children can be added or removed without the track's own box changing, which no ResizeObserver reports.
        this.#mutations = new MutationObserver(() => {
            this.#watchChildren();
            this.#schedule();
        });

        this.#mutations.observe(track, { childList: true });
        this.#schedule();
    }

    dispose() {
        this.#isDisposed = true;

        if (this.#frame !== null) {
            cancelAnimationFrame(this.#frame);
            this.#frame = null;
        }

        this.#controller.abort();
        this.#resize.disconnect();
        this.#mutations.disconnect();
    }

    // A child resizing changes what fits, so each is watched alongside the track.
    #watchChildren() {
        for (const child of this.#watched) {
            this.#resize.unobserve(child);
        }

        this.#watched = [...this.#track.children];

        for (const child of this.#watched) {
            this.#resize.observe(child);
        }
    }

    // Scrolling fires far faster than the circuit can answer, so reports are coalesced to one a frame.
    #schedule() {
        if (this.#isQueued || this.#isDisposed) {
            return;
        }

        this.#isQueued = true;
        this.#frame = requestAnimationFrame(() => this.#report());
    }

    #report() {
        this.#isQueued = false;
        this.#frame = null;

        // A track taken out of the document collapses to nothing, and its owner is on its way out with it.
        if (this.#isDisposed || !this.#track.isConnected) {
            return;
        }

        const next = this.#measure();

        if (this.#last
            && this.#last.canScrollBack === next.canScrollBack
            && this.#last.canScrollForward === next.canScrollForward
            && this.#last.frozenExtent === next.frozenExtent
            && this.#last.cornerExtent === next.cornerExtent
            && this.#last.viewportExtent === next.viewportExtent) {
            return;
        }

        this.#last = next;
        tell(this.#owner, 'HandleOverflowAsync', next);
    }

    #measure() {
        const isBlock = this.#axis === 'block';
        const offset = isBlock ? this.#track.scrollTop : Math.abs(this.#track.scrollLeft);
        const slack = isBlock ? this.#track.scrollHeight - this.#track.clientHeight : this.#track.scrollWidth - this.#track.clientWidth;

        const frozen = this.#frozen ? this.#frozenRegion() : { extent: 0, corner: 0 };

        // A sub-pixel track size leaves a fractional remainder that would otherwise read as scrollable.
        return {
            canScrollBack: slack > 1 && offset > 1,
            canScrollForward: slack > 1 && offset < slack - 1,
            frozenExtent: frozen.extent,
            cornerExtent: frozen.corner,
            viewportExtent: isBlock ? this.#track.clientHeight : this.#track.clientWidth,
        };
    }

    // The extent is the far edge along the axis of the frozen part reaching furthest in from the track's start; the
    // corner is the near far edge across the axis, where the frozen part in the track's start corner ends.
    #frozenRegion() {
        const box = this.#track.getBoundingClientRect();
        const isBlock = this.#axis === 'block';
        const isRightToLeft = getComputedStyle(this.#track).direction === 'rtl';
        const inlineEnd = rect => isRightToLeft ? box.right - rect.left : rect.right - box.left;
        const blockEnd = rect => rect.bottom - box.top;
        const along = isBlock ? blockEnd : inlineEnd;
        const across = isBlock ? inlineEnd : blockEnd;
        let extent = 0;
        let corner = Infinity;

        for (const part of this.#track.querySelectorAll(this.#frozen)) {
            const rect = part.getBoundingClientRect();
            extent = Math.max(extent, along(rect));
            corner = Math.min(corner, across(rect));
        }

        return { extent: Math.round(extent), corner: extent > 0 ? Math.round(corner) : 0 };
    }
}

/** Answers a wheel turned over the target, which reports vertically alone: a track moves by it, or an owner is told its direction. */
class WheelTurn {
    #target;
    #sink;
    #modifier;
    #controller = new AbortController();

    constructor(target, { track, owner, modifier }) {
        this.#target = target;
        this.#sink = owner ? new DirectionSink(owner) : new TrackSink(track ?? target);
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
        tell(this.#owner, 'HandleWheelAsync', Math.sign(delta));
    }
}

/** Asks for the next page as the target comes into view; the handle keeps the observer, so the target can be released before it leaves the DOM. */
class Sentinel {
    #observer;
    #isDisposed = false;

    constructor(target, { owner, root = null, rootMargin = '200px' }) {
        // A panel that scrolls rather than the page is the root, so intersection is measured against it and not the viewport.
        this.#observer = new IntersectionObserver(observations => {
            // Observations already queued still run after disconnect, and the owner may be gone by then.
            if (!this.#isDisposed && observations.some(observation => observation.isIntersecting)) {
                tell(owner, 'HandleReachedAsync');
            }
        }, { root, rootMargin });

        this.#observer.observe(target);
    }

    dispose() {
        this.#isDisposed = true;
        this.#observer.disconnect();
    }
}

/**
 * Stops the page scrolling under a list that owns its arrow, Home and End keys. Blazor fixes
 * preventDefault at render time and cannot choose per key, so the default is suppressed here for
 * those keys alone, leaving Enter, Space and Tab to act normally; the keys themselves are still
 * handled in C#.
 */
class NavigationGuard {
    #controller = new AbortController();

    constructor(target) {
        target.addEventListener('keydown', event => {
            if (NavigationKeys.has(event.key)) {
                event.preventDefault();
            }
        }, { signal: this.#controller.signal });
    }

    dispose() {
        this.#controller.abort();
    }
}

/**
 * Scroll — what a surface does with scrolling
 *
 * One handle covers whichever of these a surface asks for: watching how far its content can
 * still move along an axis, how far its frozen parts reach in from the start and how far the
 * frozen corner reaches across it; and telling its owner as a sentinel comes into view. Moving
 * a container to its center or one step along an axis is exported beside it. A right-to-left
 * container counts scrollLeft downward from zero, so inline offsets are read through Math.abs
 * and inline positions are mirrored.
 */

import { Direction } from '../../../core/scripts/Direction.js';
import { FrameScheduler } from '../../../core/scripts/FrameScheduler.js';
import { tell } from '../../../core/scripts/Owner.js';

/** @typedef {import('../../../core/scripts/Owner.js').Owner} Owner */

/**
 * @param {HTMLElement} target The surface.
 * @param {{
 *   boundary?: { owner: Owner, axis?: 'inline' | 'block', frozen?: string | null },
 *   sentinel?: { owner: Owner, root?: HTMLElement | null, rootMargin?: string }
 * }} options
 *   What the handle does for the target: report through HandleOverflowAsync how far it can still
 *   scroll along the axis, how far the parts matching the frozen selector reach in from its
 *   start, how far across the axis the frozen corner reaches and how much of it is in view; and treat the
 *   target as a sentinel and tell the owner through HandleReachedAsync as it comes into view of
 *   the root.
 */
export function attach(target, options = {}) {
    return new Scroll(target, options);
}

/** @param {HTMLElement} container */
export function toCenter(container) {
    const middle = (container.scrollWidth - container.clientWidth) / 2;

    container.scrollLeft = Direction.isRightToLeft(container) ? -middle : middle;
    container.scrollTop = (container.scrollHeight - container.clientHeight) / 2;
}

/**
 * Moves the container one step along the axis.
 * @param {HTMLElement} container
 * @param {number} direction -1 toward the start of the container, 1 toward its end.
 * @param {'inline' | 'block'} axis The axis to move along.
 * @param {{ selector: string, inset?: number } | null} stops The parts the content marks as stops, and how far in from the start a stop comes to rest; null steps by most of a page.
 */
export function step(container, direction, axis = 'inline', stops = null) {
    const isBlock = axis === 'block';
    const isRightToLeft = !isBlock && Direction.isRightToLeft(container);
    const positions = stops ? stopPositions(container, isBlock, isRightToLeft, stops.selector) : [];

    if (positions.length === 0) {
        byPage(container, direction, isBlock, isRightToLeft);
        return;
    }

    const offset = isBlock ? container.scrollTop : Math.abs(container.scrollLeft);
    const inset = stops.inset ?? 0;

    // Past the last stop the step goes to the container's end, bringing the rest of the last stop into view.
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

    container.scrollTo({ left: isRightToLeft ? -destination : destination, behavior: 'smooth' });
}

/** The distinct starts of the stops, measured along the axis from the content's start, sorted. */
function stopPositions(container, isBlock, isRightToLeft, selector) {
    const box = container.getBoundingClientRect();
    const offset = isBlock ? container.scrollTop : Math.abs(container.scrollLeft);
    const starts = new Set();

    for (const stop of container.querySelectorAll(selector)) {
        const rect = stop.getBoundingClientRect();
        const start = isBlock ? rect.top - box.top : isRightToLeft ? box.right - rect.right : rect.left - box.left;
        starts.add(Math.round(start + offset));
    }

    return [...starts].sort((first, second) => first - second);
}

function byPage(container, direction, isBlock, isRightToLeft) {
    if (isBlock) {
        container.scrollBy({ top: container.clientHeight * 0.8 * direction, behavior: 'smooth' });
        return;
    }

    const distance = container.clientWidth * 0.8 * direction;

    container.scrollBy({ left: isRightToLeft ? -distance : distance, behavior: 'smooth' });
}

/** The parts a surface asked for, disposed together. */
export class Scroll {
    #parts = [];

    constructor(target, { boundary, sentinel } = {}) {
        if (boundary) {
            this.#parts.push(new BoundaryWatch(target, boundary));
        }

        if (sentinel) {
            this.#parts.push(new Sentinel(target, sentinel));
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
    #scheduler = new FrameScheduler();
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

        this.#mutations = new MutationObserver(() => {
            this.#watchChildren();
            this.#schedule();
        });

        this.#mutations.observe(track, { childList: true });
        this.#schedule();
    }

    dispose() {
        this.#isDisposed = true;
        this.#scheduler.cancel();
        this.#controller.abort();
        this.#resize.disconnect();
        this.#mutations.disconnect();
    }

    #watchChildren() {
        for (const child of this.#watched) {
            this.#resize.unobserve(child);
        }

        this.#watched = [...this.#track.children];

        for (const child of this.#watched) {
            this.#resize.observe(child);
        }
    }

    #schedule() {
        if (!this.#isDisposed) {
            this.#scheduler.request(() => this.#report());
        }
    }

    #report() {
        // A track out of the document measures as nothing.
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

        // A sub-pixel track size leaves a fractional remainder below one pixel.
        return {
            canScrollBack: slack > 1 && offset > 1,
            canScrollForward: slack > 1 && offset < slack - 1,
            frozenExtent: frozen.extent,
            cornerExtent: frozen.corner,
            viewportExtent: isBlock ? this.#track.clientHeight : this.#track.clientWidth,
        };
    }

    /**
     * How far the frozen parts reach: the extent is the far edge along the axis of the frozen part reaching
     * furthest in from the track's start; the corner is the nearest far edge across the axis, where the frozen
     * part in the track's start corner ends.
     */
    #frozenRegion() {
        const box = this.#track.getBoundingClientRect();
        const isBlock = this.#axis === 'block';
        const isRightToLeft = Direction.isRightToLeft(this.#track);
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

/** Tells the owner as the target comes into view. */
class Sentinel {
    #observer;
    #isDisposed = false;

    constructor(target, { owner, root = null, rootMargin = '200px' }) {
        this.#observer = new IntersectionObserver(observations => {
            // Observations queued before disconnect still run.
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

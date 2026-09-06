/**
 * Scroll — what a surface does with scrolling
 *
 * One handle covers whichever of these a surface asks for: watching how far its content can
 * still move along an axis, how far its frozen parts reach in from the start and how far the
 * frozen corner reaches across it; and asking for the next page as a sentinel comes into view.
 * Moving a container to a position it cannot express declaratively, to its centre or one step
 * along an axis, is exported beside it. Inline offsets are read through Math.abs where a
 * right-to-left container counts scrollLeft downwards from zero.
 */

import { Direction } from '../../../core/scripts/Direction.js';
import { Frame } from '../../../core/scripts/Frame.js';
import { tell } from '../../../core/scripts/Owner.js';

/** @typedef {import('../../../core/scripts/Owner.js').Owner} Owner */

/**
 * @param {HTMLElement} target The surface.
 * @param {{
 *   boundary?: { owner: Owner, axis?: 'inline' | 'block', frozen?: string | null },
 *   sentinel?: { owner: Owner, root?: HTMLElement | null, rootMargin?: string }
 * }} options
 *   What the handle does for the target: report through HandleOverflowAsync, in one object, how far it can still
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
    const isBlock = axis === 'block';
    // A right-to-left container counts scrollLeft downwards from zero, so every inline position mirrors with it.
    const isRightToLeft = !isBlock && Direction.isRightToLeft(container);
    const positions = stops ? stopPositions(container, isBlock, isRightToLeft, stops.selector) : [];

    if (positions.length === 0) {
        byPage(container, direction, isBlock, isRightToLeft);
        return;
    }

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

    container.scrollTo({ left: isRightToLeft ? -destination : destination, behavior: 'smooth' });
}

// The distinct starts of the stops, measured along the axis from the content's start and sorted.
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
    #frame = new Frame();
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
        this.#frame.cancel();
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
        if (!this.#isDisposed) {
            this.#frame.request(() => this.#report());
        }
    }

    #report() {
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

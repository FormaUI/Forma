/**
 * ScrollContainer — a scroll container along one axis: where its content stands, and moving it
 *
 * Offsets are logical, counted from the content's start. A right-to-left container counts scrollLeft
 * downward from zero, so an inline offset is read through Math.abs and an inline position is mirrored.
 * Every measurement and every movement reads the element as it is when it runs.
 */

import { Direction } from '../../../core/scripts/Direction.js';

/**
 * @typedef {{
 *   canScrollBack: boolean, canScrollForward: boolean,
 *   frozenExtent: number, cornerExtent: number, viewportExtent: number | null,
 * }} ScrollState
 */

export class ScrollContainer {
    #element;
    #isBlock;

    /**
     * @param {HTMLElement} element The scroll container.
     * @param {'inline' | 'block'} axis The axis its content moves along.
     */
    constructor(element, axis = 'inline') {
        this.#element = element;
        this.#isBlock = axis === 'block';
    }

    /** Whether the content overflows the container along its axis; a sub-pixel container size leaves a remainder below one pixel. */
    get isScrollable() {
        return this.#slack() > 1;
    }

    /**
     * Measures where the content stands: whether it can still move toward the start and the end, how far the
     * parts matching the frozen selector reach in and across, and, where asked for, the visible extent.
     * @param {{ frozen?: string | null, viewportExtent?: boolean }} options
     * @returns {ScrollState}
     */
    measure({ frozen = null, viewportExtent = false } = {}) {
        const offset = this.#offset();
        const slack = this.#slack();
        const region = frozen ? this.#frozenRegion(frozen) : { extent: 0, corner: 0 };

        return {
            canScrollBack: slack > 1 && offset > 1,
            canScrollForward: slack > 1 && offset < slack - 1,
            frozenExtent: region.extent,
            cornerExtent: region.corner,
            viewportExtent: viewportExtent ? (this.#isBlock ? this.#element.clientHeight : this.#element.clientWidth) : null,
        };
    }

    /**
     * Moves one step, smoothly, and reports whether it started moving: to the next stop drawn up to the frozen
     * parts, past the last stop to the end, or by most of a page where no stop is marked.
     * @param {-1 | 1} direction -1 toward the start, 1 toward the end.
     * @param {{ stops?: string | null, frozen?: string | null }} options The selectors of the stops and of the frozen parts.
     */
    step(direction, { stops = null, frozen = null } = {}) {
        const positions = stops ? this.#stopPositions(stops) : [];

        if (positions.length === 0) {
            this.#moveByPage(direction);
            return true;
        }

        const inset = frozen ? this.#frozenRegion(frozen).extent : 0;
        const offset = this.#offset();
        const target = direction > 0
            ? positions.find(position => position - inset > offset + 1)
            : positions.findLast(position => position - inset < offset - 1);
        const destination = target === undefined
            ? (direction > 0 ? this.#slack() : 0)
            : Math.min(this.#slack(), Math.max(0, target - inset));

        if (Math.abs(destination - offset) < 1) {
            return false;
        }

        this.#scrollTo(destination, 'smooth');

        return true;
    }

    /**
     * Moves the content by the distance at once, stopping at either end, and reports whether it moved.
     * @param {number} distance Pixels toward the end, or toward the start where negative.
     */
    moveBy(distance) {
        const offset = this.#offset();
        const next = Math.max(0, Math.min(this.#slack(), offset + distance));

        if (Math.abs(next - offset) < 1) {
            return false;
        }

        this.#scrollTo(next, 'instant');

        return true;
    }

    #isMirrored() {
        return !this.#isBlock && Direction.isRightToLeft(this.#element);
    }

    #offset() {
        return this.#isBlock ? this.#element.scrollTop : Math.abs(this.#element.scrollLeft);
    }

    #slack() {
        return this.#isBlock
            ? this.#element.scrollHeight - this.#element.clientHeight
            : this.#element.scrollWidth - this.#element.clientWidth;
    }

    #scrollTo(position, behavior) {
        if (this.#isBlock) {
            this.#element.scrollTo({ top: position, behavior });
        } else {
            this.#element.scrollTo({ left: this.#isMirrored() ? -position : position, behavior });
        }
    }

    #moveByPage(direction) {
        if (this.#isBlock) {
            this.#element.scrollBy({ top: this.#element.clientHeight * 0.8 * direction, behavior: 'smooth' });
            return;
        }

        const distance = this.#element.clientWidth * 0.8 * direction;

        this.#element.scrollBy({ left: this.#isMirrored() ? -distance : distance, behavior: 'smooth' });
    }

    /** The distinct starts of the stops, measured along the axis from the content's start, sorted. */
    #stopPositions(selector) {
        const box = this.#element.getBoundingClientRect();
        const offset = this.#offset();
        const isMirrored = this.#isMirrored();
        const starts = new Set();

        for (const stop of this.#element.querySelectorAll(selector)) {
            const rect = stop.getBoundingClientRect();
            const start = this.#isBlock ? rect.top - box.top : isMirrored ? box.right - rect.right : rect.left - box.left;
            starts.add(Math.round(start + offset));
        }

        return [...starts].sort((first, second) => first - second);
    }

    /**
     * How far the frozen parts reach: the extent is the far edge along the axis of the part reaching furthest in from
     * the start; the corner is the nearest far edge across the axis, where the part in the start corner ends. A
     * right-to-left container is measured from its right edge, across the axis as along it.
     */
    #frozenRegion(selector) {
        const box = this.#element.getBoundingClientRect();
        const isRightToLeft = Direction.isRightToLeft(this.#element);
        const inlineEnd = rect => isRightToLeft ? box.right - rect.left : rect.right - box.left;
        const blockEnd = rect => rect.bottom - box.top;
        const along = this.#isBlock ? blockEnd : inlineEnd;
        const across = this.#isBlock ? inlineEnd : blockEnd;
        let extent = 0;
        let corner = Infinity;

        for (const part of this.#element.querySelectorAll(selector)) {
            const rect = part.getBoundingClientRect();
            extent = Math.max(extent, along(rect));
            corner = Math.min(corner, across(rect));
        }

        return { extent: Math.round(extent), corner: extent > 0 ? Math.round(corner) : 0 };
    }
}

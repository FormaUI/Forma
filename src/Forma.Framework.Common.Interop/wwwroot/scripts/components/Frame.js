/**
 * Frame — a frame's browser side: a box moved by the frame's drag region, resized at its edges,
 * stepped by the arrow keys on the focused drag region, and held within its bounds. The box is
 * the frame's own element, or the parent that presents it, which the frame then drives.
 *
 * The press-move-release is DragGesture's; what remains here is what the drag means for a
 * frame: which of its edges the press took hold of, the box that results, and the region and
 * size limits it is held within. The box is applied as the pointer moves, so the frame follows
 * without a round trip, and where it came to rest is reported once.
 */

import { attach as attachDragGesture } from '../behaviours/DragGesture.js';
import { tell } from '../../../core/scripts/Owner.js';

/**
 * @param {HTMLElement} target The frame element.
 * @param {{
 *   owner: import('../../../core/scripts/Owner.js').Owner,
 *   box: 'self' | 'parent',
 *   dragRegionClass: string,
 *   handleClass: string,
 *   stepPixels: number,
 *   bounds: HTMLElement | null
 * }} options
 *   The .NET owner told where the box came to rest; whether the box is the frame itself or its
 *   parent; the class of the element a drag moves the box by, which takes the arrow keys while
 *   focused; the class of the elements a drag resizes it by, each naming its edge in data-edge as
 *   one of n, s, e, w, ne, nw, se, sw; how far one arrow press moves or resizes the box; the
 *   element the box is held within, or null for the viewport.
 */
export function attach(target, options) {
    return new Frame(target, options);
}

class Frame {
    /** The class the frame carries while a drag is in progress. */
    static #DraggingClass = 'is-dragging';

    /** The class the frame carries while its drag region moves it. */
    static #MovableClass = 'is-movable';

    /** The class the frame carries while it fills its bounds, when it neither moves nor resizes. */
    static #MaximizedClass = 'is-maximized';

    /** The class the frame carries while it is held where it is, when it neither moves nor resizes either. */
    static #LockedClass = 'is-locked';

    /** The arrow keys and the direction each one moves or grows the box. */
    static #Steps = new Map([
        ['ArrowLeft', [-1, 0]],
        ['ArrowRight', [1, 0]],
        ['ArrowUp', [0, -1]],
        ['ArrowDown', [0, 1]],
    ]);

    #frame;
    #box;
    #owner;
    #dragRegionClass;
    #handleClass;
    #stepPixels;
    #bounds;
    #gesture = null;
    #observer = null;
    #listeners = new AbortController();
    #edge = null;
    #origin = null;

    constructor(frame, { owner, box, dragRegionClass, handleClass, stepPixels, bounds }) {
        this.#frame = frame;
        this.#box = box === 'parent' ? frame.parentElement : frame;
        this.#owner = owner;
        this.#dragRegionClass = dragRegionClass;
        this.#handleClass = handleClass;
        this.#stepPixels = stepPixels;
        this.#bounds = bounds ?? null;

        const { signal } = this.#listeners;

        this.#gesture = attachDragGesture(frame, {
            threshold: 0,
            axis: 'any',
            autoScroll: false,
            findSubject: event => this.#findSubject(event),
            onStart: () => this.#handleStart(),
            onMove: ({ clientX, clientY, startX, startY }) => this.#handleMove(clientX - startX, clientY - startY),
            onDrop: () => this.#handleDrop(),
            onCancel: () => this.#handleDrop(),
        });

        frame.addEventListener('keydown', event => this.#handleKeyDown(event), { signal });
        globalThis.addEventListener('resize', () => this.fit(), { passive: true, signal });

        // The frame is laid out again when it is shown, maximized or restored, and when its bounds change; each is a box to hold within its region.
        if (typeof ResizeObserver === 'function') {
            this.#observer = new ResizeObserver(() => this.fit());
            this.#observer.observe(frame);

            if (this.#bounds) {
                this.#observer.observe(this.#bounds);
            }
        }

        this.fit();
    }

    dispose() {
        this.#listeners.abort();
        this.#observer?.disconnect();
        this.#gesture?.dispose();
    }

    /** Tells the owner where the box stands now. */
    measure() {
        this.#report();
    }

    /** Holds a laid-out box within its bounds: a maximized one fills the region it is bound to, a placed one is shrunk and moved to fit, and a box that changed is reported. */
    fit() {
        const rect = this.#box.getBoundingClientRect();

        // A box not laid out yet has nothing to hold within anything.
        if (rect.width === 0 && rect.height === 0) {
            return;
        }

        const region = this.#region();

        if (this.#isMaximized) {
            if (this.#bounds) {
                this.#apply({ left: region.left, top: region.top, width: region.width, height: region.height });
            }

            return;
        }

        // A box bound to an element opens at that element's center rather than the viewport's, and takes its box from there on.
        if (!this.#isPlaced) {
            if (this.#bounds) {
                this.#apply({
                    left: region.left + Math.max(0, (region.width - rect.width) / 2),
                    top: region.top + Math.max(0, (region.height - rect.height) / 2),
                    width: Math.min(rect.width, region.width),
                    height: Math.min(rect.height, region.height),
                });
                this.#report();
            }

            return;
        }

        const { minWidth, minHeight, maxWidth, maxHeight } = this.#limits();
        const width = Math.max(minWidth, Math.min(rect.width, region.width, maxWidth));
        const height = Math.max(minHeight, Math.min(rect.height, region.height, maxHeight));
        const box = {
            left: clamp(rect.left, region.left, Math.max(region.left, region.right - width)),
            top: clamp(rect.top, region.top, Math.max(region.top, region.bottom - height)),
            width,
            height,
        };

        if (box.left !== rect.left || box.top !== rect.top || box.width !== rect.width || box.height !== rect.height) {
            this.#apply(box);
            this.#report();
        }
    }

    /**
     * Moves the box by a distance, or grows it from its bottom-right corner by that distance, as a keyboard
     * step does, held within the same bounds as a drag.
     * @param {number} dx
     * @param {number} dy
     * @param {boolean} resize
     */
    nudge(dx, dy, resize) {
        if (resize ? !this.#isResizable : !this.#isMovable) {
            return;
        }

        this.#origin = this.#box.getBoundingClientRect();
        this.#edge = resize ? 'se' : null;
        this.#apply(this.#edge ? this.#resized(dx, dy) : this.#moved(dx, dy));
        this.#origin = null;
        this.#report();
    }

    get #isMaximized() {
        return this.#frame.classList.contains(Frame.#MaximizedClass);
    }

    /** Whether the box stays where it is: maximized or locked. */
    get #isHeld() {
        return this.#isMaximized || this.#frame.classList.contains(Frame.#LockedClass);
    }

    get #isMovable() {
        return !this.#isHeld && this.#frame.classList.contains(Frame.#MovableClass);
    }

    /** Whether the frame carries resize handles, which it does only while it may be resized. */
    get #isResizable() {
        return !this.#isHeld && this.#frame.querySelector(`.${this.#handleClass}`) !== null;
    }

    /** Whether the box has been given a position of its own, as opposed to the one its layout or stylesheet gives it. */
    get #isPlaced() {
        return this.#box.style.left !== '';
    }

    /** The region the box is held within: its bounding element's box, or the viewport. */
    #region() {
        if (this.#bounds) {
            const { left, top, width, height, right, bottom } = this.#bounds.getBoundingClientRect();
            return { left, top, width, height, right, bottom };
        }

        return { left: 0, top: 0, width: globalThis.innerWidth, height: globalThis.innerHeight, right: globalThis.innerWidth, bottom: globalThis.innerHeight };
    }

    /** An arrow pressed on the focused drag region steps the box, or resizes it with Shift, and never scrolls the page. */
    #handleKeyDown(event) {
        const step = Frame.#Steps.get(event.key);

        if (!step || !event.target.classList.contains(this.#dragRegionClass)) {
            return;
        }

        event.preventDefault();

        const [dx, dy] = step;
        this.nudge(dx * this.#stepPixels, dy * this.#stepPixels, event.shiftKey);
    }

    /** The drag region or the handle the press landed on; nothing while held, nothing for a press on a control inside the drag region, and nothing for the drag region while the frame cannot move. */
    #findSubject(event) {
        if (this.#isHeld) {
            return null;
        }

        const handle = event.target.closest(`.${this.#handleClass}`);
        if (handle && this.#frame.contains(handle)) {
            this.#edge = handle.dataset.edge;
            return handle;
        }

        const dragRegion = event.target.closest(`.${this.#dragRegionClass}`);
        if (!dragRegion || !this.#isMovable || !this.#frame.contains(dragRegion) || event.target.closest('button, a, input, select, textarea, [role="toolbar"]')) {
            return null;
        }

        this.#edge = null;
        return dragRegion;
    }

    #handleStart() {
        this.#origin = this.#box.getBoundingClientRect();
        this.#frame.classList.add(Frame.#DraggingClass);
    }

    #handleMove(dx, dy) {
        const box = this.#edge ? this.#resized(dx, dy) : this.#moved(dx, dy);
        this.#apply(box);
    }

    #handleDrop() {
        this.#frame.classList.remove(Frame.#DraggingClass);
        this.#origin = null;
        this.#report();
    }

    #report() {
        const { left, top, width, height } = this.#box.getBoundingClientRect();
        tell(this.#owner, 'ReportPlacementAsync', left, top, width, height);
    }

    /** The box moved by the pointer's travel, held so it stays within its region. */
    #moved(dx, dy) {
        const { left, top, width, height } = this.#origin;
        const region = this.#region();
        return {
            left: clamp(left + dx, region.left, Math.max(region.left, region.right - width)),
            top: clamp(top + dy, region.top, Math.max(region.top, region.bottom - height)),
            width,
            height,
        };
    }

    /**
     * The box with the held edge pulled by the pointer's travel, the opposite edge staying put and the size held
     * within its limits and within its region; the minimum wins over a region too small to hold it.
     */
    #resized(dx, dy) {
        const { left, top, width, height, right, bottom } = this.#origin;
        const { minWidth, minHeight, maxWidth, maxHeight } = this.#limits();
        const region = this.#region();
        const box = { left, top, width, height };
        const widest = room => Math.max(minWidth, Math.min(maxWidth, room));
        const tallest = room => Math.max(minHeight, Math.min(maxHeight, room));

        if (this.#edge.includes('e')) {
            box.width = clamp(width + dx, minWidth, widest(region.right - left));
        }

        if (this.#edge.includes('w')) {
            box.width = clamp(width - dx, minWidth, widest(right - region.left));
            box.left = right - box.width;
        }

        if (this.#edge.includes('s')) {
            box.height = clamp(height + dy, minHeight, tallest(region.bottom - top));
        }

        if (this.#edge.includes('n')) {
            box.height = clamp(height - dy, minHeight, tallest(bottom - region.top));
            box.top = bottom - box.height;
        }

        return box;
    }

    /** The smallest and largest size the frame's and the box's styles allow, so a drag cannot take the box past either. */
    #limits() {
        const styles = this.#box === this.#frame ? [getComputedStyle(this.#frame)] : [getComputedStyle(this.#frame), getComputedStyle(this.#box)];
        const smallest = name => Math.max(...styles.map(style => parseFloat(style[name]) || 0));
        const largest = name => Math.min(...styles.map(style => parseFloat(style[name]) || Infinity));

        return {
            minWidth: smallest('minWidth'),
            minHeight: smallest('minHeight'),
            maxWidth: largest('maxWidth'),
            maxHeight: largest('maxHeight'),
        };
    }

    #apply({ left, top, width, height }) {
        this.#box.style.left = `${left}px`;
        this.#box.style.top = `${top}px`;
        this.#box.style.width = `${width}px`;
        this.#box.style.height = `${height}px`;
    }
}

function clamp(value, min, max) {
    return Math.min(Math.max(value, min), max);
}

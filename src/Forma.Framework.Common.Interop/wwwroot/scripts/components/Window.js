/**
 * Window — a window's browser side: floating, it is shown in the top layer within its bounds,
 * moved by its title bar and resized at its edges; modal, it keeps Tab inside itself and stands
 * on a backdrop; either way it remembers where focus was inside it and takes focus back.
 *
 * The press-move-release is DragGesture's; what remains here is what the drag means for a
 * window: which of its edges the press took hold of, the box that results, and the region
 * and size limits it is held within. The box is applied to the element as the pointer moves,
 * so the window follows without a round trip, and where it came to rest is reported once.
 */

import { attach as attachDragGesture } from '../behaviours/DragGesture.js';
import { Focus, focusablesIn } from '../behaviours/Focus.js';
import { tell } from '../../../core/scripts/Owner.js';

/**
 * @param {HTMLElement} target The window element; floating, it is positioned against the viewport.
 * @param {{
 *   owner: import('../../../core/scripts/Owner.js').Owner,
 *   titleBarClass: string,
 *   handleClass: string,
 *   stepPixels: number,
 *   floating: boolean,
 *   presented: boolean,
 *   modal: boolean,
 *   bounds: HTMLElement | null,
 *   backdrop: HTMLElement | null
 * }} options
 *   The .NET owner told where the window came to rest; the class of the element a drag moves
 *   the window by, which takes the arrow keys while focused; the class of the elements a drag
 *   resizes it by, each naming its edge in data-edge as one of n, s, e, w, ne, nw, se, sw; how
 *   far one arrow press moves or resizes the window; whether the window floats over the page;
 *   whether it is shown at all rather than minimized or hidden; whether Tab stays inside it;
 *   the element it is held within, or null for the viewport; the popover shown beneath it, if any.
 */
export function attach(target, options) {
    return new Window(target, options);
}

/** The window shown last, which the top layer keeps in front until another is shown. */
let topmost = null;

class Window {
    /** The class the window carries while a drag is in progress. */
    static #DraggingClass = 'is-dragging';

    /** The class a window carries while it fills its bounds, when it neither moves nor resizes. */
    static #MaximizedClass = 'is-maximized';

    /** The class a window carries while it is held where it is, when it neither moves nor resizes either. */
    static #LockedClass = 'is-locked';

    /** The arrow keys and the direction each one moves or grows the window. */
    static #Steps = new Map([
        ['ArrowLeft', [-1, 0]],
        ['ArrowRight', [1, 0]],
        ['ArrowUp', [0, -1]],
        ['ArrowDown', [0, 1]],
    ]);

    #window;
    #owner;
    #titleBarClass;
    #handleClass;
    #stepPixels;
    #floating;
    #presented;
    #bounds;
    #backdrop;
    #gesture = null;
    #boundsObserver = null;
    #listeners = new AbortController();
    #edge = null;
    #origin = null;
    #lastFocused = null;
    #raiseAfterDrop = false;

    constructor(window, { owner, titleBarClass, handleClass, stepPixels, floating, presented, modal, bounds, backdrop }) {
        this.#window = window;
        this.#owner = owner;
        this.#titleBarClass = titleBarClass;
        this.#handleClass = handleClass;
        this.#stepPixels = stepPixels;
        this.#floating = floating;
        this.#presented = presented;
        this.#bounds = bounds ?? null;
        this.#backdrop = backdrop ?? null;

        const { signal } = this.#listeners;
        window.addEventListener('focusout', event => this.#remember(event.target), { signal });

        if (!floating) {
            return;
        }

        this.#gesture = attachDragGesture(window, {
            threshold: 0,
            axis: 'any',
            autoScroll: false,
            findSubject: event => this.#findSubject(event),
            onStart: () => this.#handleStart(),
            onMove: ({ clientX, clientY, startX, startY }) => this.#handleMove(clientX - startX, clientY - startY),
            onDrop: () => this.#handleDrop(),
            onCancel: () => this.#handleDrop(),
        });

        if (modal) {
            new Focus(window, { wrap: true }, signal);
        }

        if (presented) {
            this.#show();
            this.fit();
        }

        window.addEventListener('keydown', event => this.#handleKeyDown(event), { signal });
        globalThis.addEventListener('resize', () => this.fit(), { passive: true, signal });

        if (this.#bounds && typeof ResizeObserver === 'function') {
            this.#boundsObserver = new ResizeObserver(() => this.fit());
            this.#boundsObserver.observe(this.#bounds);
        }
    }

    dispose() {
        this.#listeners.abort();
        this.#boundsObserver?.disconnect();
        this.#gesture?.dispose();
        this.#hide();
    }

    /** Tells the owner where the window stands now. */
    measure() {
        this.#report();
    }

    /** Moves focus into the window unless a control inside it already holds it: to the element last focused inside it, else its first control, else the window itself. A docked window is scrolled into view. */
    focus() {
        const active = document.activeElement;

        if (active !== this.#window && this.#window.contains(active)) {
            return;
        }

        const remembered = this.#lastFocused;
        const target = remembered?.isConnected && this.#window.contains(remembered)
            ? remembered
            : focusablesIn(this.#window)[0] ?? this.#window;

        target.focus();

        if (!this.#floating) {
            this.#window.scrollIntoView({ block: 'nearest' });
        }
    }

    /** Puts a presented floating window on top of every other floating window; one being dragged is raised once the drag ends, since hiding it would end its pointer capture. */
    raise() {
        if (!this.#floating || !this.#presented || topmost === this) {
            return;
        }

        if (this.#window.classList.contains(Window.#DraggingClass)) {
            this.#raiseAfterDrop = true;
            return;
        }

        this.#hide();
        this.#show();
    }

    /**
     * Shows a floating window in the top layer, or takes it out while it is minimized or hidden.
     * @param {boolean} presented
     */
    present(presented) {
        this.#presented = presented;

        if (!this.#floating) {
            return;
        }

        if (presented) {
            this.#show();
            this.fit();
        } else {
            this.#hide();
        }
    }

    /** Holds a presented floating window within its bounds: a maximized one fills the region it is bound to, a placed one is shrunk and moved to fit, and a box that changed is reported. */
    fit() {
        if (!this.#floating || !this.#presented) {
            return;
        }

        const region = this.#region();

        if (this.#isMaximized) {
            if (this.#bounds) {
                this.#apply({ left: region.left, top: region.top, width: region.width, height: region.height });
            }

            return;
        }

        // A window bound to an element opens at that element's center rather than the viewport's, and takes its box from there on.
        if (!this.#isPlaced) {
            if (this.#bounds) {
                const { width, height } = this.#window.getBoundingClientRect();
                this.#apply({
                    left: region.left + Math.max(0, (region.width - width) / 2),
                    top: region.top + Math.max(0, (region.height - height) / 2),
                    width: Math.min(width, region.width),
                    height: Math.min(height, region.height),
                });
                this.#report();
            }

            return;
        }

        const rect = this.#window.getBoundingClientRect();

        // A window not laid out yet has no box to hold within anything.
        if (rect.width === 0 && rect.height === 0) {
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
     * Moves the window by a distance, or grows it from its bottom-right corner by that distance, as a keyboard
     * step does, held within the same bounds as a drag.
     * @param {number} dx
     * @param {number} dy
     * @param {boolean} resize
     */
    nudge(dx, dy, resize) {
        if (!this.#floating || this.#isHeld) {
            return;
        }

        this.#origin = this.#window.getBoundingClientRect();
        this.#edge = resize ? 'se' : null;
        this.#apply(this.#edge ? this.#resized(dx, dy) : this.#moved(dx, dy));
        this.#origin = null;
        this.#report();
    }

    get #isMaximized() {
        return this.#window.classList.contains(Window.#MaximizedClass);
    }

    /** Whether the window stays where it is: maximized or locked. */
    get #isHeld() {
        return this.#isMaximized || this.#window.classList.contains(Window.#LockedClass);
    }

    /** Whether the window carries resize handles, which it does only while it may be resized. */
    get #isResizable() {
        return this.#window.querySelector(`.${this.#handleClass}`) !== null;
    }

    /** Whether the window has been given a box of its own, as opposed to the centered one its stylesheet opens it at. */
    get #isPlaced() {
        return this.#window.style.left !== '';
    }

    /** The region the window is held within: its bounding element's box, or the viewport. */
    #region() {
        if (this.#bounds) {
            const { left, top, width, height, right, bottom } = this.#bounds.getBoundingClientRect();
            return { left, top, width, height, right, bottom };
        }

        return { left: 0, top: 0, width: globalThis.innerWidth, height: globalThis.innerHeight, right: globalThis.innerWidth, bottom: globalThis.innerHeight };
    }

    /** Keeps the element focus is leaving, so focus can return there when the window is activated again. */
    #remember(element) {
        if (element instanceof HTMLElement && this.#window.contains(element)) {
            this.#lastFocused = element;
        }
    }

    /** An arrow pressed on the focused title bar steps the window, or resizes it with Shift, and never scrolls the page. */
    #handleKeyDown(event) {
        const step = Window.#Steps.get(event.key);

        if (!step || !event.target.classList.contains(this.#titleBarClass)) {
            return;
        }

        event.preventDefault();

        const resize = event.shiftKey;
        if (resize && !this.#isResizable) {
            return;
        }

        const [dx, dy] = step;
        this.nudge(dx * this.#stepPixels, dy * this.#stepPixels, resize);
    }

    /** Raises the window into the top layer, where no ancestor's box or stacking can reach it, with its backdrop shown first so it stands beneath. */
    #show() {
        if (this.#backdrop && typeof this.#backdrop.showPopover === 'function' && !this.#backdrop.matches(':popover-open')) {
            this.#backdrop.showPopover();
        }

        if (typeof this.#window.showPopover === 'function' && !this.#window.matches(':popover-open')) {
            this.#window.showPopover();
            topmost = this;
        }
    }

    #hide() {
        if (typeof this.#window.hidePopover === 'function' && this.#window.matches(':popover-open')) {
            this.#window.hidePopover();
        }

        if (this.#backdrop && typeof this.#backdrop.hidePopover === 'function' && this.#backdrop.matches(':popover-open')) {
            this.#backdrop.hidePopover();
        }

        if (topmost === this) {
            topmost = null;
        }
    }

    /** The title bar or the handle the press landed on; nothing while maximized or locked, and nothing for a press on a control inside the title bar. */
    #findSubject(event) {
        if (this.#isHeld) {
            return null;
        }

        const handle = event.target.closest(`.${this.#handleClass}`);
        if (handle && this.#window.contains(handle)) {
            this.#edge = handle.dataset.edge;
            return handle;
        }

        const titleBar = event.target.closest(`.${this.#titleBarClass}`);
        if (!titleBar || !this.#window.contains(titleBar) || event.target.closest('button, a, input, select, textarea, [role="toolbar"]')) {
            return null;
        }

        this.#edge = null;
        return titleBar;
    }

    #handleStart() {
        this.#origin = this.#window.getBoundingClientRect();
        this.#window.classList.add(Window.#DraggingClass);
    }

    #handleMove(dx, dy) {
        const box = this.#edge ? this.#resized(dx, dy) : this.#moved(dx, dy);
        this.#apply(box);
    }

    #handleDrop() {
        this.#window.classList.remove(Window.#DraggingClass);
        this.#origin = null;
        this.#report();

        if (this.#raiseAfterDrop) {
            this.#raiseAfterDrop = false;
            this.raise();
        }
    }

    #report() {
        const { left, top, width, height } = this.#window.getBoundingClientRect();
        tell(this.#owner, 'ReportPlacementAsync', left, top, width, height);
    }

    /** The box moved by the pointer's travel, held so the window stays within its region. */
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

    /** The smallest and largest size the window's styles allow it, so a drag cannot take it past either. */
    #limits() {
        const style = getComputedStyle(this.#window);
        return {
            minWidth: parseFloat(style.minWidth) || 0,
            minHeight: parseFloat(style.minHeight) || 0,
            maxWidth: parseFloat(style.maxWidth) || Infinity,
            maxHeight: parseFloat(style.maxHeight) || Infinity,
        };
    }

    #apply({ left, top, width, height }) {
        this.#window.style.left = `${left}px`;
        this.#window.style.top = `${top}px`;
        this.#window.style.width = `${width}px`;
        this.#window.style.height = `${height}px`;
    }
}

function clamp(value, min, max) {
    return Math.min(Math.max(value, min), max);
}

/**
 * SelectionFollower — an element kept beside the text selection inside its owner, holding focus while it shows
 */

import { attach as attachDragGesture } from './DragGesture.js';
import { Focus } from './Focus.js';
import { Frame } from '../../../core/scripts/Frame.js';

/** Space kept between the element and the selection, and between the element and the viewport's edge. */
const Gap = 8;

/** How far an arrow key on the handle moves the element. */
const Nudge = 8;

/**
 * @param {HTMLElement} target The element that follows the selection.
 * @param {HTMLElement | null} owner The element whose selections the target follows; any selection when null.
 * @param {{
 *   activeClass?: string,
 *   placement?: 'above' | 'below',
 *   handleSelector?: string,
 *   requiresOwner?: boolean
 * }} options
 */
export function attach(target, owner = null, options = {}) {
    return new SelectionFollower(target, owner, options);
}

class SelectionFollower {
    #target;
    #owner;
    #requiresOwner;
    #activeClass;
    #placement;
    #handleSelector;
    #gesture = null;
    #resizeObserver = null;
    #controller = new AbortController();
    #isDisposed = false;

    /** Where the last update put the element before the offset the user gave it. */
    #base = { left: 0, top: 0 };

    /** The displacement the user dragged or keyed the element to, kept across selections. */
    #offset = { x: 0, y: 0 };

    #dragOrigin = null;
    #frame = new Frame();

    constructor(target, owner = null, options = {}) {
        this.#target = target;
        this.#owner = owner;
        this.#requiresOwner = options?.requiresOwner ?? false;
        this.#activeClass = options?.activeClass ?? 'forma-selection-follower-active';
        this.#placement = options?.placement ?? 'below';
        this.#handleSelector = options?.handleSelector ?? null;

        const { signal } = this.#controller;

        new Focus(target, { wrap: true }, signal);
        target.addEventListener('keydown', event => this.#handleKeyDown(event), { signal });

        if (this.#handleSelector) {
            this.#gesture = attachDragGesture(target, {
                axis: 'any',
                autoScroll: false,
                findSubject: event => event.target.closest(this.#handleSelector) ? target : null,
                onStart: ({ startX, startY }) => this.#beginDrag(startX, startY),
                onMove: ({ clientX, clientY }) => this.#drag(clientX, clientY),
                onDrop: ({ clientX, clientY }) => this.#endDrag(clientX, clientY),
                onCancel: () => this.#cancelDrag(),
            });
        }

        document.addEventListener('selectionchange', () => this.#schedule(), { signal });
        window.addEventListener('resize', () => this.#schedule(), { signal });
        window.addEventListener('scroll', () => this.#schedule(), { signal, capture: true, passive: true });

        // The owner's own box changing — maximizing, a layout shift — moves the selection without any of the events above firing.
        this.#resizeObserver = new ResizeObserver(() => this.#schedule());
        this.#resizeObserver.observe(target);
        if (owner) {
            this.#resizeObserver.observe(owner);
        }

        this.update();
    }

    #handleKeyDown(event) {
        if (event.key === 'Escape' && this.#owner) {
            event.preventDefault();
            this.#owner.focus();
            return;
        }

        if (!this.#handleSelector || !event.target.closest(this.#handleSelector)) {
            return;
        }

        const step = SelectionFollower.#nudgeOf(event.key);
        if (step) {
            event.preventDefault();
            this.#offset = { x: this.#offset.x + step.x, y: this.#offset.y + step.y };
            this.#offset = this.#place();
        }
    }

    static #nudgeOf(key) {
        switch (key) {
            case 'ArrowUp': return { x: 0, y: -Nudge };
            case 'ArrowDown': return { x: 0, y: Nudge };
            case 'ArrowLeft': return { x: -Nudge, y: 0 };
            case 'ArrowRight': return { x: Nudge, y: 0 };
            default: return null;
        }
    }

    #beginDrag(clientX, clientY) {
        this.#dragOrigin = { clientX, clientY, offset: this.#offset };
    }

    #drag(clientX, clientY) {
        if (!this.#dragOrigin) {
            return;
        }

        const { offset } = this.#dragOrigin;
        this.#offset = {
            x: offset.x + clientX - this.#dragOrigin.clientX,
            y: offset.y + clientY - this.#dragOrigin.clientY,
        };
        this.#place();
    }

    #endDrag(clientX, clientY) {
        this.#drag(clientX, clientY);
        // The offset settles at where the element actually landed, so a drag past the viewport's edge leaves no distance to drag back through.
        this.#offset = this.#place();
        this.#dragOrigin = null;
    }

    #cancelDrag() {
        if (!this.#dragOrigin) {
            return;
        }

        this.#offset = this.#dragOrigin.offset;
        this.#dragOrigin = null;
        this.#place();
    }

    dispose() {
        this.#isDisposed = true;

        this.#frame.cancel();
        this.#gesture?.dispose();
        this.#resizeObserver?.disconnect();
        this.#controller.abort();
        this.#hide();
    }

    // Selection changes, scrolls and resizes arrive faster than frames are painted, so updates are coalesced to one a frame.
    #schedule() {
        this.#frame.request(() => this.update());
    }

    update() {
        // A drag in flight holds the element where the pointer has it, whatever the selection does meanwhile.
        if (this.#isDisposed || this.#dragOrigin) {
            return;
        }

        // An owner that was promised but is missing or gone leaves the element hidden, never following the whole page.
        if ((this.#requiresOwner && !this.#owner) || (this.#owner && !this.#owner.isConnected)) {
            this.#hide();
            return;
        }

        const selection = window.getSelection();
        if (!selection || selection.rangeCount === 0 || selection.isCollapsed) {
            this.#hide();
            return;
        }

        const range = selection.getRangeAt(0);
        const anchor = selection.anchorNode;
        const container = anchor?.nodeType === Node.TEXT_NODE ? anchor.parentElement : anchor;

        if (this.#owner && (!container || !this.#owner.contains(container))) {
            this.#hide();
            return;
        }

        const rangeRect = range.getBoundingClientRect();
        if (rangeRect.width === 0 && rangeRect.height === 0) {
            this.#hide();
            return;
        }

        this.#show();

        const { width, height } = this.#target.getBoundingClientRect();
        const left = rangeRect.left + (rangeRect.width / 2) - (width / 2);
        const above = rangeRect.top - height - Gap;
        const below = rangeRect.bottom + Gap;

        // The preferred side gives way to the other only when the element would leave the viewport there.
        let top = this.#placement === 'above' ? above : below;
        if (this.#placement === 'above' && above < Gap) {
            top = below;
        } else if (this.#placement === 'below' && below + height > window.innerHeight - Gap) {
            top = above;
        }

        this.#base = { left, top };
        this.#place();
    }

    /** Puts the element at its base plus offset, kept inside the viewport, and returns the offset it ended up at. */
    #place() {
        const { width, height } = this.#target.getBoundingClientRect();

        // A hidden element has no box to keep inside the viewport, so it keeps its offset for the next showing.
        if (width === 0 && height === 0) {
            return this.#offset;
        }

        const left = SelectionFollower.#clamp(this.#base.left + this.#offset.x, Gap, window.innerWidth - width - Gap);
        const top = SelectionFollower.#clamp(this.#base.top + this.#offset.y, Gap, window.innerHeight - height - Gap);

        this.#target.style.top = `${top}px`;
        this.#target.style.left = `${left}px`;

        return { x: left - this.#base.left, y: top - this.#base.top };
    }

    static #clamp(value, min, max) {
        return Math.max(min, Math.min(value, max));
    }

    #show() {
        if (this.#activeClass) {
            this.#target.classList.add(this.#activeClass);
        }
    }

    #hide() {
        if (this.#activeClass) {
            this.#target.classList.remove(this.#activeClass);
        }
    }
}

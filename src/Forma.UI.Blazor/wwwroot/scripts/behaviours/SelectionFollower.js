/**
 * SelectionFollower — an element kept over the text selection inside its owner, holding focus while it shows
 */

import { FocusCycle } from './FocusCycle.js';

export function attach(target, owner = null, options = {}) {
    return new SelectionFollower(target, owner, options);
}

class SelectionFollower {
    #target;
    #owner;
    #activeClass;
    #controller = new AbortController();
    #isDisposed = false;

    constructor(target, owner = null, options = {}) {
        this.#target = target;
        this.#owner = owner;
        this.#activeClass = options?.activeClass ?? 'forma-selection-follower-active';

        const { signal } = this.#controller;

        // The element holds focus while it shows; Escape hands focus back to the owner and leaves the selection standing.
        new FocusCycle(target, signal);
        target.addEventListener('keydown', event => this.#handleKeyDown(event), { signal });

        document.addEventListener('selectionchange', () => this.update(), { signal });
        window.addEventListener('resize', () => this.update(), { signal });
        window.addEventListener('scroll', () => this.update(), { signal, capture: true, passive: true });
    }

    #handleKeyDown(event) {
        if (event.key === 'Escape' && this.#owner) {
            event.preventDefault();
            this.#owner.focus();
        }
    }

    dispose() {
        this.#isDisposed = true;
        this.#controller.abort();
        this.#hide();
    }

    update() {
        if (this.#isDisposed) {
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

        const menuRect = this.#target.getBoundingClientRect();
        const viewportWidth = window.innerWidth;
        const gap = 8;

        let left = rangeRect.left + (rangeRect.width / 2) - (menuRect.width / 2);
        left = Math.max(8, Math.min(left, viewportWidth - menuRect.width - 8));

        let top = rangeRect.top - menuRect.height - gap;
        if (top < 8) {
            top = rangeRect.bottom + gap;
        }

        this.#target.style.top = `${top}px`;
        this.#target.style.left = `${left}px`;
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

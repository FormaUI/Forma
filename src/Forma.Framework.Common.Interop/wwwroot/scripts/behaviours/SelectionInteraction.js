/**
 * SelectionInteraction — click and keyboard selection across a list of keyed items
 *
 * A framework's own row click carries the item but no MouseEventArgs, so modifier keys
 * (ctrl/meta for toggle, shift for range) can only be read here, from the DOM event that
 * the item's own click handling never surfaces to Blazor. Each item is identified by a
 * key attribute already rendered by the component, so no DOM index or sort order has to be
 * reconstructed on this side.
 */

import { Controls } from '../../../core/scripts/Controls.js';
import { tell } from '../../../core/scripts/Owner.js';

/**
 * @param {HTMLElement} target
 * @param {{
 *   owner: import('../../../core/scripts/Owner.js').Owner,
 *   keyAttribute?: string,
 *   itemSelector?: string
 * }} options
 */
export function attach(target, options) {
    return new SelectionInteraction(target, options);
}

class SelectionInteraction {
    #target;
    #owner;
    #keyAttribute;
    #markerSelector;
    #itemSelector;
    #controller = new AbortController();
    #anchor = null;

    constructor(target, { owner, keyAttribute = 'data-row-key', itemSelector = 'tr' }) {
        this.#target = target;
        this.#owner = owner;
        this.#keyAttribute = keyAttribute;
        this.#markerSelector = `[${keyAttribute}]`;
        this.#itemSelector = itemSelector;

        const { signal } = this.#controller;

        target.addEventListener('mousedown', event => this.#handleMouseDown(event), { signal, capture: true });
        target.addEventListener('click', event => this.#handleClick(event), { signal, capture: true });
        target.addEventListener('keydown', event => this.#handleKeyDown(event), { signal, capture: true });
    }

    dispose() {
        this.#controller.abort();
    }

    #markers() {
        return [...this.#target.querySelectorAll(this.#markerSelector)];
    }

    #tokenOf(marker) {
        return Number(marker.getAttribute(this.#keyAttribute));
    }

    #markerIn(node) {
        return node?.closest(this.#itemSelector)?.querySelector(this.#markerSelector) ?? null;
    }

    #rangeTokens(from, to) {
        const all = this.#markers();
        const fromIndex = all.indexOf(from);
        const toIndex = all.indexOf(to);

        if (fromIndex === -1 || toIndex === -1) {
            return [this.#tokenOf(to)];
        }

        const [start, end] = fromIndex <= toIndex ? [fromIndex, toIndex] : [toIndex, fromIndex];

        return all.slice(start, end + 1).map(marker => this.#tokenOf(marker));
    }

    #focusMarker(marker, all) {
        for (const candidate of all) {
            candidate.tabIndex = candidate === marker ? 0 : -1;
        }

        marker.focus();
    }

    // Shift-click extends a native text selection on mousedown, before the click handler
    // below runs; its guards are mirrored so only a selecting click loses its default.
    #handleMouseDown(event) {
        if (event.button !== 0 || !event.shiftKey || event.altKey) {
            return;
        }

        if (event.target.closest(Controls.Interactive) || !this.#markerIn(event.target)) {
            return;
        }

        event.preventDefault();
    }

    #handleClick(event) {
        if (event.altKey) {
            return;
        }

        // A checkbox, an action icon or a link inside a cell is the click's real target;
        // consuming it here would leave that element dead.
        if (event.target.closest(Controls.Interactive)) {
            return;
        }

        const marker = this.#markerIn(event.target);

        if (!marker) {
            return;
        }

        event.stopPropagation();

        const toggle = event.ctrlKey || event.metaKey;

        if (event.shiftKey && this.#anchor) {
            tell(this.#owner, 'HandleRangeSelectAsync', this.#rangeTokens(this.#anchor, marker), toggle);
            return;
        }

        this.#anchor = marker;
        tell(this.#owner, 'HandleSelectAsync', this.#tokenOf(marker), toggle);
    }

    #handleKeyDown(event) {
        if (!Controls.NavigationKeys.has(event.key) || event.target.closest(Controls.TextEntry)) {
            return;
        }

        const all = this.#markers();

        if (all.length === 0) {
            return;
        }

        // Focus is the source of truth for "which item", not the roving tabindex: tabbing
        // straight onto one sets the former and leaves the latter wherever it was.
        const focused = document.activeElement?.closest(this.#markerSelector);
        const currentIndex = focused ? all.indexOf(focused) : -1;
        let nextIndex;

        if (event.key === 'Home') {
            nextIndex = 0;
        } else if (event.key === 'End') {
            nextIndex = all.length - 1;
        } else if (currentIndex === -1) {
            nextIndex = event.key === 'ArrowDown' ? 0 : all.length - 1;
        } else if (event.key === 'ArrowUp') {
            nextIndex = Math.max(0, currentIndex - 1);
        } else {
            nextIndex = Math.min(all.length - 1, currentIndex + 1);
        }

        if (nextIndex === currentIndex && event.key !== 'Home' && event.key !== 'End') {
            return;
        }

        event.preventDefault();

        const next = all[nextIndex];
        this.#focusMarker(next, all);

        if (event.ctrlKey || event.metaKey) {
            return;
        }

        if (event.shiftKey && this.#anchor) {
            tell(this.#owner, 'HandleRangeSelectAsync', this.#rangeTokens(this.#anchor, next), false);
            return;
        }

        this.#anchor = next;
        tell(this.#owner, 'HandleSelectAsync', this.#tokenOf(next), false);
    }
}

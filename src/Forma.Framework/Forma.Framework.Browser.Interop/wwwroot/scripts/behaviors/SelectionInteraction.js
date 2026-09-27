/**
 * SelectionInteraction — click and keyboard selection across a list of keyed items
 *
 * A framework's own row click carries the item but no MouseEventArgs, so modifier keys
 * (ctrl/meta for toggle, shift for range) can only be read here, from the DOM event that
 * the item's own click handling never surfaces to Blazor. Each item is identified by a
 * key attribute already rendered by the component, so no DOM index or sort order has to be
 * reconstructed on this side. The keys are routes on the input router, declared on the list,
 * so a key inside a nested scope, a select open in a cell, is that scope's, and a key in a
 * field inside an item is the field's by the router's native table rather than by a check here.
 */

import { Controls } from '../../../core/scripts/Controls.js';
import { tell } from '../../../core/scripts/Owner.js';
import { register } from '../../../input/scripts/InputRouter.js';

/** Where each key sends the focused item: a step either way, or to either end. */
const Moves = [
    { action: 'selection.next', key: 'ArrowDown', target: (index, count) => index < 0 ? 0 : Math.min(count - 1, index + 1) },
    { action: 'selection.previous', key: 'ArrowUp', target: (index, count) => index < 0 ? count - 1 : Math.max(0, index - 1) },
    { action: 'selection.first', key: 'Home', target: () => 0 },
    { action: 'selection.last', key: 'End', target: (index, count) => count - 1 },
];

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
    #registration;
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

        // Each key moves plain, with Shift extending the range, or with the primary modifier moving alone; one that
        // would move nothing matches nothing, so a press at the end of the list keeps its default.
        this.#registration = register(target, {
            routes: Moves.map(move => ({
                action: move.action,
                bindings: [
                    { type: 'keydown', key: move.key, when: event => this.#planMove(event, move) !== null },
                    { type: 'keydown', key: move.key, modifiers: { primary: true }, when: event => this.#planMove(event, move) !== null },
                ],
                handle: event => this.#move(event, move),
            })),
        });
    }

    dispose() {
        this.#controller.abort();
        this.#registration.dispose();
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

    /** The items and the place the key would move the focused one to, or null where there is nowhere to go. */
    #planMove(event, { key, target }) {
        const all = this.#markers();

        if (all.length === 0) {
            return null;
        }

        // Focus is the source of truth for "which item", not the roving tabindex: tabbing
        // straight onto one sets the former and leaves the latter wherever it was.
        const focused = document.activeElement?.closest(this.#markerSelector);
        const currentIndex = focused ? all.indexOf(focused) : -1;
        const nextIndex = target(currentIndex, all.length);
        const isEnd = key === 'Home' || key === 'End';

        return nextIndex === currentIndex && !isEnd ? null : { all, next: all[nextIndex] };
    }

    // The move is planned again here, since a host's remap of the action replaces the binding and its predicate with it.
    #move(event, move) {
        const plan = this.#planMove(event, move);

        if (!plan) {
            return;
        }

        const { all, next } = plan;
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

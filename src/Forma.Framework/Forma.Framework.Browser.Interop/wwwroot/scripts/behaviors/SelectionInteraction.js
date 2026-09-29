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
 *
 * The arrows move by where the items stand: down and up to the item below or above in the same
 * column, right and left to the neighbor in the same row, so one behavior serves rows down a
 * table, names flowing on into further columns, and cards packed across the width. Items laid
 * out nowhere yet, or all in one place, stand in one column in their document order.
 *
 * Enter on an item's marker activates the item, a route like the moves, so the form a host wraps
 * the list in never takes it as a submission. A list whose items are selected by their boxes
 * alone declares the activation and nothing else.
 */

import { Controls } from '../../../core/scripts/Controls.js';
import { tell } from '../../../core/scripts/Owner.js';
import { register } from '../../../input/scripts/InputRouter.js';

/** Where each key sends the focused item: a step in a direction, or to either end of its row or the list. */
const Moves = [
    { action: 'selection.next', key: 'ArrowDown', target: (layout, index) => layout.below(index) },
    { action: 'selection.previous', key: 'ArrowUp', target: (layout, index) => layout.above(index) },
    { action: 'selection.right', key: 'ArrowRight', target: (layout, index) => layout.beside(index, 1) },
    { action: 'selection.left', key: 'ArrowLeft', target: (layout, index) => layout.beside(index, -1) },
    { action: 'selection.first', key: 'Home', target: (layout, index) => layout.end(index, -1) },
    { action: 'selection.last', key: 'End', target: (layout, index) => layout.end(index, 1) },
];

/**
 * @param {HTMLElement} target
 * @param {{
 *   owner: import('../../../core/scripts/Owner.js').Owner,
 *   keyAttribute?: string,
 *   itemSelector?: string,
 *   selects?: boolean
 * }} options
 */
export function attach(target, options) {
    return new SelectionInteraction(target, options);
}

/** Where the items stand, read once per key press: each item's box, and the neighbors each direction reaches. */
class Layout {
    #boxes;
    #stacked;
    #rightToLeft;

    constructor(items, rightToLeft) {
        this.#boxes = items.map(item => item.getBoundingClientRect());
        this.#rightToLeft = rightToLeft;

        // Boxes that are all one box carry no layout, so the items stand in one column in their order.
        const first = this.#boxes[0];
        this.#stacked = this.#boxes.every(box => box.top === first.top && box.left === first.left);
    }

    get count() {
        return this.#boxes.length;
    }

    below(index) {
        return index < 0 ? 0 : this.#stacked ? Math.min(this.count - 1, index + 1) : this.#nearest(index, box => this.#sameColumn(index, box) && box.top > this.#boxes[index].top, box => box.top);
    }

    above(index) {
        return index < 0 ? this.count - 1 : this.#stacked ? Math.max(0, index - 1) : this.#nearest(index, box => this.#sameColumn(index, box) && box.top < this.#boxes[index].top, box => -box.top);
    }

    beside(index, direction) {
        if (index < 0 || this.#stacked) {
            return index < 0 ? 0 : index;
        }

        const left = this.#boxes[index].left;

        return this.#nearest(index, box => this.#sameRow(index, box) && Math.sign(box.left - left) === direction, box => direction * box.left);
    }

    // A row's own end where the item has neighbors in its row, else the list's.
    end(index, direction) {
        const rowmates = index < 0 || this.#stacked ? [] : this.#boxes.map((box, candidate) => candidate).filter(candidate => candidate !== index && this.#sameRow(index, this.#boxes[candidate]));

        if (rowmates.length === 0) {
            return direction < 0 ? 0 : this.count - 1;
        }

        const row = [index, ...rowmates];
        const toStart = direction < 0 ? !this.#rightToLeft : this.#rightToLeft;

        return row.reduce((best, candidate) => (toStart ? this.#boxes[candidate].left < this.#boxes[best].left : this.#boxes[candidate].left > this.#boxes[best].left) ? candidate : best, index);
    }

    #sameColumn(index, box) {
        return Math.abs(box.left - this.#boxes[index].left) < Math.max(1, this.#boxes[index].width / 2);
    }

    #sameRow(index, box) {
        return Math.abs(box.top - this.#boxes[index].top) < Math.max(1, this.#boxes[index].height / 2);
    }

    // The item nearest along the measure among those the direction admits, or the item itself where none does.
    #nearest(index, admits, measure) {
        let best = index;
        let bestMeasure = Infinity;

        this.#boxes.forEach((box, candidate) => {
            if (candidate === index || !admits(box)) {
                return;
            }

            const value = measure(box);

            if (value < bestMeasure) {
                bestMeasure = value;
                best = candidate;
            }
        });

        return best;
    }
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

    constructor(target, { owner, keyAttribute = 'data-row-key', itemSelector = 'tr', selects = true }) {
        this.#target = target;
        this.#owner = owner;
        this.#keyAttribute = keyAttribute;
        this.#markerSelector = `[${keyAttribute}]`;
        this.#itemSelector = itemSelector;

        const { signal } = this.#controller;

        if (selects) {
            target.addEventListener('mousedown', event => this.#handleMouseDown(event), { signal, capture: true });
            target.addEventListener('click', event => this.#handleClick(event), { signal, capture: true });
        }

        // Each key moves plain, with Shift extending the range, or with the primary modifier moving alone; one that
        // would move nothing matches nothing, so a press at the end of the list keeps its default.
        const moves = selects
            ? Moves.map(move => ({
                action: move.action,
                bindings: [
                    { type: 'keydown', key: move.key, when: event => this.#planMove(event, move) !== null },
                    { type: 'keydown', key: move.key, modifiers: { primary: true }, when: event => this.#planMove(event, move) !== null },
                ],
                handle: event => this.#move(event, move),
            }))
            : [];

        const activation = {
            action: 'selection.activate',
            bindings: [{ type: 'keydown', key: 'Enter', when: event => this.#focusedMarker(event) !== null }],
            handle: event => tell(this.#owner, 'HandleActivateAsync', this.#tokenOf(this.#focusedMarker(event))),
        };

        this.#registration = register(target, { routes: [...moves, activation] });
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

    /** The marker the key was pressed on, when it is one of this list's; a key in a control inside an item is the control's. */
    #focusedMarker(event) {
        const { target } = event;

        return target instanceof Element && target.matches(this.#markerSelector) && this.#target.contains(target) ? target : null;
    }

    #markerIn(node) {
        return node?.closest(this.#itemSelector)?.querySelector(this.#markerSelector) ?? null;
    }

    #itemOf(marker) {
        return marker.closest(this.#itemSelector) ?? marker;
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

        // The click lands focus on the item's marker, so the keys carry on from the item the pointer chose.
        this.#focusMarker(marker, this.#markers());

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
        // A key pressed on the list itself or within one of its items is the list's; one pressed in another
        // control inside the list, a header's button or a sidebar's row, is that control's own.
        if (event.target !== this.#target && !event.target.closest(this.#itemSelector)) {
            return null;
        }

        const all = this.#markers();

        if (all.length === 0) {
            return null;
        }

        // Focus is the source of truth for "which item", not the roving tabindex: tabbing
        // straight onto one sets the former and leaves the latter wherever it was.
        const focused = document.activeElement?.closest(this.#markerSelector);
        const currentIndex = focused ? all.indexOf(focused) : -1;
        // The reading direction is the list's own, which the page's may differ from.
        const rightToLeft = getComputedStyle(this.#target).direction === 'rtl';
        const nextIndex = target(new Layout(all.map(marker => this.#itemOf(marker)), rightToLeft), currentIndex);
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

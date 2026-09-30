/**
 * ReorderInteraction — dragging or keying an item into a new position in its list
 *
 * Built on DragGesture: a pointer drag finds the item under the pointer and the side of it
 * the dragged item lands on, and the keys on a handle are routes on the input router. The
 * owner is told the resulting position once per completed move.
 */

import { attach as attachDragGesture } from './DragGesture.js';
import { Controls } from '../../../core/scripts/Controls.js';
import { tell } from '../../../core/scripts/Owner.js';
import { register } from '../../../input/scripts/InputRouter.js';

/** Where each key sends the item: a step either way, or to either end. */
const Moves = [
    { action: 'sortable-list.move-up', key: 'ArrowUp', target: (index, count) => index - 1, insertAfter: false },
    { action: 'sortable-list.move-down', key: 'ArrowDown', target: (index, count) => index + 1, insertAfter: true },
    { action: 'sortable-list.move-first', key: 'Home', target: (index, count) => 0, insertAfter: false },
    { action: 'sortable-list.move-last', key: 'End', target: (index, count) => count - 1, insertAfter: true },
];

/** How long a touch rests on an item before it lifts, in milliseconds. */
const TouchHold = 400;

/**
 * @param {HTMLElement} target
 * @param {{
 *   owner: import('../../../core/scripts/Owner.js').Owner,
 *   dragFromItem?: boolean,
 *   handleSelector?: string,
 *   itemClass?: string,
 *   draggingClass?: string,
 *   activeClass?: string,
 *   beforeClass?: string,
 *   afterClass?: string
 * }} options
 */
export function attach(target, options) {
    return new ReorderInteraction(target, options);
}

class ReorderInteraction {
    #list;
    #owner;
    #dragFromItem;
    #handleSelector;
    #itemSelector;
    #ownItemSelector;
    #draggingClass;
    #activeClass;
    #beforeClass;
    #afterClass;
    #gesture;
    #registration;
    #sourceIndex = -1;

    /** The item currently marked as the drop target. */
    #indicated = null;

    constructor(list, {
        owner,
        dragFromItem = false,
        handleSelector = '.forma-sortable-list-handle',
        itemClass = 'forma-sortable-list-item',
        draggingClass = 'forma-sortable-list-dragging',
        activeClass = 'forma-sortable-list-active',
        beforeClass = 'forma-sortable-list-drop-before',
        afterClass = 'forma-sortable-list-drop-after',
    }) {
        this.#list = list;
        this.#owner = owner;
        this.#dragFromItem = dragFromItem;
        this.#handleSelector = handleSelector;
        this.#itemSelector = `.${itemClass}`;
        this.#ownItemSelector = `:scope > .${itemClass}`;
        this.#draggingClass = draggingClass;
        this.#activeClass = activeClass;
        this.#beforeClass = beforeClass;
        this.#afterClass = afterClass;

        this.#gesture = attachDragGesture(list, {
            // A handle carries touch-action: none, so only whole-item dragging needs the hold.
            holdDelay: dragFromItem ? TouchHold : 0,
            findSubject: event => this.#findSubject(event),
            onStart: ({ subject }) => this.#handleStart(subject),
            onMove: ({ subject, clientY }) => this.#handleMove(subject, clientY),
            onDrop: ({ subject, clientY }) => this.#handleDrop(subject, clientY),
            onCancel: ({ subject }) => this.#endDrag(subject),
        });

        // A key at the edge it would move toward matches no route and keeps its default.
        this.#registration = register(list, {
            routes: Moves.map(move => ({
                action: move.action,
                bindings: [{ type: 'keydown', key: move.key, when: event => this.#planMove(event, move) !== null }],
                handle: event => this.#move(event, move),
            })),
        });
    }

    dispose() {
        this.#gesture.dispose();
        this.#registration?.dispose();
    }

    #items() {
        return [...this.#list.querySelectorAll(this.#ownItemSelector)];
    }

    /** The list's own item holding a node, past any nested sortable list. */
    #ownItemOf(node) {
        for (let item = node?.closest(this.#itemSelector); item; item = item.parentElement?.closest(this.#itemSelector)) {
            if (item.parentElement === this.#list) {
                return item;
            }
        }

        return null;
    }

    #findSubject(event) {
        const handle = event.target.closest(this.#handleSelector);

        if (handle) {
            return handle.disabled ? null : this.#ownItemOf(handle);
        }

        if (!this.#dragFromItem || event.target.closest(Controls.Interactive)) {
            return null;
        }

        // The item's own handle reports whether the list is currently disabled.
        const item = this.#ownItemOf(event.target);

        return item && !item.querySelector(this.#handleSelector)?.disabled ? item : null;
    }

    /** Marks the target's item on the side the item would land, unmarking whatever was marked before. */
    #indicate(target) {
        this.#indicated?.classList.remove(this.#beforeClass, this.#afterClass);
        this.#indicated = target?.item ?? null;
        this.#indicated?.classList.add(target.insertAfter ? this.#afterClass : this.#beforeClass);
    }

    /** The item the pointer rests on and which side of its midpoint, after the last item below the list. */
    #dropTargetAt(clientY, items) {
        if (items.length === 0) {
            return null;
        }

        for (const item of items) {
            const rect = item.getBoundingClientRect();

            if (clientY < rect.bottom) {
                return { item, insertAfter: clientY > rect.top + rect.height / 2 };
            }
        }

        return { item: items[items.length - 1], insertAfter: true };
    }

    #handleStart(subject) {
        this.#sourceIndex = this.#items().indexOf(subject);
        subject.classList.add(this.#draggingClass);
        // The active class holds the drag cursor once the pointer leaves the handle.
        this.#list.classList.add(this.#activeClass);
    }

    #handleMove(subject, clientY) {
        const target = this.#dropTargetAt(clientY, this.#items());

        this.#indicate(target && target.item !== subject ? target : null);
    }

    #handleDrop(subject, clientY) {
        const items = this.#items();
        const target = this.#dropTargetAt(clientY, items);
        this.#endDrag(subject);

        if (target && target.item !== subject) {
            tell(this.#owner, 'HandleReorderAsync', this.#sourceIndex, items.indexOf(target.item), target.insertAfter);
        }
    }

    #endDrag(subject) {
        this.#indicate(null);
        subject.classList.remove(this.#draggingClass);
        this.#list.classList.remove(this.#activeClass);
    }

    /** The list's own item whose enabled handle the event was raised on. */
    #handleItemOf(event) {
        const handle = event.target.closest(this.#handleSelector);

        return handle && !handle.disabled ? this.#ownItemOf(handle) : null;
    }

    /** Where the move would take the item the event's handle belongs to. */
    #planMove(event, { target }) {
        const item = this.#handleItemOf(event);

        if (!item) {
            return null;
        }

        const all = this.#items();
        const currentIndex = all.indexOf(item);
        const targetIndex = target(currentIndex, all.length);

        return targetIndex === currentIndex || targetIndex < 0 || targetIndex >= all.length ? null : { currentIndex, targetIndex };
    }

    #move(event, move) {
        const plan = this.#planMove(event, move);

        if (plan) {
            tell(this.#owner, 'HandleReorderAsync', plan.currentIndex, plan.targetIndex, move.insertAfter);
        }
    }
}

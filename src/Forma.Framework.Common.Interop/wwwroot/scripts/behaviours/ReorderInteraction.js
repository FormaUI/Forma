/**
 * ReorderInteraction — dragging or keying an item into a new position in its list
 *
 * The press-move-release gesture, its pointer capture and the edge auto-scroll all belong
 * to DragGesture; what remains here is what a reorder means — which item a pointer is
 * over, which side of it, and the keyboard equivalent. Only the resulting position crosses
 * to .NET, once per completed move, since a pointermove fires many times a second and
 * round-tripping each one through Blazor Server would lag badly.
 */

import { attach as attachDragGesture } from './DragGesture.js';
import { Controls } from '../../../core/scripts/Controls.js';
import { tell } from '../../../core/scripts/Owner.js';

/** How long a touch rests on an item before it lifts, leaving shorter swipes to scroll. */
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
    #controller = new AbortController();
    #sourceIndex = -1;

    /** The item currently marked as the drop target, or null. */
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
            // Only whole-item dragging needs the hold: a handle carries touch-action: none,
            // so it never competes with a scroll in the first place.
            holdDelay: dragFromItem ? TouchHold : 0,
            findSubject: event => this.#findSubject(event),
            onStart: ({ subject }) => this.#handleStart(subject),
            onMove: ({ subject, clientY }) => this.#handleMove(subject, clientY),
            onDrop: ({ subject, clientY }) => this.#handleDrop(subject, clientY),
            onCancel: ({ subject }) => this.#endDrag(subject),
        });

        list.addEventListener('keydown', event => this.#handleKeyDown(event), { signal: this.#controller.signal });
    }

    dispose() {
        this.#gesture.dispose();
        this.#controller.abort();
    }

    #items() {
        return [...this.#list.querySelectorAll(this.#ownItemSelector)];
    }

    // The item an event belongs to, walking outward past any nested sortable list so a
    // list only ever answers for its own items.
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

    // Which item the pointer rests on and which side of its midpoint. Below the last item
    // it still resolves to "after the last", so releasing in the empty space under the list
    // sends the item to the end instead of doing nothing.
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
        // The cursor otherwise reverts to whatever sits under the pointer once it leaves
        // the handle, which is most of the drag.
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

    #handleKeyDown(event) {
        if (!Controls.NavigationKeys.has(event.key)) {
            return;
        }

        const handle = event.target.closest(this.#handleSelector);
        const item = handle ? this.#ownItemOf(handle) : null;

        if (!item) {
            return;
        }

        const all = this.#items();
        const currentIndex = all.indexOf(item);
        let targetIndex;
        let insertAfter;

        if (event.key === 'Home') {
            targetIndex = 0;
            insertAfter = false;
        } else if (event.key === 'End') {
            targetIndex = all.length - 1;
            insertAfter = true;
        } else if (event.key === 'ArrowUp') {
            targetIndex = currentIndex - 1;
            insertAfter = false;
        } else {
            targetIndex = currentIndex + 1;
            insertAfter = true;
        }

        // An item already at the edge it was asked to move toward stays put, and the key
        // keeps its default so the page still scrolls rather than swallowing the press.
        if (targetIndex === currentIndex || targetIndex < 0 || targetIndex >= all.length) {
            return;
        }

        event.preventDefault();
        tell(this.#owner, 'HandleReorderAsync', currentIndex, targetIndex, insertAfter);
    }
}

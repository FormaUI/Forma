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

/** Elements that own their own press, so an item never takes it for a drag. */
const INTERACTIVE = 'a[href], button, input, select, textarea, label, [role="button"], [role="link"], [contenteditable="true"]';

/** How long a touch rests on an item before it lifts, leaving shorter swipes to scroll. */
const TOUCH_HOLD = 400;

const NAVIGATION_KEYS = ['ArrowUp', 'ArrowDown', 'Home', 'End'];

/**
 * @param {HTMLElement} target
 * @param {{
 *   owner: { invokeMethodAsync: (name: string, ...args: unknown[]) => Promise<unknown> },
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
            holdDelay: dragFromItem ? TOUCH_HOLD : 0,
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

        if (!this.#dragFromItem || event.target.closest(INTERACTIVE)) {
            return null;
        }

        // The item's own handle reports whether the list is currently disabled.
        const item = this.#ownItemOf(event.target);

        return item && !item.querySelector(this.#handleSelector)?.disabled ? item : null;
    }

    #clearIndicators() {
        for (const item of this.#items()) {
            item.classList.remove(this.#beforeClass, this.#afterClass);
        }
    }

    // Which item the pointer rests on and which side of its midpoint. Below the last item
    // it still resolves to "after the last", so releasing in the empty space under the list
    // sends the item to the end instead of doing nothing.
    #dropTargetAt(clientY) {
        const all = this.#items();

        if (all.length === 0) {
            return null;
        }

        for (const item of all) {
            const rect = item.getBoundingClientRect();

            if (clientY < rect.bottom) {
                return { item, insertAfter: clientY > rect.top + rect.height / 2 };
            }
        }

        return { item: all[all.length - 1], insertAfter: true };
    }

    #handleStart(subject) {
        this.#sourceIndex = this.#items().indexOf(subject);
        subject.classList.add(this.#draggingClass);
        // The cursor otherwise reverts to whatever sits under the pointer once it leaves
        // the handle, which is most of the drag.
        this.#list.classList.add(this.#activeClass);
    }

    #handleMove(subject, clientY) {
        this.#clearIndicators();

        const target = this.#dropTargetAt(clientY);

        if (target && target.item !== subject) {
            target.item.classList.add(target.insertAfter ? this.#afterClass : this.#beforeClass);
        }
    }

    #handleDrop(subject, clientY) {
        const target = this.#dropTargetAt(clientY);
        this.#endDrag(subject);

        if (target && target.item !== subject) {
            this.#owner.invokeMethodAsync(
                'HandleReorderAsync', this.#sourceIndex, this.#items().indexOf(target.item), target.insertAfter);
        }
    }

    #endDrag(subject) {
        this.#clearIndicators();
        subject.classList.remove(this.#draggingClass);
        this.#list.classList.remove(this.#activeClass);
    }

    #handleKeyDown(event) {
        if (!NAVIGATION_KEYS.includes(event.key)) {
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
        this.#owner.invokeMethodAsync('HandleReorderAsync', currentIndex, targetIndex, insertAfter);
    }
}

/**
 * FormaSortableList — reorder interop
 *
 * The press-move-release gesture, its pointer capture and the edge auto-scroll all belong
 * to the shared DragGesture module; what remains here is what a reorder means — which row
 * a pointer is over, which side of it, and the keyboard equivalent. Only the resulting
 * position crosses to .NET, once per completed move, since a pointermove fires many times
 * a second and round-tripping each one through Blazor Server would lag badly.
 */

import { DragGesture } from '../../scripts/DragGesture.js';

const HANDLE = '.forma-sortable-list-handle';
const ITEM_CLASS = 'forma-sortable-list-item';
const ITEM = `.${ITEM_CLASS}`;
const OWN_ITEM = `:scope > ${ITEM}`;
const DRAGGING = 'forma-sortable-list-dragging';
const LIST_DRAGGING = 'forma-sortable-list-active';
const BEFORE = 'forma-sortable-list-drop-before';
const AFTER = 'forma-sortable-list-drop-after';

/** Elements that own their own press, so a row never takes it for a drag. */
const INTERACTIVE = 'a[href], button, input, select, textarea, label, [role="button"], [role="link"], [contenteditable="true"]';

/** How long a touch rests on a row before it lifts, leaving shorter swipes to scroll. */
const TOUCH_HOLD = 400;

/**
 * @param {HTMLElement} list
 * @param {DotNet.DotNetObject} dotNetRef
 * @param {boolean} dragFromItem
 */
export function attachSortable(list, dotNetRef, dragFromItem) {
    let sourceIndex = -1;

    function items() {
        return Array.from(list.querySelectorAll(OWN_ITEM));
    }

    // The item an event belongs to, walking outward past any nested sortable list so a
    // list only ever answers for its own rows.
    function ownItemOf(target) {
        for (let item = target?.closest(ITEM); item; item = item.parentElement?.closest(ITEM)) {
            if (item.parentElement === list) {
                return item;
            }
        }

        return null;
    }

    function clearIndicators() {
        for (const item of items()) {
            item.classList.remove(BEFORE, AFTER);
        }
    }

    // Which row the pointer rests on and which side of its midpoint. Below the last row it
    // still resolves to "after the last", so releasing in the empty space under the list
    // sends the row to the end instead of doing nothing.
    function dropTargetAt(clientY) {
        const all = items();
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

    function endDrag(subject) {
        clearIndicators();
        subject.classList.remove(DRAGGING);
        list.classList.remove(LIST_DRAGGING);
    }

    const gesture = DragGesture.create(list, {
        // Only whole-row dragging needs the hold: a handle carries touch-action: none, so it
        // never competes with a scroll in the first place.
        holdDelay: dragFromItem ? TOUCH_HOLD : 0,

        findSubject(event) {
            const handle = event.target.closest(HANDLE);
            if (handle) {
                return handle.disabled ? null : ownItemOf(handle);
            }

            if (!dragFromItem || event.target.closest(INTERACTIVE)) {
                return null;
            }

            // The row's own handle reports whether the list is currently disabled.
            const item = ownItemOf(event.target);
            return item && !item.querySelector(HANDLE)?.disabled ? item : null;
        },

        onStart({ subject }) {
            sourceIndex = items().indexOf(subject);
            subject.classList.add(DRAGGING);
            // The cursor otherwise reverts to whatever sits under the pointer once it
            // leaves the handle, which is most of the drag.
            list.classList.add(LIST_DRAGGING);
        },

        onMove({ subject, clientY }) {
            clearIndicators();
            const target = dropTargetAt(clientY);
            if (target && target.item !== subject) {
                target.item.classList.add(target.insertAfter ? AFTER : BEFORE);
            }
        },

        onDrop({ subject, clientY }) {
            const target = dropTargetAt(clientY);
            endDrag(subject);

            if (target && target.item !== subject) {
                void dotNetRef.invokeMethodAsync(
                    'HandleReorderAsync', sourceIndex, items().indexOf(target.item), target.insertAfter);
            }
        },

        onCancel({ subject }) {
            endDrag(subject);
        },
    });

    function handleKeydown(event) {
        if (!['ArrowUp', 'ArrowDown', 'Home', 'End'].includes(event.key)) {
            return;
        }

        const handle = event.target.closest(HANDLE);
        const item = handle ? ownItemOf(handle) : null;
        if (!item) {
            return;
        }

        const all = items();
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
        void dotNetRef.invokeMethodAsync('HandleReorderAsync', currentIndex, targetIndex, insertAfter);
    }

    gesture.attach();
    list.addEventListener('keydown', handleKeydown);

    list.removeAllSortableHandlers = () => {
        gesture.detach();
        list.removeEventListener('keydown', handleKeydown);
        delete list.removeAllSortableHandlers;
    };
}

/**
 * @param {HTMLElement} list
 */
export function detachSortable(list) {
    list.removeAllSortableHandlers?.();
}

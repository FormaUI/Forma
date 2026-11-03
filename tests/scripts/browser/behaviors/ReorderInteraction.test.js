import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { attach } from '#browser/behaviors/ReorderInteraction.js';
import { configure, resolve } from '#input/InputRouter.js';
import { fakeOwner } from '#support/owner.js';
import { pointerEvent, stackRows, stubPointerCapture } from '#support/platform.js';

describe('ReorderInteraction', () => {
    /** @type {HTMLElement} */
    let list;
    /** @type {ReturnType<typeof fakeOwner>} */
    let owner;

    const DRAGGING = 'forma-sortable-list-dragging';
    const ACTIVE = 'forma-sortable-list-active';
    const BEFORE = 'forma-sortable-list-drop-before';
    const AFTER = 'forma-sortable-list-drop-after';

    // Rows run from 200 to 360, clear of the edges where a held drag starts auto-scrolling.
    const rowOf = index => ({ top: 200 + (index * 40), middle: 220 + (index * 40) });

    const row = (label, trailing = '') => `
        <li class="forma-sortable-list-item">
            <button class="forma-sortable-list-handle">Move</button>
            <span class="label">${label}</span>${trailing}
        </li>`;

    beforeEach(() => {
        document.body.innerHTML = `
            <ul id="list">
                ${row('One')}
                ${row('Two')}
                ${row('Three')}
                ${row('Four', '<a href="#open">Open</a>')}
            </ul>`;
        list = document.querySelector('#list');
        owner = fakeOwner();
        stubPointerCapture(list);
        stackRows(items());
    });

    // The list handles attached in a test, whose registrations the router holds until they are disposed after it.
    const handles = [];
    const attachList = options => {
        const handle = attach(list, options);
        handles.push(handle);

        return handle;
    };

    afterEach(() => {
        handles.splice(0).forEach(handle => handle.dispose());
        configure({ bindings: {} });
        vi.useRealTimers();
    });

    const items = () => [...list.querySelectorAll('.forma-sortable-list-item')];
    const handleOf = index => items()[index].querySelector('.forma-sortable-list-handle');
    const labelOf = index => items()[index].querySelector('.label');
    const marked = className => items().findIndex(item => item.classList.contains(className));
    const reordered = () => expect(owner.invokeMethodAsync);

    const press = (element, clientY, pointerType = 'mouse') =>
        element.dispatchEvent(pointerEvent('pointerdown', { clientY, pointerType }));

    const move = (clientY, pointerType = 'mouse') =>
        list.dispatchEvent(pointerEvent('pointermove', { clientY, pointerType }));

    const lift = (clientY, pointerType = 'mouse') =>
        list.dispatchEvent(pointerEvent('pointerup', { clientY, pointerType }));

    const key = (element, name) => {
        const event = new KeyboardEvent('keydown', { key: name, bubbles: true, cancelable: true });
        element.dispatchEvent(event);

        return event;
    };

    describe('from the keyboard', () => {
        it('moves an item down one place', () => {
            attachList({ owner });

            key(handleOf(0), 'ArrowDown');

            reordered().toHaveBeenCalledExactlyOnceWith('HandleReorderAsync', 0, 1, true);
        });

        it('moves an item up one place', () => {
            attachList({ owner });

            key(handleOf(2), 'ArrowUp');

            reordered().toHaveBeenCalledExactlyOnceWith('HandleReorderAsync', 2, 1, false);
        });

        it('sends an item to the front of the list', () => {
            attachList({ owner });

            key(handleOf(2), 'Home');

            reordered().toHaveBeenCalledExactlyOnceWith('HandleReorderAsync', 2, 0, false);
        });

        it('sends an item to the end of the list', () => {
            attachList({ owner });

            key(handleOf(0), 'End');

            reordered().toHaveBeenCalledExactlyOnceWith('HandleReorderAsync', 0, 3, true);
        });

        it('claims the key it moved with', () => {
            attachList({ owner });

            expect(key(handleOf(0), 'ArrowDown').defaultPrevented).toBe(true);
        });

        // An item already at the edge stays put, and the key still scrolls the page.
        it('leaves the first item where it is on ArrowUp', () => {
            attachList({ owner });

            const event = key(handleOf(0), 'ArrowUp');

            reordered().not.toHaveBeenCalled();
            expect(event.defaultPrevented).toBe(false);
        });

        it('leaves the last item where it is on ArrowDown', () => {
            attachList({ owner });

            const event = key(handleOf(3), 'ArrowDown');

            reordered().not.toHaveBeenCalled();
            expect(event.defaultPrevented).toBe(false);
        });

        it('ignores a key pressed outside a handle', () => {
            attachList({ owner });

            key(labelOf(0), 'ArrowDown');

            reordered().not.toHaveBeenCalled();
        });

        it('ignores a key it does not move with', () => {
            attachList({ owner });

            key(handleOf(0), 'Enter');

            reordered().not.toHaveBeenCalled();
        });

        it('declares its keys as routes on the input router', () => {
            attachList({ owner });

            const event = key(handleOf(0), 'ArrowDown');

            expect(resolve(event)).toMatchObject({ owner: list, action: 'sortable-list.move-down', precedence: 'structural' });
        });

        it('leaves a key that moves nothing to the page', () => {
            attachList({ owner });
            const reached = vi.fn();
            document.body.addEventListener('keydown', reached, { once: true });

            const event = key(handleOf(0), 'Home');

            reordered().not.toHaveBeenCalled();
            expect(resolve(event).action).toBeNull();
            expect(reached).toHaveBeenCalledOnce();
        });

        it('takes the host\'s remap of a move', () => {
            configure({ bindings: { 'sortable-list.move-down': [{ type: 'keydown', key: 'j' }] } });
            attachList({ owner });

            key(handleOf(0), 'j');
            key(handleOf(0), 'ArrowDown');

            reordered().toHaveBeenCalledExactlyOnceWith('HandleReorderAsync', 0, 1, true);
        });

        it('answers for its own items alone when a list is nested', () => {
            labelOf(0).insertAdjacentHTML('afterend', `<ul id="inner">${row('Inner one')}${row('Inner two')}</ul>`);
            const inner = document.querySelector('#inner');
            const innerOwner = fakeOwner();
            attachList({ owner });
            handles.push(attach(inner, { owner: innerOwner }));

            key(inner.querySelector('.forma-sortable-list-handle'), 'ArrowDown');

            expect(innerOwner.invokeMethodAsync).toHaveBeenCalledExactlyOnceWith('HandleReorderAsync', 0, 1, true);
            reordered().not.toHaveBeenCalled();
        });

        it('leaves a key on a disabled handle alone', () => {
            attachList({ owner });
            handleOf(0).disabled = true;

            const event = key(handleOf(0), 'ArrowDown');

            reordered().not.toHaveBeenCalled();
            expect(event.defaultPrevented).toBe(false);
        });
    });

    describe('by dragging a handle', () => {
        it('marks the item being dragged and the list around it', () => {
            attachList({ owner });

            press(handleOf(0), rowOf(0).middle);
            move(rowOf(2).middle);

            expect(marked(DRAGGING)).toBe(0);
            expect(list.classList.contains(ACTIVE)).toBe(true);
        });

        it('shows the drop landing after the item the pointer has passed the middle of', () => {
            attachList({ owner });

            press(handleOf(0), rowOf(0).middle);
            move(rowOf(2).middle + 10);

            expect(marked(AFTER)).toBe(2);
        });

        it('shows the drop landing before the item the pointer is above the middle of', () => {
            attachList({ owner });

            press(handleOf(0), rowOf(0).middle);
            move(rowOf(2).middle - 10);

            expect(marked(BEFORE)).toBe(2);
        });

        it('clears the previous landing as the pointer crosses on', () => {
            attachList({ owner });

            press(handleOf(0), rowOf(0).middle);
            move(rowOf(1).middle - 10);
            move(rowOf(2).middle + 10);

            expect(marked(BEFORE)).toBe(-1);
            expect(marked(AFTER)).toBe(2);
        });

        it('moves the landing to the other side of an item as the pointer crosses its middle', () => {
            attachList({ owner });
            press(handleOf(0), rowOf(0).middle);

            move(rowOf(1).top + 5);
            expect(marked(BEFORE)).toBe(1);

            move(rowOf(1).middle + 10);

            expect(marked(AFTER)).toBe(1);
            expect(marked(BEFORE)).toBe(-1);
        });

        it('shows no landing over the item being dragged', () => {
            attachList({ owner });

            press(handleOf(0), rowOf(0).middle);
            move(rowOf(0).middle - 6);

            expect(marked(BEFORE)).toBe(-1);
            expect(marked(AFTER)).toBe(-1);
        });

        it('reports where the item landed', () => {
            attachList({ owner });

            press(handleOf(0), rowOf(0).middle);
            move(rowOf(2).middle + 10);
            lift(rowOf(2).middle + 10);

            reordered().toHaveBeenCalledExactlyOnceWith('HandleReorderAsync', 0, 2, true);
        });

        it('sends an item released below the list to the end', () => {
            attachList({ owner });

            press(handleOf(0), rowOf(0).middle);
            move(600);
            lift(600);

            reordered().toHaveBeenCalledExactlyOnceWith('HandleReorderAsync', 0, 3, true);
        });

        it('reports nothing when an item is released on itself', () => {
            attachList({ owner });

            press(handleOf(0), rowOf(0).middle);
            move(rowOf(0).middle - 6);
            lift(rowOf(0).middle - 6);

            reordered().not.toHaveBeenCalled();
        });

        it('clears every mark once the item lands', () => {
            attachList({ owner });

            press(handleOf(0), rowOf(0).middle);
            move(rowOf(2).middle + 10);
            lift(rowOf(2).middle + 10);

            expect([marked(DRAGGING), marked(BEFORE), marked(AFTER)]).toEqual([-1, -1, -1]);
            expect(list.classList.contains(ACTIVE)).toBe(false);
        });

        it('abandons the move on Escape', () => {
            attachList({ owner });
            press(handleOf(0), rowOf(0).middle);
            move(rowOf(2).middle + 10);

            key(list, 'Escape');

            reordered().not.toHaveBeenCalled();
            expect([marked(DRAGGING), marked(AFTER)]).toEqual([-1, -1]);
        });

        it('refuses to drag from a disabled handle', () => {
            handleOf(0).disabled = true;
            attachList({ owner });

            press(handleOf(0), rowOf(0).middle);
            move(rowOf(2).middle);

            expect(marked(DRAGGING)).toBe(-1);
        });

        it('stops dragging and keying once disposed', () => {
            const handle = attachList({ owner });

            handle.dispose();
            press(handleOf(0), rowOf(0).middle);
            move(rowOf(2).middle + 10);
            lift(rowOf(2).middle + 10);
            key(handleOf(0), 'ArrowDown');

            reordered().not.toHaveBeenCalled();
            expect(marked(DRAGGING)).toBe(-1);
        });
    });

    describe('by dragging the item itself', () => {
        it('ignores a press on the item body by default', () => {
            attachList({ owner });

            press(labelOf(0), rowOf(0).middle);
            move(rowOf(2).middle + 10);

            expect(marked(DRAGGING)).toBe(-1);
        });

        it('drags from the item body when the list allows it', () => {
            attachList({ owner, dragFromItem: true });

            press(labelOf(0), rowOf(0).middle);
            move(rowOf(2).middle + 10);
            lift(rowOf(2).middle + 10);

            reordered().toHaveBeenCalledExactlyOnceWith('HandleReorderAsync', 0, 2, true);
        });

        it('leaves a press on a link inside the item to that link', () => {
            attachList({ owner, dragFromItem: true });

            press(items()[3].querySelector('a'), rowOf(3).middle);
            move(rowOf(0).middle);

            expect(marked(DRAGGING)).toBe(-1);
        });

        it('refuses to drag an item whose handle is disabled', () => {
            handleOf(0).disabled = true;
            attachList({ owner, dragFromItem: true });

            press(labelOf(0), rowOf(0).middle);
            move(rowOf(2).middle);

            expect(marked(DRAGGING)).toBe(-1);
        });
    });

    describe('by dragging the item itself under a finger', () => {
        beforeEach(() => vi.useFakeTimers());

        it('leaves an ordinary swipe to scroll the list', () => {
            attachList({ owner, dragFromItem: true });

            press(labelOf(0), rowOf(0).middle, 'touch');
            move(rowOf(2).middle, 'touch');
            vi.advanceTimersByTime(400);
            lift(rowOf(2).middle, 'touch');

            reordered().not.toHaveBeenCalled();
            expect(marked(DRAGGING)).toBe(-1);
        });

        it('drags once the finger has rested on the item', () => {
            attachList({ owner, dragFromItem: true });

            press(labelOf(0), rowOf(0).middle, 'touch');
            vi.advanceTimersByTime(400);
            move(rowOf(2).middle + 10, 'touch');
            lift(rowOf(2).middle + 10, 'touch');

            reordered().toHaveBeenCalledExactlyOnceWith('HandleReorderAsync', 0, 2, true);
        });

        // A handle carries touch-action: none, so a touch on it drags at once.
        it('drags from a handle without waiting', () => {
            attachList({ owner });

            press(handleOf(0), rowOf(0).middle, 'touch');
            move(rowOf(2).middle + 10, 'touch');
            lift(rowOf(2).middle + 10, 'touch');

            reordered().toHaveBeenCalledExactlyOnceWith('HandleReorderAsync', 0, 2, true);
        });
    });
});

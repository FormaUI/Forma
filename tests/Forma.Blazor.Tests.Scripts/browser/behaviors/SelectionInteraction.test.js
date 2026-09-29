import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { attach } from '#browser/behaviors/SelectionInteraction.js';
import { configure, register, resolve } from '#input/InputRouter.js';
import { fakeOwner } from '#support/owner.js';

describe('SelectionInteraction', () => {
    /** @type {HTMLTableElement} */
    let grid;
    /** @type {ReturnType<typeof fakeOwner>} */
    let owner;

    const row = (key, trailing) =>
        `<tr><td><span data-row-key="${key}" tabindex="-1">Row</span></td><td>${trailing}</td></tr>`;

    // The router keeps every registration until it is disposed, so each test's are released after it.
    const handles = [];
    const attachSelection = (target, options) => {
        const handle = attach(target, options);
        handles.push(handle);

        return handle;
    };

    beforeEach(() => {
        document.body.innerHTML = `
            <table id="grid"><tbody>
                ${row(1, '<input type="checkbox" />')}
                ${row(2, '<a href="#go">Go</a>')}
                ${row(3, '<button>Edit</button>')}
                ${row(4, '<input type="text" />')}
            </tbody></table>`;
        grid = document.querySelector('#grid');
        owner = fakeOwner();
    });

    afterEach(() => {
        handles.splice(0).forEach(handle => handle.dispose());
        configure({ bindings: {} });
    });

    const cellOf = key => document.querySelector(`[data-row-key="${key}"]`);
    const selected = () => expect(owner.invokeMethodAsync);

    // A press from the pointer reaches the router as a press and then a click that follows it.
    const click = (element, modifiers = {}) => {
        element.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true, cancelable: true, button: 0, pointerId: 1, ...modifiers }));
        element.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true, detail: 1, ...modifiers }));
    };

    const doubleClick = element => {
        click(element);
        click(element);
        element.dispatchEvent(new MouseEvent('dblclick', { bubbles: true, cancelable: true, detail: 2 }));
    };

    const press = (element, key, modifiers = {}) => {
        const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true, ...modifiers });
        element.dispatchEvent(event);

        return event;
    };

    describe('with the pointer', () => {
        it('selects the clicked item', () => {
            attachSelection(grid, { owner });

            click(cellOf(2));

            selected().toHaveBeenLastCalledWith('HandleSelectAsync', 2, false);
        });

        it('lands focus on the clicked item, so the keys carry on from it', () => {
            attachSelection(grid, { owner });

            click(cellOf(2));
            press(cellOf(2), 'ArrowDown');

            expect(document.activeElement).toBe(cellOf(3));
            selected().toHaveBeenLastCalledWith('HandleSelectAsync', 3, false);
        });

        it('toggles rather than replaces when control is held', () => {
            attachSelection(grid, { owner });

            click(cellOf(2), { ctrlKey: true });

            selected().toHaveBeenLastCalledWith('HandleSelectAsync', 2, true);
        });

        it('extends from the last plain click to the shift-clicked item', () => {
            attachSelection(grid, { owner });

            click(cellOf(2));
            click(cellOf(4), { shiftKey: true });

            selected().toHaveBeenLastCalledWith('HandleRangeSelectAsync', [2, 3, 4], false);
        });

        it('extends backwards in the order the items are rendered', () => {
            attachSelection(grid, { owner });

            click(cellOf(3));
            click(cellOf(1), { shiftKey: true });

            selected().toHaveBeenLastCalledWith('HandleRangeSelectAsync', [1, 2, 3], false);
        });

        it('selects rather than extends when nothing anchored the range', () => {
            attachSelection(grid, { owner });

            click(cellOf(3), { shiftKey: true });

            selected().toHaveBeenLastCalledWith('HandleSelectAsync', 3, false);
        });

        // A shift-click would otherwise extend the page's own text selection across the rows.
        it('stops a shift-click selecting the text it crosses', () => {
            attachSelection(grid, { owner });
            click(cellOf(1));

            const event = new PointerEvent('pointerdown', { bubbles: true, cancelable: true, button: 0, pointerId: 1, shiftKey: true });
            cellOf(3).dispatchEvent(event);

            expect(event.defaultPrevented).toBe(true);
        });

        it.each`
            element         | selector
            ${'a checkbox'} | ${'input[type="checkbox"]'}
            ${'a link'}     | ${'a[href]'}
            ${'a button'}   | ${'button'}
        `('leaves a click on $element to that element', ({ selector }) => {
            attachSelection(grid, { owner });

            click(grid.querySelector(selector));

            selected().not.toHaveBeenCalled();
        });

        it('leaves the default of the click that selects alone', () => {
            attachSelection(grid, { owner });

            const event = new MouseEvent('click', { bubbles: true, cancelable: true, detail: 1 });
            cellOf(2).dispatchEvent(event);

            selected().toHaveBeenLastCalledWith('HandleSelectAsync', 2, false);
            expect(event.defaultPrevented).toBe(false);
        });

        it('ignores a click outside any item', () => {
            attachSelection(grid, { owner });

            click(grid);

            selected().not.toHaveBeenCalled();
        });
    });

    describe('with the keyboard', () => {
        it('moves to the next item on ArrowDown', () => {
            attachSelection(grid, { owner });
            cellOf(1).focus();

            press(cellOf(1), 'ArrowDown');

            selected().toHaveBeenLastCalledWith('HandleSelectAsync', 2, false);
        });

        it('moves to the previous item on ArrowUp', () => {
            attachSelection(grid, { owner });
            cellOf(3).focus();

            press(cellOf(3), 'ArrowUp');

            selected().toHaveBeenLastCalledWith('HandleSelectAsync', 2, false);
        });

        it('jumps to the first item on Home', () => {
            attachSelection(grid, { owner });
            cellOf(3).focus();

            press(cellOf(3), 'Home');

            selected().toHaveBeenLastCalledWith('HandleSelectAsync', 1, false);
        });

        it('jumps to the last item on End', () => {
            attachSelection(grid, { owner });
            cellOf(1).focus();

            press(cellOf(1), 'End');

            selected().toHaveBeenLastCalledWith('HandleSelectAsync', 4, false);
        });

        it('starts from the first item when nothing is focused', () => {
            attachSelection(grid, { owner });

            press(grid, 'ArrowDown');

            selected().toHaveBeenLastCalledWith('HandleSelectAsync', 1, false);
        });

        it('stays put at the end of the list', () => {
            attachSelection(grid, { owner });
            cellOf(4).focus();

            const event = press(cellOf(4), 'ArrowDown');

            selected().not.toHaveBeenCalled();
            expect(event.defaultPrevented).toBe(false);
        });

        it('moves focus to the item it selected', () => {
            attachSelection(grid, { owner });
            cellOf(1).focus();

            press(cellOf(1), 'ArrowDown');

            expect(document.activeElement).toBe(cellOf(2));
        });

        it('leaves the tab stop on the focused item alone', () => {
            attachSelection(grid, { owner });
            cellOf(1).focus();

            press(cellOf(1), 'ArrowDown');

            expect([...grid.querySelectorAll('[data-row-key]')].map(cell => cell.tabIndex)).toEqual([-1, 0, -1, -1]);
        });

        it('moves without selecting when control is held', () => {
            attachSelection(grid, { owner });
            cellOf(1).focus();

            press(cellOf(1), 'ArrowDown');
            owner.invokeMethodAsync.mockClear();
            press(cellOf(2), 'ArrowDown', { ctrlKey: true });

            expect(document.activeElement).toBe(cellOf(3));
            selected().not.toHaveBeenCalled();
        });

        it('extends the range when shift is held', () => {
            attachSelection(grid, { owner });
            click(cellOf(1));
            cellOf(1).focus();

            press(cellOf(1), 'ArrowDown', { shiftKey: true });

            selected().toHaveBeenLastCalledWith('HandleRangeSelectAsync', [1, 2], false);
        });

        // The caret keys are the field's by the router's native table; a vertical arrow, which a single-line field
        // does not consume, is the list's.
        it('leaves the caret keys to a text field inside an item', () => {
            attachSelection(grid, { owner });
            const field = grid.querySelector('input[type="text"]');

            const home = press(field, 'Home');
            const left = press(field, 'ArrowLeft');

            selected().not.toHaveBeenCalled();
            expect(home.defaultPrevented).toBe(false);
            expect(left.defaultPrevented).toBe(false);
        });

        it('moves between items on a vertical arrow pressed in a text field inside an item', () => {
            attachSelection(grid, { owner });
            cellOf(4).focus();
            const field = grid.querySelector('input[type="text"]');

            press(field, 'ArrowUp');

            selected().toHaveBeenLastCalledWith('HandleSelectAsync', 3, false);
        });

        it('declares the keys as routes on the list', () => {
            attachSelection(grid, { owner });
            cellOf(1).focus();

            const event = press(cellOf(1), 'ArrowDown');

            expect(resolve(event)).toMatchObject({ owner: grid, action: 'selection.next', precedence: 'structural' });
        });

        it('leaves a key pressed in a control of the list outside its items alone', () => {
            attachSelection(grid, { owner });
            grid.insertAdjacentHTML('afterbegin', '<thead><tr><th><button id="sort">Name</button></th></tr></thead>');
            document.querySelector('#sort').focus();

            const event = press(document.querySelector('#sort'), 'ArrowDown');

            selected().not.toHaveBeenCalled();
            expect(event.defaultPrevented).toBe(false);
        });

        it('leaves a key inside a nested scope that is open to that scope', () => {
            attachSelection(grid, { owner });
            cellOf(2).insertAdjacentHTML('afterend', '<div id="popup" popover="manual"><button id="option">Option</button></div>');
            const popup = register(document.querySelector('#popup'), {});
            handles.push(popup);
            popup.activate();
            document.querySelector('#option').focus();

            const event = press(document.querySelector('#option'), 'ArrowDown');

            selected().not.toHaveBeenCalled();
            expect(event.defaultPrevented).toBe(false);
        });

        it('takes the host\'s remap of a move', () => {
            configure({ bindings: { 'selection.next': [{ type: 'keydown', key: 'j' }] } });
            attachSelection(grid, { owner });
            cellOf(1).focus();

            press(cellOf(1), 'j');
            const unbound = press(cellOf(2), 'ArrowDown');

            selected().toHaveBeenLastCalledWith('HandleSelectAsync', 2, false);
            expect(unbound.defaultPrevented).toBe(false);
        });

        it('moves nothing from a remapped key at the end of the list', () => {
            configure({ bindings: { 'selection.next': [{ type: 'keydown', key: 'j' }] } });
            attachSelection(grid, { owner });
            cellOf(4).focus();

            expect(() => press(cellOf(4), 'j')).not.toThrow();

            selected().not.toHaveBeenCalled();
        });
    });

    describe('across a packed layout', () => {
        // Six cards packed three across, each a keyed marker inside a card the item selector names.
        const card = key => `<div class="card"><span data-row-key="${key}" tabindex="-1">Card</span></div>`;
        const pack = (columns, { width = 100, height = 100 } = {}) => {
            [...document.querySelectorAll('.card')].forEach((element, index) => {
                const left = (index % columns) * width;
                const top = Math.floor(index / columns) * height;
                element.getBoundingClientRect = () => ({ top, bottom: top + height, left, right: left + width, width, height, x: left, y: top });
            });
        };

        beforeEach(() => {
            document.body.innerHTML = `<div id="cards">${[1, 2, 3, 4, 5, 6].map(card).join('')}</div>`;
            grid = document.querySelector('#cards');
            pack(3);
        });

        it('moves to the card below on ArrowDown', () => {
            attachSelection(grid, { owner, itemSelector: '.card' });
            cellOf(2).focus();

            press(cellOf(2), 'ArrowDown');

            selected().toHaveBeenLastCalledWith('HandleSelectAsync', 5, false);
        });

        it('moves to the card above on ArrowUp', () => {
            attachSelection(grid, { owner, itemSelector: '.card' });
            cellOf(6).focus();

            press(cellOf(6), 'ArrowUp');

            selected().toHaveBeenLastCalledWith('HandleSelectAsync', 3, false);
        });

        it('moves along the row on ArrowRight and ArrowLeft', () => {
            attachSelection(grid, { owner, itemSelector: '.card' });
            cellOf(1).focus();

            press(cellOf(1), 'ArrowRight');
            selected().toHaveBeenLastCalledWith('HandleSelectAsync', 2, false);

            press(cellOf(2), 'ArrowLeft');
            selected().toHaveBeenLastCalledWith('HandleSelectAsync', 1, false);
        });

        it('stays put at the end of a row rather than wrapping', () => {
            attachSelection(grid, { owner, itemSelector: '.card' });
            cellOf(3).focus();

            const event = press(cellOf(3), 'ArrowRight');

            selected().not.toHaveBeenCalled();
            expect(event.defaultPrevented).toBe(false);
        });

        it('reaches the ends of the row on Home and End', () => {
            attachSelection(grid, { owner, itemSelector: '.card' });
            cellOf(5).focus();

            press(cellOf(5), 'Home');
            selected().toHaveBeenLastCalledWith('HandleSelectAsync', 4, false);

            press(cellOf(4), 'End');
            selected().toHaveBeenLastCalledWith('HandleSelectAsync', 6, false);
        });

        it("reads the ends of a row by the list's own direction", () => {
            grid.style.direction = 'rtl';
            attachSelection(grid, { owner, itemSelector: '.card' });
            cellOf(5).focus();

            press(cellOf(5), 'Home');
            selected().toHaveBeenLastCalledWith('HandleSelectAsync', 6, false);

            press(cellOf(6), 'End');
            selected().toHaveBeenLastCalledWith('HandleSelectAsync', 4, false);
        });

        it('leaves a sideways arrow alone in a single column', () => {
            pack(1);
            attachSelection(grid, { owner, itemSelector: '.card' });
            cellOf(2).focus();

            const event = press(cellOf(2), 'ArrowRight');

            selected().not.toHaveBeenCalled();
            expect(event.defaultPrevented).toBe(false);
        });

        it('reaches the ends of the whole list on Ctrl+Home and Ctrl+End, moving the focus alone', () => {
            attachSelection(grid, { owner, itemSelector: '.card' });
            cellOf(5).focus();

            press(cellOf(5), 'Home', { ctrlKey: true });
            expect(document.activeElement).toBe(cellOf(1));

            press(cellOf(1), 'End', { ctrlKey: true });
            expect(document.activeElement).toBe(cellOf(6));
            selected().not.toHaveBeenCalled();
        });

        it('extends the range to the end of the whole list on Ctrl+Shift+End', () => {
            attachSelection(grid, { owner, itemSelector: '.card' });
            click(cellOf(2));

            press(cellOf(2), 'End', { ctrlKey: true, shiftKey: true });

            selected().toHaveBeenLastCalledWith('HandleRangeSelectAsync', [2, 3, 4, 5, 6], false);
        });

        it('moves the focus alone with Ctrl and Shift on an arrow, which reaches no end', () => {
            attachSelection(grid, { owner, itemSelector: '.card' });
            click(cellOf(1));

            press(cellOf(1), 'ArrowRight', { ctrlKey: true, shiftKey: true });

            expect(document.activeElement).toBe(cellOf(2));
            selected().toHaveBeenLastCalledWith('HandleSelectAsync', 1, false);
        });
    });

    describe('over an owner that seeks', () => {
        // Four items drawn out of a longer order, each carrying its position in it.
        const item = (key, position) => `<div class="item" data-item-position="${position}"><span data-row-key="${key}" tabindex="-1">Item</span></div>`;
        const options = () => ({ owner, itemSelector: '.item', positionAttribute: 'data-item-position', seeks: true });
        const settle = () => new Promise(resolve => setTimeout(resolve));

        beforeEach(() => {
            document.body.innerHTML = `<div id="list">${[[1, 10], [2, 11], [3, 12], [4, 13]].map(([key, position]) => item(key, position)).join('')}</div>`;
            grid = document.querySelector('#list');
        });

        it('asks the owner for the last item on End, to select it', () => {
            attachSelection(grid, options());
            cellOf(2).focus();

            press(cellOf(2), 'End');

            selected().toHaveBeenLastCalledWith('HandleSeekAsync', 1, true, 'select', null);
        });

        it('asks for the first item on Ctrl+Home, to move the focus alone', () => {
            attachSelection(grid, options());
            cellOf(2).focus();

            press(cellOf(2), 'Home', { ctrlKey: true });

            selected().toHaveBeenLastCalledWith('HandleSeekAsync', 1, false, 'focus', null);
        });

        it('asks for the last item on Shift+End, to extend from the anchor by its position', () => {
            attachSelection(grid, options());
            click(cellOf(2));

            press(cellOf(2), 'End', { shiftKey: true });

            selected().toHaveBeenLastCalledWith('HandleSeekAsync', 1, true, 'extend', 11);
        });

        it('joins a second End pressed while the first is still asked for', () => {
            attachSelection(grid, options());
            cellOf(2).focus();

            press(cellOf(2), 'End');
            press(cellOf(2), 'End');

            expect(owner.invokeMethodAsync.mock.calls.filter(([method]) => method === 'HandleSeekAsync')).toHaveLength(1);
        });

        it('asks again when the same end is pressed to select otherwise', () => {
            attachSelection(grid, options());
            click(cellOf(2));

            press(cellOf(2), 'End');
            press(cellOf(2), 'End', { shiftKey: true });

            expect(owner.invokeMethodAsync.mock.calls.filter(([method]) => method === 'HandleSeekAsync')).toEqual([
                ['HandleSeekAsync', 1, true, 'select', null],
                ['HandleSeekAsync', 2, true, 'extend', 11],
            ]);
        });

        it('asks again once the owner let a request go', () => {
            const handle = attachSelection(grid, options());
            cellOf(2).focus();

            press(cellOf(2), 'End');
            handle.cancelSeek();
            press(cellOf(2), 'End');

            expect(owner.invokeMethodAsync.mock.calls.filter(([method]) => method === 'HandleSeekAsync')).toHaveLength(2);
        });

        it('lands the focus on the item once the owner names it and it is drawn', async () => {
            const handle = attachSelection(grid, options());
            cellOf(2).focus();
            press(cellOf(2), 'End');

            handle.focusItem(1, 99);
            grid.insertAdjacentHTML('beforeend', item(9, 99));
            await settle();

            expect(document.activeElement).toBe(cellOf(9));
        });

        it('extends from the item it landed on, which a plain End anchors', async () => {
            const handle = attachSelection(grid, options());
            cellOf(2).focus();
            press(cellOf(2), 'End');
            grid.insertAdjacentHTML('beforeend', item(9, 99));
            handle.focusItem(1, 99);

            press(cellOf(9), 'ArrowUp', { shiftKey: true });

            selected().toHaveBeenLastCalledWith('HandleRangeSelectByPositionAsync', 99, 13, false);
            await settle();
        });

        it('lets go of an item asked for once the reader moves on', async () => {
            const handle = attachSelection(grid, options());
            cellOf(2).focus();
            press(cellOf(2), 'End');

            press(cellOf(2), 'ArrowDown');
            handle.focusItem(1, 99);
            grid.insertAdjacentHTML('beforeend', item(9, 99));
            await settle();

            expect(document.activeElement).toBe(cellOf(3));
        });

        it('lets go of an item still waiting to be drawn when the owner cancels it', async () => {
            const handle = attachSelection(grid, options());
            cellOf(2).focus();
            press(cellOf(2), 'End');
            handle.focusItem(1, 99);

            handle.cancelSeek();
            grid.insertAdjacentHTML('beforeend', item(9, 99));
            await settle();

            expect(document.activeElement).toBe(cellOf(2));
        });

        // A render drawing the focused item afresh: its element leaves, a new one takes its place, and the focus falls to nothing.
        const redraw = key => {
            const old = cellOf(key).closest('.item');
            const position = old.getAttribute('data-item-position');
            old.insertAdjacentHTML('afterend', item(key, position));
            old.querySelector('[data-row-key]').dispatchEvent(new FocusEvent('focusout', { bubbles: true, relatedTarget: null }));
            old.remove();
            document.body.focus();
        };
        const nextFrame = () => new Promise(resolve => requestAnimationFrame(() => resolve()));

        it('takes the focus back onto an item a render drew afresh', async () => {
            attachSelection(grid, options());
            cellOf(2).focus();

            redraw(2);
            await nextFrame();

            expect(document.activeElement).toBe(cellOf(2));
        });

        it('leaves the focus where a press sent it', async () => {
            attachSelection(grid, options());
            cellOf(2).focus();

            document.body.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true, button: 0 }));
            redraw(2);
            await nextFrame();

            expect(document.activeElement).not.toBe(cellOf(2));
        });

        it('extends a shift-click by position, so an anchor scrolled out of the page still counts', () => {
            attachSelection(grid, options());
            click(cellOf(1));
            cellOf(1).closest('.item').remove();

            click(cellOf(4), { shiftKey: true });

            selected().toHaveBeenLastCalledWith('HandleRangeSelectByPositionAsync', 10, 13, false);
        });
    });

    describe('activating', () => {
        it('activates a pressed item where the list does not select', () => {
            attachSelection(grid, { owner, selects: false });

            click(cellOf(2));

            selected().toHaveBeenLastCalledWith('HandleActivateAsync', 2);
        });

        it('activates on a double press where the list selects, the presses before it selecting', () => {
            attachSelection(grid, { owner });

            doubleClick(cellOf(3));

            selected().toHaveBeenNthCalledWith(1, 'HandleSelectAsync', 3, false);
            selected().toHaveBeenLastCalledWith('HandleActivateAsync', 3);
        });

        it('reads the token an item carries itself, for an item with no marker', () => {
            document.body.innerHTML = '<ul id="items"><li data-item="7"><span id="seven">Seven</span></li></ul>';
            attachSelection(document.querySelector('#items'), { owner, itemSelector: '[data-item]', itemKeyAttribute: 'data-item', selects: false });

            click(document.querySelector('#seven'));

            selected().toHaveBeenLastCalledWith('HandleActivateAsync', 7);
        });

        it('leaves a press on the item\'s own controls, and on a native control inside it, activating nothing', () => {
            document.body.innerHTML = '<ul id="items"><li data-item="7"><span data-item-control><span id="gutter">Box</span></span><button id="action">Act</button><span id="name">Seven</span></li></ul>';
            attachSelection(document.querySelector('#items'), { owner, itemSelector: '[data-item]', itemKeyAttribute: 'data-item', controlSelector: '[data-item-control]', selects: false });

            click(document.querySelector('#gutter'));
            click(document.querySelector('#action'));

            selected().not.toHaveBeenCalled();
        });

        it('activates the item whose marker holds the focus on Enter', () => {
            attachSelection(grid, { owner });
            cellOf(2).focus();

            const event = press(cellOf(2), 'Enter');

            selected().toHaveBeenLastCalledWith('HandleActivateAsync', 2);
            expect(event.defaultPrevented).toBe(true);
        });

        it('leaves Enter in a control inside an item to the control', () => {
            attachSelection(grid, { owner });
            const field = grid.querySelector('input[type="text"]');
            field.focus();

            const event = press(field, 'Enter');

            selected().not.toHaveBeenCalled();
            expect(event.defaultPrevented).toBe(false);
        });

        it('activates rather than selects where the list does not select, leaving the arrows alone', () => {
            attachSelection(grid, { owner, selects: false });
            cellOf(1).focus();

            click(cellOf(2));
            const moved = press(cellOf(1), 'ArrowDown');
            press(cellOf(1), 'Enter');

            expect(moved.defaultPrevented).toBe(false);
            expect(owner.invokeMethodAsync.mock.calls).toEqual([['HandleActivateAsync', 2], ['HandleActivateAsync', 1]]);
        });
    });

    it('stops responding once disposed', () => {
        const handle = attachSelection(grid, { owner });

        handle.dispose();
        click(cellOf(2));
        press(grid, 'ArrowDown');

        selected().not.toHaveBeenCalled();
    });
});

import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { attach } from '#dataview/SelectionInteraction.js';
import { configure, register, resolve } from '#input/InputRouter.js';
import { fakeOwner } from '#support/owner.js';

describe('SelectionInteraction', () => {
    /** @type {HTMLTableElement} */
    let grid;
    /** @type {ReturnType<typeof fakeOwner>} */
    let owner;

    const row = (key, trailing) =>
        `<tr><td><span data-row-key="${key}" tabindex="-1">Row</span></td><td>${trailing}</td></tr>`;

    // The handles attached in a test, whose registrations the router holds until they are disposed after it.
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

    // A pointer press followed by the click it produces.
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

            selected().toHaveBeenLastCalledWith('HandleRangeSelectAsync', { value: [2, 3, 4] }, false);
        });

        it('carries a range past the budget of the transfer as a stream of its tokens', () => {
            globalThis.DotNet = { createJSStreamReference: bytes => ({ bytes }) };

            try {
                const count = 6000;
                grid.querySelector('tbody').innerHTML = Array.from({ length: count }, (_, index) => row(index + 1, '')).join('');
                attachSelection(grid, { owner });

                click(cellOf(1));
                click(cellOf(count), { shiftKey: true });

                const [method, transfer, additive] = owner.invokeMethodAsync.mock.lastCall;
                expect(method).toBe('HandleRangeSelectAsync');
                expect(additive).toBe(false);
                expect(JSON.parse(new TextDecoder().decode(transfer.stream.bytes))).toEqual(Array.from({ length: count }, (_, index) => index + 1));
            } finally {
                delete globalThis.DotNet;
            }
        });

        it('extends backwards from the anchor to the item reached', () => {
            attachSelection(grid, { owner });

            click(cellOf(3));
            click(cellOf(1), { shiftKey: true });

            selected().toHaveBeenLastCalledWith('HandleRangeSelectAsync', { value: [3, 2, 1] }, false);
        });

        it('selects rather than extends when nothing anchored the range', () => {
            attachSelection(grid, { owner });

            click(cellOf(3), { shiftKey: true });

            selected().toHaveBeenLastCalledWith('HandleSelectAsync', 3, false);
        });

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

            selected().toHaveBeenLastCalledWith('HandleRangeSelectAsync', { value: [1, 2] }, false);
        });

        // The router's native table gives the caret keys to the field and a vertical arrow to the list.
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

    describe('among boxes the reader checks', () => {
        const options = () => ({ owner, selects: false, moves: true });

        it.each`
            key            | from | to
            ${'ArrowDown'} | ${1} | ${2}
            ${'ArrowUp'}   | ${3} | ${2}
            ${'Home'}      | ${3} | ${1}
            ${'End'}       | ${1} | ${4}
        `('moves the focus alone on $key', ({ key, from, to }) => {
            attachSelection(grid, options());
            cellOf(from).focus();

            const event = press(cellOf(from), key);

            expect(document.activeElement).toBe(cellOf(to));
            expect(event.defaultPrevented).toBe(true);
            selected().not.toHaveBeenCalled();
        });

        it('moves the focus alone whatever modifier is held', () => {
            attachSelection(grid, options());
            cellOf(1).focus();

            press(cellOf(1), 'ArrowDown', { shiftKey: true });
            press(cellOf(2), 'ArrowDown', { ctrlKey: true });

            expect(document.activeElement).toBe(cellOf(3));
            selected().not.toHaveBeenCalled();
        });

        it('leaves Space to the box', () => {
            attachSelection(grid, options());
            cellOf(2).focus();

            const event = press(cellOf(2), ' ');

            expect(event.defaultPrevented).toBe(false);
            selected().not.toHaveBeenCalled();
        });

        it('asks an owner that seeks for the last item, to move the focus alone', () => {
            document.body.innerHTML = `<div id="list">${[[1, 10], [2, 11]].map(([key, position]) => `<div class="item" data-item-position="${position}"><span data-row-key="${key}" tabindex="-1">Item</span></div>`).join('')}</div>`;
            grid = document.querySelector('#list');
            attachSelection(grid, { ...options(), itemSelector: '.item', positionAttribute: 'data-item-position', seeks: true });
            cellOf(1).focus();

            press(cellOf(1), 'End', { shiftKey: true });

            selected().toHaveBeenLastCalledWith('HandleSeekAsync', 1, true, 'focus', null);
        });
    });

    describe('as one tab stop', () => {
        const tabIndexes = () => [...grid.querySelectorAll('[data-row-key]')].map(marker => marker.tabIndex);
        const settle = () => new Promise(resolve => setTimeout(resolve));

        it('makes the first marker the tab stop once attached', () => {
            attachSelection(grid, { owner, selects: false, moves: true });

            expect(tabIndexes()).toEqual([0, -1, -1, -1]);
        });

        it('makes a marker taking the focus the tab stop', () => {
            attachSelection(grid, { owner });

            cellOf(3).focus();

            expect(tabIndexes()).toEqual([-1, -1, 0, -1]);
        });

        it('draws a marker a render adds out of the tab order', async () => {
            attachSelection(grid, { owner });
            cellOf(2).focus();

            grid.querySelector('tbody').insertAdjacentHTML('beforeend', '<tr><td><span data-row-key="5">Row</span></td></tr>');
            await settle();

            expect(tabIndexes()).toEqual([-1, 0, -1, -1, -1]);
        });

        it('hands the tab stop to the first marker once the focused one is gone', async () => {
            attachSelection(grid, { owner });
            cellOf(3).focus();

            cellOf(3).closest('tr').remove();
            await settle();

            expect(tabIndexes()).toEqual([0, -1, -1]);
        });

        it('leaves the tab order alone where the keys move nothing', () => {
            attachSelection(grid, { owner, selects: false });

            cellOf(3).focus();

            expect(tabIndexes()).toEqual([-1, -1, -1, -1]);
        });
    });

    describe('gathering its items\' controls', () => {
        const settle = () => new Promise(resolve => setTimeout(resolve));
        const item = key => `<tr data-item-root><td><span data-row-key="${key}" tabindex="-1">Row</span></td><td><button class="open">Open</button><button class="more" data-item-actions>More</button><input type="text" class="field" /></td></tr>`;
        const controlsOf = key => [...cellOf(key).closest('tr').querySelectorAll('.open, .more, .field')];
        const reachable = key => controlsOf(key).map(control => control.tabIndex >= 0);
        const gather = (options = {}) => attachSelection(grid, { owner, itemRootSelector: '[data-item-root]', actionsSelector: '[data-item-actions]', gathersControls: true, ...options });

        beforeEach(() => {
            grid.querySelector('tbody').innerHTML = [1, 2, 3, 4].map(item).join('');
        });

        it('takes every item\'s controls but the tab stop\'s out of the Tab sequence', () => {
            gather();

            expect([1, 2, 3, 4].map(key => reachable(key).every(Boolean))).toEqual([true, false, false, false]);
            expect(cellOf(2).tabIndex).toBe(-1);
        });

        it('hands the controls to the item the focus moves to', () => {
            gather();

            cellOf(3).focus();

            expect([1, 2, 3, 4].map(key => reachable(key).every(Boolean))).toEqual([false, false, true, false]);
        });

        it('makes the item whose control takes the focus the one, its marker the tab stop', () => {
            gather();

            controlsOf(2)[0].focus();

            expect([1, 2, 3, 4].map(key => reachable(key).every(Boolean))).toEqual([false, true, false, false]);
            expect(cellOf(2).tabIndex).toBe(0);
        });

        it('takes the controls of an item a render adds out of the Tab sequence', async () => {
            gather();

            const drawn = document.createElement('template');
            drawn.innerHTML = `<table><tbody>${item(5)}</tbody></table>`;
            grid.querySelector('tbody').append(drawn.content.querySelector('tr'));
            await settle();

            expect(reachable(5)).toEqual([false, false, false]);
        });

        it('takes a control another item enables out of the Tab sequence', async () => {
            controlsOf(3)[0].disabled = true;
            gather();

            controlsOf(3)[0].disabled = false;
            await settle();

            expect(controlsOf(3)[0].tabIndex).toBe(-1);
        });

        it('gives each control back the tab index it had once let go', () => {
            controlsOf(2)[0].setAttribute('tabindex', '2');
            const handle = gather();

            handle.dispose();

            expect(controlsOf(2)[0].getAttribute('tabindex')).toBe('2');
            expect(controlsOf(3)[0].hasAttribute('tabindex')).toBe(false);
        });

        it('keeps the controls of the item each collection\'s tab stop holds in the Tab sequence, a tree\'s row beside a list\'s marker', () => {
            const container = document.createElement('div');
            const tree = document.createElement('div');
            tree.innerHTML = '<div data-item-root tabindex="-1"><button class="node-more">More</button></div><div data-item-root tabindex="0"><button class="node-more">More</button></div>';
            grid.before(container);
            container.append(tree, grid);
            attachSelection(container, { owner, itemRootSelector: '[data-item-root]', gathersControls: true });

            expect([...tree.querySelectorAll('.node-more')].map(control => control.tabIndex)).toEqual([-1, 0]);
            expect([1, 2, 3, 4].map(key => reachable(key).every(Boolean))).toEqual([true, false, false, false]);
        });

        it('leaves every item\'s controls in the Tab sequence while no item holds the tab stop', () => {
            gather({ selects: false });

            expect([1, 2, 3, 4].map(key => reachable(key).every(Boolean))).toEqual([true, true, true, true]);
        });

        it('presses the opener of an item\'s actions on Shift+F10 and the Menu key on its marker', () => {
            gather();
            const opener = controlsOf(1)[1];
            let presses = 0;
            opener.addEventListener('click', () => presses++);
            cellOf(1).focus();

            const shifted = press(cellOf(1), 'F10', { shiftKey: true });
            press(cellOf(1), 'ContextMenu');

            expect(presses).toBe(2);
            expect(shifted.defaultPrevented).toBe(true);
        });

        it('leaves the menu keys to a control inside the item', () => {
            gather();
            const opener = controlsOf(1)[1];
            let presses = 0;
            opener.addEventListener('click', () => presses++);
            const field = controlsOf(1)[2];
            field.focus();

            const event = press(field, 'F10', { shiftKey: true });

            expect(presses).toBe(0);
            expect(event.defaultPrevented).toBe(false);
        });

        it('takes the browser\'s own menu the key raises, and leaves the next to it', () => {
            gather();
            cellOf(1).focus();
            press(cellOf(1), 'F10', { shiftKey: true });

            const raised = new MouseEvent('contextmenu', { bubbles: true, cancelable: true });
            document.body.dispatchEvent(raised);
            const next = new MouseEvent('contextmenu', { bubbles: true, cancelable: true });
            document.body.dispatchEvent(next);

            expect(raised.defaultPrevented).toBe(true);
            expect(next.defaultPrevented).toBe(false);
        });

        it('brings the focus back to the marker once the actions hand it back to their opener', () => {
            gather();
            const opener = controlsOf(1)[1];
            const menu = document.createElement('button');
            document.body.append(menu);
            opener.addEventListener('click', () => menu.focus());
            cellOf(1).focus();

            press(cellOf(1), 'ContextMenu');
            opener.focus();

            expect(document.activeElement).toBe(cellOf(1));
        });

        it('leaves the focus on the opener once the reader has gone to another item', () => {
            gather();
            const opener = controlsOf(1)[1];
            cellOf(1).focus();

            press(cellOf(1), 'ContextMenu');
            cellOf(2).focus();
            controlsOf(1)[1].focus();

            expect(document.activeElement).toBe(opener);
        });
    });

    describe('across a packed layout', () => {
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

            selected().toHaveBeenLastCalledWith('HandleRangeSelectAsync', { value: [2, 3, 4, 5, 6] }, false);
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

        // A render taking the focused item's marker away, as one that can no longer take the focus draws none, leaving the item drawn.
        const unmark = key => {
            const marker = cellOf(key);
            marker.dispatchEvent(new FocusEvent('focusout', { bubbles: true, relatedTarget: null }));
            marker.remove();
            document.body.focus();
        };

        it('hands the focus to the next item that carries a marker when the focused one loses its own', async () => {
            attachSelection(grid, options());
            cellOf(2).focus();

            unmark(2);
            await nextFrame();

            expect(document.activeElement).toBe(cellOf(3));
        });

        it('hands the focus to the previous item that carries a marker when the last one loses its own', async () => {
            attachSelection(grid, options());
            cellOf(4).focus();

            unmark(4);
            await nextFrame();

            expect(document.activeElement).toBe(cellOf(3));
        });

        it('leaves the focus where a press sent it', async () => {
            attachSelection(grid, options());
            cellOf(2).focus();

            document.body.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true, button: 0 }));
            redraw(2);
            await nextFrame();

            expect(document.activeElement).not.toBe(cellOf(2));
        });

        it.each([
            ['Tab', {}],
            ['Shift+Tab', { shiftKey: true }],
        ])('leaves the focus where %s sent it, out of the page', async (_, modifiers) => {
            attachSelection(grid, options());
            cellOf(2).focus();

            // Past the page's last stop the focus goes to the browser, leaving the marker drawn and the page holding nothing.
            const tab = press(cellOf(2), 'Tab', modifiers);
            cellOf(2).blur();
            await nextFrame();

            expect(tab.defaultPrevented).toBe(false);
            expect(document.activeElement).toBe(document.body);
        });

        it('leaves Tab to a surface around the list that routes it', () => {
            const surface = document.createElement('div');
            grid.replaceWith(surface);
            surface.append(grid);
            let routed = false;
            handles.push(register(surface, { routes: [{ action: 'focus.next', bindings: [{ type: 'keydown', key: 'Tab' }], handle: () => { routed = true; } }] }));
            attachSelection(grid, options());
            cellOf(2).focus();

            press(cellOf(2), 'Tab');

            expect(routed).toBe(true);
        });

        // A non-focusable part of the page outside the list, where a press moves nothing the list owns.
        const elsewhere = () => {
            const paragraph = document.createElement('p');
            document.body.append(paragraph);

            return paragraph;
        };
        const pointer = (element, type, init) => element.dispatchEvent(new PointerEvent(type, { bubbles: true, cancelable: true, pointerId: 1, ...init }));

        it.each([
            ['a Tab', () => {
                press(cellOf(2), 'Tab');
                cellOf(2).dispatchEvent(new KeyboardEvent('keyup', { key: 'Tab', bubbles: true }));
            }],
            ['a press', () => click(elsewhere())],
            ['a secondary press', () => {
                const target = elsewhere();
                pointer(target, 'pointerdown', { button: 2 });
                pointer(target, 'pointerup', { button: 2 });
            }],
            ['a canceled press', () => {
                const target = elsewhere();
                pointer(target, 'pointerdown', { button: 0, pointerType: 'touch' });
                pointer(target, 'pointercancel', { button: 0, pointerType: 'touch' });
            }],
        ])('takes the focus back after %s that a host handled while keeping the focus where it was', async (_, input) => {
            attachSelection(grid, options());
            cellOf(2).focus();

            input();
            redraw(2);
            await nextFrame();

            expect(document.activeElement).toBe(cellOf(2));
        });

        it('leaves the focus where a tap sent it, the tap moving it only after its release', async () => {
            attachSelection(grid, options());
            cellOf(2).focus();
            const target = elsewhere();

            // The browser raises a tap's compatibility press, which moves the focus, between the release and the click.
            pointer(target, 'pointerdown', { button: 0, pointerType: 'touch' });
            pointer(target, 'pointerup', { button: 0, pointerType: 'touch' });
            cellOf(2).blur();
            target.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true, detail: 1 }));
            await nextFrame();

            expect(document.activeElement).toBe(document.body);
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

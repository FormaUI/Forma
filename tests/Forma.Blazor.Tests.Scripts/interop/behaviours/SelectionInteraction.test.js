import { describe, it, expect, beforeEach } from 'vitest';
import { attach } from '#interop/behaviours/SelectionInteraction.js';
import { fakeOwner } from '#support/owner.js';

describe('SelectionInteraction', () => {
    /** @type {HTMLTableElement} */
    let grid;
    /** @type {ReturnType<typeof fakeOwner>} */
    let owner;

    const row = (key, trailing) =>
        `<tr><td><span data-row-key="${key}" tabindex="-1">Row</span></td><td>${trailing}</td></tr>`;

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

    const cellOf = key => document.querySelector(`[data-row-key="${key}"]`);
    const selected = () => expect(owner.invokeMethodAsync);

    const click = (element, modifiers = {}) => {
        element.dispatchEvent(new MouseEvent('mousedown', { bubbles: true, cancelable: true, button: 0, ...modifiers }));
        element.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true, ...modifiers }));
    };

    const press = (element, key, modifiers = {}) => {
        const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true, ...modifiers });
        element.dispatchEvent(event);

        return event;
    };

    describe('with the pointer', () => {
        it('selects the clicked item', () => {
            attach(grid, { owner });

            click(cellOf(2));

            selected().toHaveBeenLastCalledWith('HandleSelectAsync', 2, false);
        });

        it('toggles rather than replaces when control is held', () => {
            attach(grid, { owner });

            click(cellOf(2), { ctrlKey: true });

            selected().toHaveBeenLastCalledWith('HandleSelectAsync', 2, true);
        });

        it('extends from the last plain click to the shift-clicked item', () => {
            attach(grid, { owner });

            click(cellOf(2));
            click(cellOf(4), { shiftKey: true });

            selected().toHaveBeenLastCalledWith('HandleRangeSelectAsync', [2, 3, 4], false);
        });

        it('extends backwards in the order the items are rendered', () => {
            attach(grid, { owner });

            click(cellOf(3));
            click(cellOf(1), { shiftKey: true });

            selected().toHaveBeenLastCalledWith('HandleRangeSelectAsync', [1, 2, 3], false);
        });

        it('selects rather than extends when nothing anchored the range', () => {
            attach(grid, { owner });

            click(cellOf(3), { shiftKey: true });

            selected().toHaveBeenLastCalledWith('HandleSelectAsync', 3, false);
        });

        // A shift-click would otherwise extend the page's own text selection across the rows.
        it('stops a shift-click selecting the text it crosses', () => {
            attach(grid, { owner });
            click(cellOf(1));

            const event = new MouseEvent('mousedown', { bubbles: true, cancelable: true, button: 0, shiftKey: true });
            cellOf(3).dispatchEvent(event);

            expect(event.defaultPrevented).toBe(true);
        });

        it.each`
            element         | selector
            ${'a checkbox'} | ${'input[type="checkbox"]'}
            ${'a link'}     | ${'a[href]'}
            ${'a button'}   | ${'button'}
        `('leaves a click on $element to that element', ({ selector }) => {
            attach(grid, { owner });

            click(grid.querySelector(selector));

            selected().not.toHaveBeenCalled();
        });

        it('ignores a click outside any item', () => {
            attach(grid, { owner });

            click(grid);

            selected().not.toHaveBeenCalled();
        });
    });

    describe('with the keyboard', () => {
        it('moves to the next item on ArrowDown', () => {
            attach(grid, { owner });
            cellOf(1).focus();

            press(cellOf(1), 'ArrowDown');

            selected().toHaveBeenLastCalledWith('HandleSelectAsync', 2, false);
        });

        it('moves to the previous item on ArrowUp', () => {
            attach(grid, { owner });
            cellOf(3).focus();

            press(cellOf(3), 'ArrowUp');

            selected().toHaveBeenLastCalledWith('HandleSelectAsync', 2, false);
        });

        it('jumps to the first item on Home', () => {
            attach(grid, { owner });
            cellOf(3).focus();

            press(cellOf(3), 'Home');

            selected().toHaveBeenLastCalledWith('HandleSelectAsync', 1, false);
        });

        it('jumps to the last item on End', () => {
            attach(grid, { owner });
            cellOf(1).focus();

            press(cellOf(1), 'End');

            selected().toHaveBeenLastCalledWith('HandleSelectAsync', 4, false);
        });

        it('starts from the first item when nothing is focused', () => {
            attach(grid, { owner });

            press(grid, 'ArrowDown');

            selected().toHaveBeenLastCalledWith('HandleSelectAsync', 1, false);
        });

        it('stays put at the end of the list', () => {
            attach(grid, { owner });
            cellOf(4).focus();

            const event = press(cellOf(4), 'ArrowDown');

            selected().not.toHaveBeenCalled();
            expect(event.defaultPrevented).toBe(false);
        });

        it('moves focus to the item it selected', () => {
            attach(grid, { owner });
            cellOf(1).focus();

            press(cellOf(1), 'ArrowDown');

            expect(document.activeElement).toBe(cellOf(2));
        });

        it('leaves the tab stop on the focused item alone', () => {
            attach(grid, { owner });
            cellOf(1).focus();

            press(cellOf(1), 'ArrowDown');

            expect([...grid.querySelectorAll('[data-row-key]')].map(cell => cell.tabIndex)).toEqual([-1, 0, -1, -1]);
        });

        it('moves without selecting when control is held', () => {
            attach(grid, { owner });
            cellOf(1).focus();

            press(cellOf(1), 'ArrowDown');
            owner.invokeMethodAsync.mockClear();
            press(cellOf(2), 'ArrowDown', { ctrlKey: true });

            expect(document.activeElement).toBe(cellOf(3));
            selected().not.toHaveBeenCalled();
        });

        it('extends the range when shift is held', () => {
            attach(grid, { owner });
            click(cellOf(1));
            cellOf(1).focus();

            press(cellOf(1), 'ArrowDown', { shiftKey: true });

            selected().toHaveBeenLastCalledWith('HandleRangeSelectAsync', [1, 2], false);
        });

        // Those keys move a caret inside a field rather than moving between items.
        it('leaves the arrow keys to a text field inside an item', () => {
            attach(grid, { owner });
            const field = grid.querySelector('input[type="text"]');

            const event = press(field, 'ArrowDown');

            selected().not.toHaveBeenCalled();
            expect(event.defaultPrevented).toBe(false);
        });
    });

    it('stops responding once disposed', () => {
        const handle = attach(grid, { owner });

        handle.dispose();
        click(cellOf(2));
        press(grid, 'ArrowDown');

        selected().not.toHaveBeenCalled();
    });
});

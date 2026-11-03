import { describe, it, expect, beforeEach } from 'vitest';
import { readSelection, take, writeSelection } from '#browser/operations/TextEntry.js';

describe('TextEntry', () => {
    /** @type {HTMLInputElement} */
    let input;

    beforeEach(() => {
        document.body.innerHTML = '<input id="entry" />';
        input = document.querySelector('#entry');
    });

    describe('take', () => {
        it('reads what is currently typed', () => {
            input.value = 'Rider';

            expect(take(input)).toEqual({ value: 'Rider' });
        });

        it('empties the entry it read', () => {
            input.value = 'Rider';

            take(input);

            expect(input.value).toBe('');
        });

        it('reads an empty entry as an empty string', () => {
            expect(take(input)).toEqual({ value: '' });
        });
    });

    describe('readSelection', () => {
        it('reports the selected range as a plain start and end', () => {
            input.value = 'Rider';
            input.setSelectionRange(1, 4);

            expect(readSelection(input)).toEqual({ start: 1, end: 4 });
        });

        it('reports a caret as an empty range at its position', () => {
            input.value = 'Rider';
            input.setSelectionRange(2, 2);

            expect(readSelection(input)).toEqual({ start: 2, end: 2 });
        });
    });

    describe('writeSelection', () => {
        it('moves the selection to the given range', () => {
            input.value = 'Rider';

            writeSelection(input, 1, 3);

            expect([input.selectionStart, input.selectionEnd]).toEqual([1, 3]);
        });
    });
});

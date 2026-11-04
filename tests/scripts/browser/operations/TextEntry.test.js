import { describe, it, expect, beforeEach } from 'vitest';
import { readSelection, writeSelection } from '#browser/operations/TextEntry.js';

describe('TextEntry', () => {
    /** @type {HTMLInputElement} */
    let input;

    beforeEach(() => {
        document.body.innerHTML = '<input id="entry" />';
        input = document.querySelector('#entry');
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

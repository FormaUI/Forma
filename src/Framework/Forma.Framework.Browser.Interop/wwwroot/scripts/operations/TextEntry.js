/**
 * TextEntry — the value and selected range of a text entry
 */

import { Transfer } from '../../../core/scripts/Transfer.js';

/**
 * Reads what is currently typed and empties the entry, answering through the transfer.
 * @param {HTMLInputElement} input
 */
export function take(input) {
    const entry = input.value;
    input.value = '';

    return Transfer.of(entry);
}

/** @param {HTMLInputElement | HTMLTextAreaElement} input */
export function readSelection(input) {
    return { start: input.selectionStart, end: input.selectionEnd };
}

/** @param {HTMLInputElement | HTMLTextAreaElement} input */
export function writeSelection(input, start, end) {
    input.setSelectionRange(start, end);
}

/**
 * TextEntry — the selected range of a text entry
 */

/** @param {HTMLInputElement | HTMLTextAreaElement} input */
export function readSelection(input) {
    return { start: input.selectionStart, end: input.selectionEnd };
}

/** @param {HTMLInputElement | HTMLTextAreaElement} input */
export function writeSelection(input, start, end) {
    input.setSelectionRange(start, end);
}

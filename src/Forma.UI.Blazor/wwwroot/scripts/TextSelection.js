// TextSelection — Reads and writes the native selection of a textarea or input, which Blazor cannot observe directly.

export function readSelection(element) {
    return { start: element.selectionStart, end: element.selectionEnd };
}

export function writeSelection(element, start, end) {
    element.setSelectionRange(start, end);
}

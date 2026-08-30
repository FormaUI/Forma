/**
 * Clipboard — what the reader carries between applications
 */

/**
 * Places text on the clipboard.
 * @param {string} text
 */
export function write(text) {
    return navigator.clipboard.writeText(text);
}

/**
 * CheckBox — state a checkbox holds as a DOM property
 */

/** @param {HTMLInputElement} checkbox */
export function setIndeterminate(checkbox, isIndeterminate) {
    checkbox.indeterminate = isIndeterminate;
}

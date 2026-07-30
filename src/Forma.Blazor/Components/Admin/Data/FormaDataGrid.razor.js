/**
 * FormaDataGrid — select-all checkbox interop
 *
 * "Some but not all rows selected" is an IDL-only property on the element, with no
 * content attribute Blazor could render, so the tri-state header checkbox has to be
 * set from script.
 */

/**
 * @param {HTMLInputElement} checkbox
 * @param {boolean} indeterminate
 */
export function setIndeterminate(checkbox, indeterminate) {
    checkbox.indeterminate = indeterminate;
}

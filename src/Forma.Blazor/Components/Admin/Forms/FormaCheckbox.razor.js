/**
 * FormaCheckbox — indeterminate interop
 *
 * "Some but not all" is an IDL-only property on the element, with no content
 * attribute Blazor could render, so it has to be set from script.
 */

/**
 * @param {HTMLInputElement} checkbox
 * @param {boolean} indeterminate
 */
export function setIndeterminate(checkbox, indeterminate) {
    checkbox.indeterminate = indeterminate;
}

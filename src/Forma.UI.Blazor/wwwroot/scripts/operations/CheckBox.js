/**
 * CheckBox — state a checkbox exposes only as a property
 *
 * "Some but not all" is an IDL-only property with no content attribute Blazor could
 * render, so it has to be set from script.
 */

/** @param {HTMLInputElement} checkbox */
export function setIndeterminate(checkbox, isIndeterminate) {
    checkbox.indeterminate = isIndeterminate;
}

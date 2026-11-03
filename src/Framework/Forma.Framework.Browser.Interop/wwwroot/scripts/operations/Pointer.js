/**
 * Pointer — routing a pointer to one element for the rest of its gesture
 */

/** @param {HTMLElement} element */
export function capture(element, pointerId) {
    element.setPointerCapture(pointerId);
}

/** @param {HTMLElement} element */
export function release(element, pointerId) {
    if (element.hasPointerCapture(pointerId)) {
        element.releasePointerCapture(pointerId);
    }
}

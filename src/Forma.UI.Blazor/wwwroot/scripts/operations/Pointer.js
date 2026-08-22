/**
 * Pointer — routing a pointer to one element for the rest of its gesture
 *
 * Capture keeps an element receiving pointermove once the pointer leaves its hit area,
 * which is the whole of a drag past the first few pixels.
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

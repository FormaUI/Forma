/**
 * FormaSplitter — pointer capture
 *
 * Capture keeps the divider receiving pointermove once the pointer leaves its narrow hit
 * area, which is the whole of a drag past the first few pixels.
 */

/**
 * @param {HTMLElement} element
 * @param {number} pointerId
 */
export function capturePointer(element, pointerId) {
    element.setPointerCapture(pointerId);
}

/**
 * @param {HTMLElement} element
 * @param {number} pointerId
 */
export function releasePointer(element, pointerId) {
    if (element.hasPointerCapture(pointerId)) {
        element.releasePointerCapture(pointerId);
    }
}

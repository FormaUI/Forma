// Pointer capture keeps the splitter receiving pointermove once the pointer leaves its narrow hit area.

export function capturePointer(element, pointerId) {
    element.setPointerCapture(pointerId);
}

export function releasePointer(element, pointerId) {
    if (element.hasPointerCapture(pointerId)) {
        element.releasePointerCapture(pointerId);
    }
}

// Mouse users get click-drag panning; touch and pen already pan the scrollable frame natively.
export function attach(frame) {
    if (frame.dataset.dragBound) {
        return;
    }

    frame.dataset.dragBound = 'true';
    let drag = null;

    frame.addEventListener('pointerdown', event => {
        if (event.pointerType !== 'mouse' || !frame.classList.contains('zoomed')) {
            return;
        }

        drag = { pointerId: event.pointerId, x: event.clientX, y: event.clientY, left: frame.scrollLeft, top: frame.scrollTop };
        frame.setPointerCapture(event.pointerId);
    });

    frame.addEventListener('pointermove', event => {
        if (!drag || event.pointerId !== drag.pointerId) {
            return;
        }

        frame.scrollLeft = drag.left - (event.clientX - drag.x);
        frame.scrollTop = drag.top - (event.clientY - drag.y);
    });

    const endDrag = event => {
        if (drag?.pointerId === event.pointerId) {
            drag = null;
        }
    };

    frame.addEventListener('pointerup', endDrag);
    frame.addEventListener('pointercancel', endDrag);
}

export function centerScroll(frame) {
    frame.scrollLeft = (frame.scrollWidth - frame.clientWidth) / 2;
    frame.scrollTop = (frame.scrollHeight - frame.clientHeight) / 2;
}

// Mouse users get click-drag panning; touch and pen already pan the scrollable frame natively.

// Keyed weakly, so a frame's listeners go with it, and re-attaching replaces them
// rather than doubling them.
const bindings = new WeakMap();

export function attach(frame) {
    bindings.get(frame)?.abort();

    const controller = new AbortController();
    const { signal } = controller;

    bindings.set(frame, controller);

    let drag = null;

    frame.addEventListener('pointerdown', event => {
        if (event.pointerType !== 'mouse' || !frame.classList.contains('zoomed')) {
            return;
        }

        drag = { pointerId: event.pointerId, x: event.clientX, y: event.clientY, left: frame.scrollLeft, top: frame.scrollTop };
        frame.setPointerCapture(event.pointerId);
    }, { signal });

    frame.addEventListener('pointermove', event => {
        if (!drag || event.pointerId !== drag.pointerId) {
            return;
        }

        frame.scrollLeft = drag.left - (event.clientX - drag.x);
        frame.scrollTop = drag.top - (event.clientY - drag.y);
    }, { signal });

    const endDrag = event => {
        if (drag?.pointerId === event.pointerId) {
            drag = null;
        }
    };

    frame.addEventListener('pointerup', endDrag, { signal });
    frame.addEventListener('pointercancel', endDrag, { signal });
}

export function centerScroll(frame) {
    frame.scrollLeft = (frame.scrollWidth - frame.clientWidth) / 2;
    frame.scrollTop = (frame.scrollHeight - frame.clientHeight) / 2;
}

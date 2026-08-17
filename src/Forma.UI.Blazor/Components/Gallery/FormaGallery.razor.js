// Pointer capture keeps the splitter receiving pointermove once the pointer leaves its narrow hit area.

export function capturePointer(element, pointerId) {
    element.setPointerCapture(pointerId);
}

export function releasePointer(element, pointerId) {
    if (element.hasPointerCapture(pointerId)) {
        element.releasePointerCapture(pointerId);
    }
}

// Observers are keyed by element id because the element a gallery is disposing may already be gone from the DOM.
const sentinels = new Map();

export function observeSentinel(id, rootId, reference) {
    disconnectSentinel(id);

    const sentinel = document.getElementById(id);

    if (!sentinel) {
        return;
    }

    // The panel scrolls rather than the page, so intersection is measured against it and not the viewport.
    const observer = new IntersectionObserver(entries => {
        if (entries.some(entry => entry.isIntersecting)) {
            reference.invokeMethodAsync('LoadNextPageAsync');
        }
    }, { root: document.getElementById(rootId), rootMargin: '200px' });

    observer.observe(sentinel);
    sentinels.set(id, observer);
}

export function disconnectSentinel(id) {
    const observer = sentinels.get(id);

    if (observer) {
        observer.disconnect();
        sentinels.delete(id);
    }
}

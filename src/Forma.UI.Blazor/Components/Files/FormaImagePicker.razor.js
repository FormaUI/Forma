// Object URLs let a thumbnail render from the browser's own File, so no image
// bytes cross the circuit and the file stays unread for the host.

// Keyed weakly, so a container's URLs and listeners go with it, and a new selection
// replaces them rather than stacking on the images it reuses.
const bindings = new WeakMap();

export function preview(container, names) {
    const input = container.querySelector('input[type="file"]');

    if (!input) {
        return;
    }

    // Clears the previous batch, whose images may have been removed before they loaded.
    const previous = bindings.get(container);

    previous?.controller.abort();

    for (const url of previous?.urls ?? []) {
        URL.revokeObjectURL(url);
    }

    const byName = new Map();

    for (const file of input.files) {
        if (!byName.has(file.name)) {
            byName.set(file.name, file);
        }
    }

    const controller = new AbortController();
    const { signal } = controller;
    const urls = [];

    container.querySelectorAll('[data-forma-preview]').forEach((image, index) => {
        const file = byName.get(names[index]);

        if (!file) {
            image.removeAttribute('src');
            return;
        }

        const url = URL.createObjectURL(file);

        urls.push(url);

        image.addEventListener('load', () => URL.revokeObjectURL(url), { once: true, signal });
        image.addEventListener('error', () => {
            URL.revokeObjectURL(url);
            image.removeAttribute('src');
        }, { once: true, signal });

        image.src = url;
    });

    bindings.set(container, { controller, urls });
}

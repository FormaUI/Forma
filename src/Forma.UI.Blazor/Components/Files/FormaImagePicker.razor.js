// Object URLs let a thumbnail render from the browser's own File, so no image
// bytes cross the circuit and the file stays unread for the host.
export function preview(container, names) {
    const input = container.querySelector('input[type="file"]');

    if (!input) {
        return;
    }

    const byName = new Map();

    for (const file of input.files) {
        if (!byName.has(file.name)) {
            byName.set(file.name, file);
        }
    }

    const images = container.querySelectorAll('[data-forma-preview]');

    images.forEach((image, index) => {
        const file = byName.get(names[index]);

        if (!file) {
            return;
        }

        const url = URL.createObjectURL(file);

        image.addEventListener('load', () => URL.revokeObjectURL(url), { once: true });
        image.addEventListener('error', () => {
            URL.revokeObjectURL(url);
            image.removeAttribute('src');
        }, { once: true });

        image.src = url;
    });
}

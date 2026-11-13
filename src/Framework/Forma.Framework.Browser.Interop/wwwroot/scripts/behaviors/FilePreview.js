/**
 * FilePreview — thumbnails rendered from the browser's own File objects
 *
 * Each thumbnail is an object URL for its file, revoked once its image has settled or the
 * handle is disposed.
 */

/**
 * @param {HTMLElement} target
 * @param {{ imageSelector?: string, fileInputSelector?: string }} options
 */
export function attach(target, options = {}) {
    return new FilePreview(target, options);
}

class FilePreview {
    #target;
    #imageSelector;
    #fileInputSelector;
    #controller = null;
    #urls = [];

    constructor(target, { imageSelector = '[data-forma-preview]', fileInputSelector = 'input[type="file"]' }) {
        this.#target = target;
        this.#imageSelector = imageSelector;
        this.#fileInputSelector = fileInputSelector;
    }

    /** Renders one thumbnail per name, matching each against the file input's current selection. */
    show(names) {
        const input = this.#target.querySelector(this.#fileInputSelector);

        if (!input) {
            return;
        }

        this.#revoke();

        const byName = new Map();

        for (const file of input.files) {
            if (!byName.has(file.name)) {
                byName.set(file.name, file);
            }
        }

        this.#controller = new AbortController();
        const { signal } = this.#controller;

        this.#target.querySelectorAll(this.#imageSelector).forEach((image, index) => {
            const file = byName.get(names[index]);

            if (!file) {
                image.removeAttribute('src');
                return;
            }

            const url = URL.createObjectURL(file);
            this.#urls.push(url);

            image.addEventListener('load', () => URL.revokeObjectURL(url), { once: true, signal });
            image.addEventListener('error', () => {
                URL.revokeObjectURL(url);
                image.removeAttribute('src');
            }, { once: true, signal });

            image.src = url;
        });
    }

    /** Lets go of the thumbnails shown, releasing the addresses still held for them. */
    clear() {
        this.#revoke();
    }

    dispose() {
        this.#revoke();
    }

    // Images removed before they loaded never fire, so their URLs are released here instead.
    #revoke() {
        this.#controller?.abort();
        this.#controller = null;

        for (const url of this.#urls) {
            URL.revokeObjectURL(url);
        }

        this.#urls = [];
    }
}

/**
 * FileDropZone — a drop zone that hands dropped files to a file input
 *
 * Assigning the files to the input and firing change sends a dropped selection down the
 * same path a click-picked one takes, so Blazor needs no separate handling for it.
 */

import { attach as attachDropZone } from './DropZone.js';

/**
 * @param {HTMLElement} target
 * @param {{ activeClass?: string }} options
 */
export function attach(target, options = {}) {
    return new FileDropZone(target, options);
}

class FileDropZone {
    #input;
    #zone;

    constructor(target, { activeClass }) {
        this.#input = target.querySelector('input[type="file"]');
        this.#zone = attachDropZone(target, {
            activeClass,
            onDrop: transfer => this.#handleDrop(transfer),
        });
    }

    dispose() {
        this.#zone.dispose();
    }

    #handleDrop(transfer) {
        if (!this.#input || this.#input.disabled || transfer.files.length === 0) {
            return;
        }

        this.#input.files = transfer.files;
        this.#input.dispatchEvent(new Event('change', { bubbles: true }));
    }
}

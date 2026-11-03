/**
 * FileDropZone — a drop zone that hands dropped files to a file input
 *
 * A drop assigns the files to the input and fires change, the path a click-picked selection
 * takes.
 */

import { attach as attachDropZone } from './DropZone.js';

/**
 * @param {HTMLElement} target
 * @param {{ action: string, activeClass?: string }} options The action the drop is routed as, and the class marking the target while a drag is over it.
 */
export function attach(target, options) {
    return new FileDropZone(target, options);
}

class FileDropZone {
    #target;
    #zone;

    constructor(target, { action, activeClass }) {
        this.#target = target;

        this.#zone = attachDropZone(target, {
            action,
            accepts: ['Files'],
            activeClass,
            onDrop: transfer => this.#handleDrop(transfer),
        });
    }

    dispose() {
        this.#zone.dispose();
    }

    #handleDrop(transfer) {
        const input = this.#target.querySelector('input[type="file"]');

        if (!input || input.disabled || transfer.files.length === 0) {
            return;
        }

        input.files = transfer.files;
        input.dispatchEvent(new Event('change', { bubbles: true }));
    }
}

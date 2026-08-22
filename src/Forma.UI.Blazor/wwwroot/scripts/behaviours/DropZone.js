/**
 * DropZone — a drag-and-drop target
 *
 * Nested children fire dragenter and dragleave as the pointer crosses them, so the active
 * state is depth-counted rather than toggled per event.
 */

/**
 * @param {HTMLElement} target
 * @param {{ activeClass?: string, onDrop?: (transfer: DataTransfer) => void }} options
 */
export function attach(target, options = {}) {
    return new DropZone(target, options);
}

class DropZone {
    #target;
    #activeClass;
    #onDrop;
    #controller = new AbortController();
    #depth = 0;

    constructor(target, { activeClass, onDrop }) {
        this.#target = target;
        this.#activeClass = activeClass;
        this.#onDrop = onDrop;

        const { signal } = this.#controller;

        target.addEventListener('dragenter', event => this.#handleEnter(event), { signal });
        target.addEventListener('dragover', event => this.#handleOver(event), { signal });
        target.addEventListener('dragleave', () => this.#handleLeave(), { signal });
        target.addEventListener('drop', event => this.#handleDrop(event), { signal });
    }

    dispose() {
        this.#controller.abort();
        this.#setActive(false);
    }

    #handleEnter(event) {
        event.preventDefault();
        this.#depth += 1;
        this.#setActive(true);
    }

    #handleOver(event) {
        event.preventDefault();
        event.dataTransfer.dropEffect = 'copy';
    }

    #handleLeave() {
        this.#depth = Math.max(this.#depth - 1, 0);

        if (this.#depth === 0) {
            this.#setActive(false);
        }
    }

    #handleDrop(event) {
        event.preventDefault();
        this.#depth = 0;
        this.#setActive(false);
        this.#onDrop?.(event.dataTransfer);
    }

    #setActive(isActive) {
        if (this.#activeClass) {
            this.#target.classList.toggle(this.#activeClass, isActive);
        }
    }
}

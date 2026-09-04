/**
 * Modal — opening and closing a native dialog as a modal
 *
 * showModal and close are imperative DOM calls with no HTML-attribute equivalent, and the
 * backdrop is part of the dialog element itself, so a click landing on the dialog rather
 * than on its content is a backdrop click.
 */

/**
 * @param {HTMLDialogElement} target
 */
export function attach(target) {
    return new Modal(target);
}

class Modal {
    #dialog;
    #controller = null;

    constructor(dialog) {
        this.#dialog = dialog;
    }

    open(closeOnBackdropClick) {
        // A dialog can close without close() being called, through Escape or a method="dialog"
        // form, leaving the previous listener attached.
        this.#release();

        if (this.#dialog.open) {
            return;
        }

        this.#dialog.showModal();

        if (!closeOnBackdropClick) {
            return;
        }

        this.#controller = new AbortController();

        this.#dialog.addEventListener('click', event => {
            if (event.target === this.#dialog) {
                this.#dialog.close();
            }
        }, { signal: this.#controller.signal });
    }

    close() {
        this.#release();

        if (this.#dialog.open) {
            this.#dialog.close();
        }
    }

    dispose() {
        this.#release();
    }

    #release() {
        this.#controller?.abort();
        this.#controller = null;
    }
}

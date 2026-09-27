/**
 * Dialog — opening and closing a native dialog as a modal
 *
 * showModal and close are imperative DOM calls with no HTML-attribute equivalent, and the
 * scrim is part of the dialog element itself, so a click landing on the dialog rather
 * than on its content is a scrim click. showModal puts focus on the first control in the
 * dialog, which is the header's close button, so the body's first control takes it instead
 * where the body has one: a form opens on its first field, not on the way out of it.
 */

import { focusablesIn } from '../behaviors/Focus.js';

/**
 * @param {HTMLDialogElement} target
 */
export function attach(target) {
    return new Dialog(target);
}

class Dialog {
    #dialog;
    #controller = null;

    constructor(dialog) {
        this.#dialog = dialog;
    }

    /**
     * @param {{ closeOnScrimClick?: boolean, focusWithin?: string }} options
     * closeOnScrimClick closes the dialog on a click landing beside its content; focusWithin selects the
     * part whose first control takes focus as the dialog opens, and a part with no control leaves focus
     * where showModal put it.
     */
    open({ closeOnScrimClick = false, focusWithin = null } = {}) {
        // A dialog can close without close() being called, through Escape or a method="dialog"
        // form, leaving the previous listener attached.
        this.#release();

        if (this.#dialog.open) {
            return;
        }

        this.#dialog.showModal();
        this.#focusWithin(focusWithin);

        if (!closeOnScrimClick) {
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

    #focusWithin(selector) {
        const part = selector ? this.#dialog.querySelector(selector) : null;
        const [first] = part ? focusablesIn(part) : [];

        first?.focus();
    }

    #release() {
        this.#controller?.abort();
        this.#controller = null;
    }
}

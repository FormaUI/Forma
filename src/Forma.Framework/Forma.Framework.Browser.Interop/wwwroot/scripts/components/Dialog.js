/**
 * Dialog — opening and closing a native dialog as a modal
 *
 * The scrim is part of the dialog element itself, so a click landing on the dialog rather
 * than on its content is a scrim click.
 */

import { FocusSurface } from '../focus/FocusSurface.mjs';

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
     * part whose first control takes focus as the dialog opens.
     */
    open({ closeOnScrimClick = false, focusWithin = null } = {}) {
        // Escape or a method="dialog" form closes the dialog past close(), leaving the previous listener attached.
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
        const [first] = part ? new FocusSurface(part).controls() : [];

        first?.focus();
    }

    #release() {
        this.#controller?.abort();
        this.#controller = null;
    }
}

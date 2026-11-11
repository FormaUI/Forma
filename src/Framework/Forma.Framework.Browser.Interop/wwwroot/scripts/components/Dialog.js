/**
 * Dialog — opening and closing a native dialog as a modal
 *
 * The scrim is part of the dialog element itself, so a click landing on the dialog rather
 * than on its content is a scrim click. A dialog closing hands the focus back to what held it as
 * the dialog opened, as the browser does, and where that has gone with a menu that closed, to its
 * nearest ancestor that can take the focus.
 */

import { FocusSurface } from '../focus/FocusSurface.mjs';

/**
 * @param {HTMLDialogElement} target
 */
export function attach(target) {
    return new Dialog(target);
}

/** An element a script can move the focus to. */
const Focusable = 'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]';

class Dialog {
    #dialog;
    #controller = null;
    /** What held the focus as the dialog opened, and its focusable ancestor, until the focus is handed back. */
    #focusReturn = null;

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

        const opener = this.#dialog.ownerDocument.activeElement;
        this.#focusReturn = { opener, focusableAncestor: Dialog.#focusableAncestorOf(opener) };
        this.#dialog.addEventListener('close', () => this.#returnFocusLater(), { once: true });
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

    // A dialog taken away while open closes without a close event, as its owner stopping drawing it does.
    dispose() {
        this.#release();
        this.#returnFocusLater();
    }

    #focusWithin(selector) {
        const part = selector ? this.#dialog.querySelector(selector) : null;
        const [firstControl] = part ? new FocusSurface(part).controls() : [];

        firstControl?.focus();
    }

    #returnFocusLater() {
        const focusReturn = this.#focusReturn;
        this.#focusReturn = null;

        if (focusReturn) {
            requestAnimationFrame(() => this.#returnFocus(focusReturn));
        }
    }

    // The browser hands the focus back to the opener itself; where the opener went, the focus falls to nothing, or stays in the closed
    // dialog until its owner takes it away, as in WebKit, and goes to the opener's focusable ancestor.
    #returnFocus({ opener, focusableAncestor }) {
        const document = this.#dialog.ownerDocument;
        const activeElement = document.activeElement;

        if ((this.#dialog.open && this.#dialog.isConnected) || (activeElement && activeElement !== document.body && !this.#dialog.contains(activeElement))) {
            return;
        }

        const target = opener?.isConnected ? opener : focusableAncestor?.isConnected ? focusableAncestor : null;
        target?.focus();
    }

    // The nearest ancestor of the opener that can take the focus and stays when a popover holding the opener closes.
    static #focusableAncestorOf(opener) {
        for (let element = opener?.parentElement; element; element = element.parentElement) {
            if (element.closest('[popover]') === null && element.matches(Focusable)) {
                return element;
            }
        }

        return null;
    }

    #release() {
        this.#controller?.abort();
        this.#controller = null;
    }
}

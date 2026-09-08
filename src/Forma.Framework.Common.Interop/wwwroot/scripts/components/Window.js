/**
 * Window — a window's browser side: shown in the top layer, put in front of the other windows
 * when raised; modal, it keeps Tab inside itself and stands on a scrim; either way it remembers
 * where focus was inside it and takes focus back. Its box is its frame's.
 */

import { Focus, focusablesIn } from '../behaviours/Focus.js';

/**
 * @param {HTMLElement} target The window element, a manual popover.
 * @param {{
 *   presented: boolean,
 *   modal: boolean,
 *   scrim: HTMLElement | null
 * }} options
 *   Whether the window is shown at all rather than minimized or hidden; whether Tab stays
 *   inside it; the popover shown beneath it, if any.
 */
export function attach(target, options) {
    return new Window(target, options);
}

/** The window shown last, which the top layer keeps in front until another is shown. */
let topmost = null;

class Window {
    #window;
    #presented;
    #scrim;
    #listeners = new AbortController();
    #lastFocused = null;
    #isPressed = false;
    #raiseAfterRelease = false;

    constructor(window, { presented, modal, scrim }) {
        this.#window = window;
        this.#presented = presented;
        this.#scrim = scrim ?? null;

        const { signal } = this.#listeners;
        window.addEventListener('focusout', event => this.#remember(event.target), { signal });
        window.addEventListener('pointerdown', () => { this.#isPressed = true; }, { signal });
        globalThis.addEventListener('pointerup', () => this.#handleRelease(), { capture: true, signal });
        globalThis.addEventListener('pointercancel', () => this.#handleRelease(), { capture: true, signal });
        globalThis.addEventListener('blur', () => this.#handleRelease(), { signal });

        if (modal) {
            new Focus(window, { wrap: true }, signal);
        }

        if (presented) {
            this.#show();
        }
    }

    dispose() {
        this.#listeners.abort();
        this.#hide();
    }

    /** Moves focus into the window unless a control inside it already holds it: to the element last focused inside it, else its first control, else the window itself. */
    focus() {
        const active = document.activeElement;

        if (active !== this.#window && this.#window.contains(active)) {
            return;
        }

        const remembered = this.#lastFocused;
        const target = remembered?.isConnected && this.#window.contains(remembered)
            ? remembered
            : focusablesIn(this.#window)[0] ?? this.#window;

        target.focus();
    }

    /** Puts a presented window on top of every other window; one pressed inside is raised once the press ends, since hiding it would end the press's pointer capture. A press ends with the pointer's release or cancel, or with the browser window losing focus. */
    raise() {
        if (!this.#presented || topmost === this) {
            return;
        }

        if (this.#isPressed) {
            this.#raiseAfterRelease = true;
            return;
        }

        this.#hide();
        this.#show();
    }

    /**
     * Shows the window in the top layer, or takes it out while it is minimized or hidden.
     * @param {boolean} presented
     */
    present(presented) {
        this.#presented = presented;

        if (presented) {
            this.#show();
        } else {
            this.#hide();
        }
    }

    /** Keeps the element focus is leaving, so focus can return there when the window is activated again. */
    #remember(element) {
        if (element instanceof HTMLElement && this.#window.contains(element)) {
            this.#lastFocused = element;
        }
    }

    #handleRelease() {
        this.#isPressed = false;

        if (this.#raiseAfterRelease) {
            this.#raiseAfterRelease = false;
            this.raise();
        }
    }

    /** Raises the window into the top layer, where no ancestor's box or stacking can reach it, with its scrim shown first so it stands beneath. */
    #show() {
        if (this.#scrim && typeof this.#scrim.showPopover === 'function' && !this.#scrim.matches(':popover-open')) {
            this.#scrim.showPopover();
        }

        if (typeof this.#window.showPopover === 'function' && !this.#window.matches(':popover-open')) {
            this.#window.showPopover();
            topmost = this;
        }
    }

    #hide() {
        if (typeof this.#window.hidePopover === 'function' && this.#window.matches(':popover-open')) {
            this.#window.hidePopover();
        }

        if (this.#scrim && typeof this.#scrim.hidePopover === 'function' && this.#scrim.matches(':popover-open')) {
            this.#scrim.hidePopover();
        }

        if (topmost === this) {
            topmost = null;
        }
    }
}

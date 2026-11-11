/**
 * Window — a window's browser side: shown in the top layer, put in front of the other windows
 * when raised; modal, it keeps Tab inside itself and stands on a scrim; either way it remembers
 * where focus was inside it and takes focus back.
 */

import { tell } from '../../../core/scripts/Owner.js';
import { FocusSurface } from '../focus/FocusSurface.mjs';
import { TabOrder } from '../focus/TabOrder.mjs';

/**
 * @param {HTMLElement} target The window element, a manual popover.
 * @param {{
 *   isPresented: boolean,
 *   isModal: boolean,
 *   scrim: HTMLElement | null,
 *   owner?: import('../../../core/scripts/Owner.js').Owner | null
 * }} options
 *   Whether the window is shown; whether Tab stays inside it; the scrim popover shown
 *   beneath it; and the .NET owner told of each press inside it.
 */
export function attach(target, options) {
    return new Window(target, options);
}

/** The window shown last, which the top layer keeps in front until another is shown. */
let topmostWindow = null;

class Window {
    #window;
    #isPresented;
    #scrim;
    #owner;
    #listeners = new AbortController();
    #lastFocusedElement = null;
    #isPressed = false;
    #raiseAfterRelease = false;

    constructor(window, { isPresented, isModal, scrim, owner = null }) {
        this.#window = window;
        this.#isPresented = isPresented;
        this.#scrim = scrim ?? null;
        this.#owner = owner;

        const { signal } = this.#listeners;
        window.addEventListener('focusout', event => this.#remember(event.target), { signal });

        // A press anywhere inside is observed, neither prevented nor stopped, and stays with what owns it.
        window.addEventListener('pointerdown', () => {
            this.#isPressed = true;
            tell(this.#owner, 'HandlePressAsync');
        }, { signal });
        globalThis.addEventListener('pointerup', () => this.#handleRelease(), { capture: true, signal });
        globalThis.addEventListener('pointercancel', () => this.#handleRelease(), { capture: true, signal });
        globalThis.addEventListener('blur', () => this.#handleRelease(), { signal });

        if (isModal) {
            const tabOrder = new TabOrder(new FocusSurface(window), { wrap: true });
            signal.addEventListener('abort', () => tabOrder.dispose(), { once: true });
        }

        if (isPresented) {
            this.#show();
        }
    }

    dispose() {
        this.#listeners.abort();
        this.#hide();
    }

    /**
     * Moves focus into the window.
     *
     * Focus already on a control inside the window stays there; otherwise it goes to the element last
     * focused inside the window, else its first control, else the window itself.
     */
    focus() {
        const activeElement = document.activeElement;

        if (activeElement !== this.#window && this.#window.contains(activeElement)) {
            return;
        }

        const rememberedElement = this.#lastFocusedElement;
        const target = rememberedElement?.isConnected && this.#window.contains(rememberedElement)
            ? rememberedElement
            : new FocusSurface(this.#window).controls()[0] ?? this.#window;

        target.focus();
    }

    /**
     * Puts a presented window on top of every other window.
     *
     * A window pressed inside is raised once the press ends, with the pointer's release or cancel or the
     * browser window losing focus.
     */
    raise() {
        if (!this.#isPresented || topmostWindow === this) {
            return;
        }

        // Hiding a pressed window would end the press's pointer capture.
        if (this.#isPressed) {
            this.#raiseAfterRelease = true;
            return;
        }

        this.#hide();
        this.#show();
    }

    /**
     * Shows the window in the top layer or takes it out, to match isPresented.
     * @param {boolean} isPresented
     */
    present(isPresented) {
        this.#isPresented = isPresented;

        if (isPresented) {
            this.#show();
        } else {
            this.#hide();
        }
    }

    /** Keeps the element inside the window that focus is leaving. */
    #remember(element) {
        if (element instanceof HTMLElement && this.#window.contains(element)) {
            this.#lastFocusedElement = element;
        }
    }

    #handleRelease() {
        this.#isPressed = false;

        if (this.#raiseAfterRelease) {
            this.#raiseAfterRelease = false;
            this.raise();
        }
    }

    /** Shows the window in the top layer, its scrim shown first so it stands beneath. */
    #show() {
        if (this.#scrim && typeof this.#scrim.showPopover === 'function' && !this.#scrim.matches(':popover-open')) {
            this.#scrim.showPopover();
        }

        if (typeof this.#window.showPopover === 'function' && !this.#window.matches(':popover-open')) {
            this.#window.showPopover();
            topmostWindow = this;
        }
    }

    #hide() {
        if (typeof this.#window.hidePopover === 'function' && this.#window.matches(':popover-open')) {
            this.#window.hidePopover();
        }

        if (this.#scrim && typeof this.#scrim.hidePopover === 'function' && this.#scrim.matches(':popover-open')) {
            this.#scrim.hidePopover();
        }

        if (topmostWindow === this) {
            topmostWindow = null;
        }
    }
}

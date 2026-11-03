/**
 * FocusRecovery — a surface handing the focus to its marked heir when the element holding it goes away
 */

export class FocusRecovery {
    #target;
    #heir;
    #mutations;
    #abort = new AbortController();
    /** The element inside the surface that last took the focus, while the surface still holds it. */
    #held = null;

    /**
     * @param {HTMLElement} target The surface.
     * @param {string} heir The attribute marking the element that takes the focus.
     */
    constructor(target, heir) {
        this.#target = target;
        this.#heir = heir;

        const signal = this.#abort.signal;
        target.addEventListener('focusin', event => { this.#held = event.target; }, { signal });

        // A focus the reader took out of the surface is not the surface's to hand on.
        target.addEventListener('focusout', event => {
            if (event.relatedTarget !== null && !target.contains(event.relatedTarget)) {
                this.#held = null;
            }
        }, { signal });

        this.#mutations = new MutationObserver(() => this.#handOn());
        this.#mutations.observe(target, { childList: true, subtree: true, attributes: true, attributeFilter: [heir] });
    }

    dispose() {
        this.#abort.abort();
        this.#mutations.disconnect();
    }

    /** Moves a focus that fell to nothing with the element holding it to the element marked as heir. */
    #handOn() {
        const held = this.#held;
        const active = this.#target.ownerDocument.activeElement;

        if (held === null || held.isConnected || (active !== null && active !== this.#target.ownerDocument.body)) {
            return;
        }

        const selector = `[${this.#heir}]`;
        const heir = this.#target.matches(selector) ? this.#target : this.#target.querySelector(selector);

        if (heir !== null) {
            this.#held = null;
            heir.focus();
        }
    }
}

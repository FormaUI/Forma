/**
 * FocusHandOff — keeping the focus inside a surface when the element holding it goes away
 *
 * The surface's owner marks the element that takes the focus, with the attribute it names, in
 * the same render that takes the focused element away. Once that render is on the page, in the
 * same task, the focus moves to the marked element, so the browser reports one move of the focus
 * rather than its fall to the page and then the element. Only a focus that fell to nothing is
 * handed on: one the reader moved elsewhere stays where it went.
 */

/**
 * @param {HTMLElement} target The surface.
 * @param {{ attribute?: string }} options The attribute marking the element that takes the focus.
 */
export function attach(target, options = {}) {
    return new FocusHandOff(target, options);
}

class FocusHandOff {
    #target;
    #attribute;
    #observer;
    #listening = new AbortController();
    #held = null;

    constructor(target, { attribute = 'data-focus-heir' }) {
        this.#target = target;
        this.#attribute = attribute;

        const signal = this.#listening.signal;
        target.addEventListener('focusin', event => { this.#held = event.target; }, { signal });

        // A focus the reader took out of the surface is not the surface's to hand on.
        target.addEventListener('focusout', event => {
            if (event.relatedTarget !== null && !target.contains(event.relatedTarget)) {
                this.#held = null;
            }
        }, { signal });

        this.#observer = new MutationObserver(() => this.#handOn());
        this.#observer.observe(target, { childList: true, subtree: true, attributes: true, attributeFilter: [attribute] });
    }

    dispose() {
        this.#listening.abort();
        this.#observer.disconnect();
    }

    #handOn() {
        const held = this.#held;
        const active = this.#target.ownerDocument.activeElement;

        if (held === null || held.isConnected || (active !== null && active !== this.#target.ownerDocument.body)) {
            return;
        }

        const selector = `[${this.#attribute}]`;
        const heir = this.#target.matches(selector) ? this.#target : this.#target.querySelector(selector);

        if (heir !== null) {
            this.#held = null;
            heir.focus();
        }
    }
}

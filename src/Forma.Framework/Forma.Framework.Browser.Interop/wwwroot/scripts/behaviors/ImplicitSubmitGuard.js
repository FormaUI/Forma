/**
 * ImplicitSubmitGuard — stopping Enter from submitting the form around a control
 *
 * A host may place a control inside a form, where Enter would implicitly submit it on top
 * of whatever the control does with the key itself. A surface holding many such controls
 * among others names them with a selector, so Enter on a button or a link within it keeps
 * its default.
 */

/**
 * @param {HTMLElement} target
 * @param {{ within?: string }} options
 */
export function attach(target, options = {}) {
    return new ImplicitSubmitGuard(target, options);
}

class ImplicitSubmitGuard {
    #controller = new AbortController();

    constructor(target, { within = null }) {
        target.addEventListener('keydown', event => {
            if (event.key === 'Enter' && (within === null || event.target.closest(within))) {
                event.preventDefault();
            }
        }, { signal: this.#controller.signal });
    }

    dispose() {
        this.#controller.abort();
    }
}

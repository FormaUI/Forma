/**
 * ImplicitSubmitGuard — stopping Enter from submitting the form around a control
 *
 * A host may place a control inside a form, where Enter would implicitly submit it on top
 * of whatever the control does with the key itself.
 */

/** @param {HTMLElement} target */
export function attach(target) {
    return new ImplicitSubmitGuard(target);
}

class ImplicitSubmitGuard {
    #controller = new AbortController();

    constructor(target) {
        target.addEventListener('keydown', event => {
            if (event.key === 'Enter') {
                event.preventDefault();
            }
        }, { signal: this.#controller.signal });
    }

    dispose() {
        this.#controller.abort();
    }
}

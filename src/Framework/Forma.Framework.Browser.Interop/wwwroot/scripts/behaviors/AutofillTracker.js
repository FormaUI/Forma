/**
 * AutofillTracker — marking a field the browser filled without the user typing
 *
 * The field stylesheet runs a zero-duration animation while :-webkit-autofill applies, and
 * its 'animationstart' event toggles the filled class.
 */

/**
 * @param {HTMLInputElement} target
 * @param {{ filledClass?: string, startAnimation?: string, cancelAnimation?: string }} options
 */
export function attach(target, options = {}) {
    return new AutofillTracker(target, options);
}

class AutofillTracker {
    #controller = new AbortController();

    constructor(target, {
        filledClass = 'has-value',
        startAnimation = 'forma-autofill-start',
        cancelAnimation = 'forma-autofill-cancel',
    }) {
        target.addEventListener('animationstart', event => {
            if (event.animationName === startAnimation) {
                target.classList.add(filledClass);
            } else if (event.animationName === cancelAnimation) {
                target.classList.remove(filledClass);
            }
        }, { signal: this.#controller.signal });
    }

    dispose() {
        this.#controller.abort();
    }
}

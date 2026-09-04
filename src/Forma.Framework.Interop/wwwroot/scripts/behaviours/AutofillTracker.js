/**
 * AutofillTracker — marking a field the browser filled without the user typing
 *
 * A browser filling a field automatically applies :-webkit-autofill, and never fires an
 * input event for it. The field stylesheet hangs a zero-duration animation off that
 * pseudo-class, so the fill surfaces as an 'animationstart' event that can be listened
 * for. The class it toggles is what keeps a floating label floated over a value the user
 * never typed.
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

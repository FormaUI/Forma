/**
 * FormaInput — autofill detection
 *
 * Browsers apply :-webkit-autofill when they fill a field automatically.
 * The CSS applies a zero-duration animation on that pseudo-class, which
 * triggers the 'animationstart' event. We listen for it and add/remove
 * the .has-value class so the floating label stays floated over autofilled values.
 */

/**
 * @param {HTMLInputElement} input
 */
export function initAutofillDetection(input) {
    input.addEventListener('animationstart', (e) => {
        if (e.animationName === 'forma-autofill-start') {
            input.classList.add('has-value');
        } else if (e.animationName === 'forma-autofill-cancel') {
            input.classList.remove('has-value');
        }
    });
}

/**
 * Autofill detection
 *
 * A browser filling a field automatically applies :-webkit-autofill, and never fires an
 * input event for it. The field stylesheet hangs a zero-duration animation off that
 * pseudo-class, so the fill surfaces as an 'animationstart' event that can be listened
 * for. The .has-value class it toggles is what keeps a floating label floated over a
 * value the user never typed.
 */

/**
 * @param {HTMLInputElement} input
 */
export function initAutofillDetection(input) {
    input.addEventListener('animationstart', event => {
        if (event.animationName === 'forma-autofill-start') {
            input.classList.add('has-value');
        } else if (event.animationName === 'forma-autofill-cancel') {
            input.classList.remove('has-value');
        }
    });
}

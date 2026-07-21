/**
 * SiteFloatingLabelField — autofill detection
 *
 * Browsers apply :-webkit-autofill when they fill a field automatically.
 * The CSS applies a zero-duration animation on that pseudo-class, which
 * triggers the 'animationstart' event. We listen for it and add/remove
 * the .has-value class so the floating label stays floated over autofilled values.
 */

/**
 * @param {HTMLInputElement} inputEl
 */
export function initAutofillDetection(inputEl) {
    inputEl.addEventListener('animationstart', (e) => {
        if (e.animationName === 'site-autofill-start') {
            inputEl.classList.add('has-value');
        } else if (e.animationName === 'site-autofill-cancel') {
            inputEl.classList.remove('has-value');
        }
    });
}

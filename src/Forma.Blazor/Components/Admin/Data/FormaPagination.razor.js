/**
 * FormaPagination — page box interop
 *
 * Two rough edges the DOM will not smooth over declaratively:
 *  - a host may place the pager inside a form, where Enter would implicitly submit it
 *    on top of running the jump;
 *  - typing that happens before the circuit connects never reaches the component, so
 *    the box itself has to be readable as a fallback.
 */

/**
 * @param {HTMLInputElement} input
 */
export function suppressImplicitSubmit(input) {
    if (input.dataset.formaSubmitGuard) {
        return;
    }

    input.dataset.formaSubmitGuard = 'true';
    input.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') {
            e.preventDefault();
        }
    });
}

/**
 * Reads what is currently typed and empties the box.
 * @param {HTMLInputElement} input
 * @returns {string}
 */
export function takePageEntry(input) {
    const entry = input.value;
    input.value = '';
    return entry;
}

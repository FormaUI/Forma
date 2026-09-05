import { describe, it, expect, beforeEach } from 'vitest';
import { setIndeterminate } from '#common/operations/CheckBox.js';

describe('CheckBox', () => {
    /** @type {HTMLInputElement} */
    let checkbox;

    beforeEach(() => {
        document.body.innerHTML = '<input type="checkbox" id="all" />';
        checkbox = document.querySelector('#all');
    });

    it('puts the checkbox into its mixed state', () => {
        setIndeterminate(checkbox, true);

        expect(checkbox.indeterminate).toBe(true);
    });

    it('takes the checkbox out of its mixed state', () => {
        setIndeterminate(checkbox, true);

        setIndeterminate(checkbox, false);

        expect(checkbox.indeterminate).toBe(false);
    });

    // The mixed state is what the checkbox shows, not what it submits.
    it('leaves the checked value alone', () => {
        checkbox.checked = true;

        setIndeterminate(checkbox, true);

        expect(checkbox.checked).toBe(true);
    });
});

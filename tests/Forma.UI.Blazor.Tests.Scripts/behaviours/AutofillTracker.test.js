import { describe, it, expect, beforeEach } from 'vitest';
import { attach } from '#behaviours/AutofillTracker.js';

describe('AutofillTracker', () => {
    /** @type {HTMLInputElement} */
    let input;

    beforeEach(() => {
        document.body.innerHTML = '<input id="field" />';
        input = document.querySelector('#field');
    });

    const animate = animationName => {
        const event = new Event('animationstart', { bubbles: true });
        event.animationName = animationName;
        input.dispatchEvent(event);
    };

    it('marks the field filled when the browser fills it', () => {
        attach(input);

        animate('forma-autofill-start');

        expect(input.classList.contains('has-value')).toBe(true);
    });

    it('clears the mark when the browser takes the value back', () => {
        attach(input);

        animate('forma-autofill-start');
        animate('forma-autofill-cancel');

        expect(input.classList.contains('has-value')).toBe(false);
    });

    it('ignores an animation the field runs for its own reasons', () => {
        attach(input);

        animate('forma-shake');

        expect(input.classList.contains('has-value')).toBe(false);
    });

    it('honours the caller-imposed class and animation names', () => {
        attach(input, { filledClass: 'filled', startAnimation: 'start', cancelAnimation: 'cancel' });

        animate('start');

        expect(input.classList.contains('filled')).toBe(true);
    });

    it('stops tracking once disposed', () => {
        const handle = attach(input);

        handle.dispose();
        animate('forma-autofill-start');

        expect(input.classList.contains('has-value')).toBe(false);
    });
});

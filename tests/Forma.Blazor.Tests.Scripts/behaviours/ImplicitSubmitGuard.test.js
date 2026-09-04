import { describe, it, expect, beforeEach } from 'vitest';
import { attach } from '#interop/behaviours/ImplicitSubmitGuard.js';

describe('ImplicitSubmitGuard', () => {
    /** @type {HTMLInputElement} */
    let input;

    beforeEach(() => {
        document.body.innerHTML = '<form><input id="jump" /></form>';
        input = document.querySelector('#jump');
    });

    const press = key => {
        const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true });
        input.dispatchEvent(event);

        return event;
    };

    it('stops Enter submitting the surrounding form', () => {
        attach(input);

        expect(press('Enter').defaultPrevented).toBe(true);
    });

    it.each(['Escape', 'Tab', 'a'])('leaves %s alone', key => {
        attach(input);

        expect(press(key).defaultPrevented).toBe(false);
    });

    it('stops guarding once disposed', () => {
        const handle = attach(input);

        handle.dispose();

        expect(press('Enter').defaultPrevented).toBe(false);
    });
});

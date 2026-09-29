import { describe, it, expect, beforeEach } from 'vitest';
import { attach } from '#browser/behaviors/ImplicitSubmitGuard.js';

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

    it('guards the controls a selector names within a surface and leaves the rest alone', () => {
        document.body.innerHTML = '<form><div id="surface"><input id="marker" data-row-key="1" /><button id="action">Go</button></div></form>';
        attach(document.querySelector('#surface'), { within: '[data-row-key]' });
        const press = (element, key) => {
            const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true });
            element.dispatchEvent(event);

            return event;
        };

        expect(press(document.querySelector('#marker'), 'Enter').defaultPrevented).toBe(true);
        expect(press(document.querySelector('#action'), 'Enter').defaultPrevented).toBe(false);
    });

    it('stops guarding once disposed', () => {
        const handle = attach(input);

        handle.dispose();

        expect(press('Enter').defaultPrevented).toBe(false);
    });
});

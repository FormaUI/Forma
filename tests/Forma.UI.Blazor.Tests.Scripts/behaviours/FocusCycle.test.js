import { describe, it, expect, beforeEach } from 'vitest';
import { attach } from '#behaviours/FocusCycle.js';

describe('FocusCycle', () => {
    beforeEach(() => {
        document.body.innerHTML = '<div id="surface"><button id="one">One</button><button id="two" disabled>Two</button><button id="three">Three</button></div><button id="after">After</button>';
    });

    function tab(shiftKey = false) {
        const event = new KeyboardEvent('keydown', { key: 'Tab', shiftKey, bubbles: true, cancelable: true });
        document.activeElement.dispatchEvent(event);

        return event;
    }

    it('moves Tab to the next enabled control and wraps at the end', () => {
        attach(document.querySelector('#surface'));
        document.querySelector('#one').focus();

        const first = tab();
        expect(document.activeElement.id).toBe('three');

        tab();
        expect(document.activeElement.id).toBe('one');
        expect(first.defaultPrevented).toBe(true);
    });

    it('moves Shift+Tab back and wraps at the start', () => {
        attach(document.querySelector('#surface'));
        document.querySelector('#one').focus();

        tab(true);

        expect(document.activeElement.id).toBe('three');
    });

    it('leaves a key other than Tab alone', () => {
        attach(document.querySelector('#surface'));
        document.querySelector('#one').focus();

        const event = new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true });
        document.activeElement.dispatchEvent(event);

        expect(event.defaultPrevented).toBe(false);
        expect(document.activeElement.id).toBe('one');
    });

    it('stops cycling once disposed', () => {
        const handle = attach(document.querySelector('#surface'));
        document.querySelector('#one').focus();
        handle.dispose();

        const event = tab();

        expect(event.defaultPrevented).toBe(false);
    });
});

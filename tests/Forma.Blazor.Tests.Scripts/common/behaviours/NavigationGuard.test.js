import { describe, it, expect, beforeEach } from 'vitest';
import { attach } from '#common/behaviours/NavigationGuard.js';

describe('NavigationGuard', () => {
    /** @type {HTMLElement} */
    let list;

    beforeEach(() => {
        document.body.innerHTML = '<ul id="list"></ul>';
        list = document.querySelector('#list');
    });

    const press = key => {
        const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true });
        list.dispatchEvent(event);

        return event;
    };

    it.each(['ArrowUp', 'ArrowDown', 'Home', 'End'])('stops the page scrolling on %s', key => {
        attach(list);

        expect(press(key).defaultPrevented).toBe(true);
    });

    it.each(['Enter', ' ', 'Tab', 'Escape', 'ArrowLeft', 'ArrowRight'])('leaves %s alone', key => {
        attach(list);

        expect(press(key).defaultPrevented).toBe(false);
    });

    it('stops guarding once disposed', () => {
        const handle = attach(list);

        handle.dispose();

        expect(press('ArrowDown').defaultPrevented).toBe(false);
    });
});

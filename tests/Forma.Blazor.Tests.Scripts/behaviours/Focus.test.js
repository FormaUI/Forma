import { describe, it, expect, beforeEach } from 'vitest';
import { attach, focusablesIn } from '#behaviours/Focus.js';

function tab(shiftKey = false) {
    const event = new KeyboardEvent('keydown', { key: 'Tab', shiftKey, bubbles: true, cancelable: true });
    document.activeElement.dispatchEvent(event);

    return event;
}

const focus = id => document.querySelector(`#${id}`).focus();

describe('Focus wrapping inside a surface', () => {
    beforeEach(() => {
        document.body.innerHTML = '<div id="surface"><button id="one">One</button><button id="two" disabled>Two</button><button id="three">Three</button></div><button id="after">After</button>';
    });

    const start = () => attach(document.querySelector('#surface'), { wrap: true });

    it('moves Tab to the next enabled control and wraps at the end', () => {
        start();
        focus('one');

        const first = tab();
        expect(document.activeElement.id).toBe('three');

        tab();
        expect(document.activeElement.id).toBe('one');
        expect(first.defaultPrevented).toBe(true);
    });

    it('moves Shift+Tab back and wraps at the start', () => {
        start();
        focus('one');

        tab(true);

        expect(document.activeElement.id).toBe('three');
    });

    it('leaves a key other than Tab alone', () => {
        start();
        focus('one');

        const event = new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true });
        document.activeElement.dispatchEvent(event);

        expect(event.defaultPrevented).toBe(false);
        expect(document.activeElement.id).toBe('one');
    });

    it('stops once disposed', () => {
        const handle = start();
        focus('one');
        handle.dispose();

        const event = tab();

        expect(event.defaultPrevented).toBe(false);
    });
});

describe('Focus arranged inside a surface', () => {
    beforeEach(() => {
        document.body.innerHTML = `
            <button id="before">Before</button>
            <div id="composer">
                <div class="toolbar"><button id="bold">Bold</button><button id="italic">Italic</button></div>
                <div class="canvas"><div id="editor" tabindex="0"></div><div class="bar" style="display: none"><button id="hidden">Hidden</button></div></div>
                <button id="status">Status</button>
            </div>
            <button id="after">After</button>
        `;
    });

    const start = () => attach(document.querySelector('#composer'), { order: ['.canvas'] });

    it('visits the named region first, then the rest in document order', () => {
        start();
        focus('editor');

        tab();
        expect(document.activeElement.id).toBe('bold');
        tab();
        expect(document.activeElement.id).toBe('italic');
        tab();
        expect(document.activeElement.id).toBe('status');
    });

    it('leaves for the control after the surface past the last one', () => {
        start();
        focus('status');

        const event = tab();

        expect(document.activeElement.id).toBe('after');
        expect(event.defaultPrevented).toBe(true);
    });

    it('leaves for the control before the surface ahead of the first one', () => {
        start();
        focus('editor');

        tab(true);

        expect(document.activeElement.id).toBe('before');
    });

    it('walks back from the rest into the named region', () => {
        start();
        focus('bold');

        tab(true);

        expect(document.activeElement.id).toBe('editor');
    });

    it('skips controls that are not displayed', () => {
        expect(focusablesIn(document.querySelector('#composer')).map(control => control.id)).toEqual(['bold', 'italic', 'editor', 'status']);
    });

    it('leaves a Tab a nested surface already answered alone', () => {
        start();
        focus('editor');
        document.querySelector('#editor').addEventListener('keydown', event => event.preventDefault());

        tab();

        expect(document.activeElement.id).toBe('editor');
    });

    it('stops arranging once disposed', () => {
        const handle = start();
        focus('editor');
        handle.dispose();

        const event = tab();

        expect(event.defaultPrevented).toBe(false);
    });
});

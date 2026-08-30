import { describe, it, expect, beforeEach } from 'vitest';
import { attach } from '#behaviours/FocusSequence.js';

describe('FocusSequence', () => {
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

    function tab(shiftKey = false) {
        const event = new KeyboardEvent('keydown', { key: 'Tab', shiftKey, bubbles: true, cancelable: true });
        document.activeElement.dispatchEvent(event);

        return event;
    }

    const focus = id => document.querySelector(`#${id}`).focus();

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

    it('leaves for the control after the container past the last one', () => {
        start();
        focus('status');

        const event = tab();

        expect(document.activeElement.id).toBe('after');
        expect(event.defaultPrevented).toBe(true);
    });

    it('leaves for the control before the container ahead of the first one', () => {
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
        start();
        focus('editor');

        tab();

        expect(document.activeElement.id).toBe('bold');
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

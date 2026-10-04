import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { attach } from '#browser/behaviors/FocusHandOff.js';

describe('FocusHandOff', () => {
    /** @type {HTMLElement} */
    let surface;
    let handle;

    beforeEach(() => {
        document.body.innerHTML = `
            <button id="outside">Outside</button>
            <div id="surface" tabindex="-1">
                <div id="first" tabindex="0">First</div>
                <div id="second" tabindex="-1">Second</div>
            </div>`;
        surface = document.querySelector('#surface');
    });

    afterEach(() => handle?.dispose());

    // A render's removal and its mark are seen together once the mutation observer has run.
    const settle = () => new Promise(resolve => setTimeout(resolve, 0));

    it('hands the focus to the marked element when the element holding it goes away', async () => {
        handle = attach(surface);
        document.querySelector('#first').focus();

        document.querySelector('#second').setAttribute('data-focus-heir', '');
        document.querySelector('#first').remove();
        await settle();

        expect(document.activeElement?.id).toBe('second');
    });

    it('hands the focus to the surface itself where the surface is marked', async () => {
        handle = attach(surface);
        document.querySelector('#first').focus();

        surface.setAttribute('data-focus-heir', '');
        document.querySelector('#first').remove();
        await settle();

        expect(document.activeElement).toBe(surface);
    });

    it('leaves a focus the reader took out of the surface where it went', async () => {
        handle = attach(surface);
        document.querySelector('#first').focus();
        document.querySelector('#outside').focus();

        document.querySelector('#second').setAttribute('data-focus-heir', '');
        document.querySelector('#first').remove();
        await settle();

        expect(document.activeElement?.id).toBe('outside');
    });

    it('hands nothing on while no element is marked', async () => {
        handle = attach(surface);
        document.querySelector('#first').focus();

        document.querySelector('#first').remove();
        await settle();

        expect(document.activeElement).toBe(document.body);
    });

    it('hands nothing on while the element holding the focus stays', async () => {
        handle = attach(surface);
        document.querySelector('#first').focus();

        document.querySelector('#second').setAttribute('data-focus-heir', '');
        await settle();

        expect(document.activeElement?.id).toBe('first');
    });

    it('honours the attribute its owner names', async () => {
        handle = attach(surface, { attribute: 'data-next' });
        document.querySelector('#first').focus();

        document.querySelector('#second').setAttribute('data-next', '');
        document.querySelector('#first').remove();
        await settle();

        expect(document.activeElement?.id).toBe('second');
    });

    it('stops handing on once disposed', async () => {
        handle = attach(surface);
        document.querySelector('#first').focus();
        handle.dispose();

        document.querySelector('#second').setAttribute('data-focus-heir', '');
        document.querySelector('#first').remove();
        await settle();

        expect(document.activeElement).toBe(document.body);
    });
});

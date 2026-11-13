import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { attach } from '#browser/behaviors/FileDropZone.js';
import { configure } from '#input/InputRouter.js';

describe('FileDropZone', () => {
    /** @type {HTMLElement} */
    let zone;
    /** @type {ReturnType<typeof attach> | null} */
    let handle;

    beforeEach(() => {
        document.body.innerHTML = '<label id="zone"><input type="file" /></label>';
        zone = document.querySelector('#zone');
        handle = null;
    });

    afterEach(() => {
        handle?.dispose();
        configure({ bindings: {} });
    });

    // The value a browser shows for a chosen file, which only an empty one can be set back to.
    const chooseFile = input => {
        let value = 'C:\\fakepath\\cover.jpg';
        Object.defineProperty(input, 'value', { get: () => value, set: next => { value = next; }, configurable: true });
    };

    it('empties its file input once cleared, so the same file chosen again is a change', () => {
        handle = attach(zone, { action: 'test.drop' });
        const input = zone.querySelector('input');
        chooseFile(input);

        handle.clear();

        expect(input.value).toBe('');
    });

    it('clears nothing without a file input', () => {
        zone.querySelector('input').remove();
        handle = attach(zone, { action: 'test.drop' });

        expect(() => handle.clear()).not.toThrow();
    });
});

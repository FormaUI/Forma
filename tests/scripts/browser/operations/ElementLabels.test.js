import { describe, it, expect, beforeEach } from 'vitest';
import { read } from '#browser/operations/ElementLabels.js';

describe('ElementLabels', () => {
    beforeEach(() => {
        document.body.innerHTML = `
            <button id="text"><svg aria-hidden="true"></svg> Rename
                <span>  this   file </span></button>
            <button id="labeled" aria-label="Close"><span>×</span></button>
            <button id="marked">Pin<span aria-hidden="true">✓</span></button>`;
    });

    const byId = id => document.getElementById(id);

    it('reads each element by its text, white space collapsed and what assistive technology skips left out', () => {
        expect(read([byId('text'), byId('marked')])).toEqual({ value: ['Rename this file', 'Pin'] });
    });

    it('reads an element by its own label where it carries one', () => {
        expect(read([byId('labeled')])).toEqual({ value: ['Close'] });
    });

    it('reads an element no longer in the document as empty', () => {
        const removed = byId('text');
        removed.remove();

        expect(read([removed, null, byId('marked')])).toEqual({ value: ['', '', 'Pin'] });
    });
});

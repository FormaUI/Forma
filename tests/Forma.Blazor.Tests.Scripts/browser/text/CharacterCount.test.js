import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { CharacterCount } from '#browser/text/CharacterCount.mjs';

const counts = [];

function count(options) {
    const characterCount = new CharacterCount(field(), options);
    counts.push(characterCount);

    return characterCount;
}

function type(value) {
    field().value = value;
    field().dispatchEvent(new Event('input', { bubbles: true }));
}

beforeEach(() => {
    document.body.innerHTML = `
        <textarea id="field"></textarea>
        <bdi id="count">0 / 280</bdi>`;
});

afterEach(() => {
    while (counts.length > 0) {
        counts.pop().dispose();
    }
});

const field = () => document.querySelector('#field');
const output = () => document.querySelector('#count');

describe('CharacterCount', () => {
    it('shows the count of what was typed beside the most the field accepts', () => {
        count({ output: 'count', maxLength: 280 });

        type('Hello');

        expect(output().textContent).toBe('5 / 280');
    });

    it('shows the count alone where the field accepts any length', () => {
        output().textContent = '0';
        count({ output: 'count', maxLength: null });

        type('Hello, world');

        expect(output().textContent).toBe('12');
    });

    it('counts UTF-16 code units, as the field\'s own limit does', () => {
        count({ output: 'count', maxLength: 280 });

        type('😀');

        expect(output().textContent).toBe('2 / 280');
    });

    it('writes into the output\'s own text, which the renderer that drew it goes on updating', () => {
        const text = output().firstChild;
        count({ output: 'count', maxLength: 280 });

        type('Hi');

        expect(output().firstChild).toBe(text);
        expect(text.nodeValue).toBe('2 / 280');
    });

    it('counts what was typed before it attached', () => {
        field().value = 'Early';

        count({ output: 'count', maxLength: 280 });

        expect(output().textContent).toBe('5 / 280');
    });

    it('stops counting once disposed', () => {
        count({ output: 'count', maxLength: 280 }).dispose();

        type('Hello');

        expect(output().textContent).toBe('0 / 280');
    });
});

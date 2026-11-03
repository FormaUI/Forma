import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { Transfer } from '#core/Transfer.js';

describe('Transfer', () => {
    const decoder = new TextDecoder();
    let createJSStreamReference;

    beforeEach(() => {
        createJSStreamReference = vi.fn(bytes => ({ bytes }));
        globalThis.DotNet = { createJSStreamReference };
    });

    afterEach(() => {
        delete globalThis.DotNet;
    });

    it('carries a value inline while it fits the budget', () => {
        expect(Transfer.of('a short text')).toEqual({ value: 'a short text' });
        expect(Transfer.of({ items: [1, 2, 3] })).toEqual({ value: { items: [1, 2, 3] } });
        expect(createJSStreamReference).not.toHaveBeenCalled();
    });

    it('carries nothing as null', () => {
        expect(Transfer.of(undefined)).toEqual({ value: null });
        expect(Transfer.of(null)).toEqual({ value: null });
    });

    it('streams a text past the budget as its own bytes', () => {
        const text = 'x'.repeat(Transfer.budget + 1);

        const transferred = Transfer.of(text);

        expect(transferred.text).toBe(true);
        expect(decoder.decode(transferred.stream.bytes)).toBe(text);
    });

    it('streams any other value past the budget as its JSON', () => {
        const items = Array.from({ length: Transfer.budget }, (_, index) => index);

        const transferred = Transfer.of(items);

        expect(transferred.text).toBe(false);
        expect(JSON.parse(decoder.decode(transferred.stream.bytes))).toEqual(items);
    });

    it('measures the budget on the value as the hub escapes it, twice', () => {
        // Each quote escapes to \" in the call's JSON and to \\\" once that JSON is a string inside the hub's message,
        // so a text of quotes a quarter of the budget long encodes past it.
        const quotes = '"'.repeat(Transfer.budget / 4 + 1);

        expect(Transfer.of(quotes).text).toBe(true);
        expect(Transfer.of('x'.repeat(Transfer.budget / 4 + 1))).toEqual({ value: 'x'.repeat(Transfer.budget / 4 + 1) });
    });

    it('measures the budget in UTF-8 bytes', () => {
        // A character of three UTF-8 bytes a third of the budget long fills it; one more passes it.
        const fits = '中'.repeat(Transfer.budget / 3 - 2);
        const passes = '中'.repeat(Transfer.budget / 3 + 1);

        expect(Transfer.of(fits)).toEqual({ value: fits });
        expect(Transfer.of(passes).text).toBe(true);
    });
});

import { describe, it, expect, beforeEach } from 'vitest';
import { sort } from '#browser/operations/DocumentOrder.js';

describe('DocumentOrder', () => {
    beforeEach(() => {
        document.body.innerHTML = '<ul><li id="a"></li><li id="b"><span id="c"></span></li><li id="d"></li></ul>';
    });

    const byId = id => document.getElementById(id);

    it('gives the places of the elements in the order the document holds them', () => {
        expect(sort([byId('d'), byId('a'), byId('c'), byId('b')])).toEqual({ value: [1, 3, 2, 0] });
    });

    it('puts the elements no longer in the document last, in the order given', () => {
        const removed = byId('b');
        removed.remove();

        expect(sort([removed, byId('d'), null, byId('a')])).toEqual({ value: [3, 1, 0, 2] });
    });
});

import { describe, it, expect, beforeEach } from 'vitest';
import { Direction } from '#common/core/Direction.js';

describe('Direction', () => {
    /** @type {HTMLElement} */
    let element;

    beforeEach(() => {
        document.body.innerHTML = '<div id="element">Text</div>';
        element = document.querySelector('#element');
    });

    it('reads a left-to-right element as not right to left', () => {
        expect(Direction.isRightToLeft(element)).toBe(false);
    });

    it('reads a right-to-left element as right to left', () => {
        element.style.direction = 'rtl';

        expect(Direction.isRightToLeft(element)).toBe(true);
    });
});

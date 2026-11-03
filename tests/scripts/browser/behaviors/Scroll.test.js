import { describe, it, expect, beforeEach, vi } from 'vitest';
import { toCenter } from '#browser/behaviors/Scroll.js';
import { sizeScroller } from '#support/platform.js';

describe('Scroll moving a container', () => {
    /** @type {HTMLElement} */
    let container;

    beforeEach(() => {
        document.body.innerHTML = '<div id="track"></div>';
        container = document.querySelector('#track');
        container.scrollBy = vi.fn();
    });

    describe('toCenter', () => {
        it('puts the middle of the content in view on both axes', () => {
            sizeScroller(container, { scrollWidth: 900, clientWidth: 300, scrollHeight: 500, clientHeight: 100 });

            toCenter(container);

            expect([container.scrollLeft, container.scrollTop]).toEqual([300, 200]);
        });

        it('leaves a container whose content fits at its origin', () => {
            sizeScroller(container, { scrollWidth: 300, clientWidth: 300, scrollHeight: 100, clientHeight: 100 });

            toCenter(container);

            expect([container.scrollLeft, container.scrollTop]).toEqual([0, 0]);
        });

        // A right-to-left container counts scrollLeft downwards from zero and clamps a positive position to its start.
        it('counts the inline middle downwards in a right-to-left container', () => {
            container.style.direction = 'rtl';
            sizeScroller(container, { scrollWidth: 900, clientWidth: 300, scrollHeight: 500, clientHeight: 100 });

            toCenter(container);

            expect([container.scrollLeft, container.scrollTop]).toEqual([-300, 200]);
        });
    });
});

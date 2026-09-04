import { describe, it, expect, beforeEach, vi } from 'vitest';
import { byPage, toCenter } from '#interop/operations/Scroll.js';
import { sizeScroller } from '#support/platform.js';

describe('Scroll', () => {
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
    });

    describe('byPage', () => {
        it('steps most of a page towards the end', () => {
            sizeScroller(container, { clientWidth: 500 });

            byPage(container, 1);

            expect(container.scrollBy).toHaveBeenCalledExactlyOnceWith({ left: 400, behavior: 'smooth' });
        });

        it('steps most of a page towards the start', () => {
            sizeScroller(container, { clientWidth: 500 });

            byPage(container, -1);

            expect(container.scrollBy).toHaveBeenCalledExactlyOnceWith({ left: -400, behavior: 'smooth' });
        });

        // A right-to-left container counts scrollLeft downwards from zero.
        it('mirrors the step in a right-to-left container', () => {
            container.style.direction = 'rtl';
            sizeScroller(container, { clientWidth: 500 });

            byPage(container, 1);

            expect(container.scrollBy).toHaveBeenCalledExactlyOnceWith({ left: -400, behavior: 'smooth' });
        });
    });
});

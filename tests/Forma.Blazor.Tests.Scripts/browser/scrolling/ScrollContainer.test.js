import { describe, it, expect, beforeEach, vi } from 'vitest';
import { ScrollContainer } from '#browser/scrolling/ScrollContainer.mjs';
import { sizeScroller } from '#support/platform.js';

describe('ScrollContainer', () => {
    /** @type {HTMLElement} */
    let container;

    beforeEach(() => {
        document.body.innerHTML = '<div id="track"></div>';
        container = document.querySelector('#track');
        container.scrollBy = vi.fn();
    });

    describe('stepping without stops', () => {
        it('moves most of a page towards the end', () => {
            sizeScroller(container, { clientWidth: 500 });

            new ScrollContainer(container).step(1);

            expect(container.scrollBy).toHaveBeenCalledExactlyOnceWith({ left: 400, behavior: 'smooth' });
        });

        it('moves most of a page towards the start', () => {
            sizeScroller(container, { clientWidth: 500 });

            new ScrollContainer(container).step(-1);

            expect(container.scrollBy).toHaveBeenCalledExactlyOnceWith({ left: -400, behavior: 'smooth' });
        });

        // A right-to-left container counts scrollLeft downwards from zero.
        it('mirrors the page in a right-to-left container', () => {
            container.style.direction = 'rtl';
            sizeScroller(container, { clientWidth: 500 });

            new ScrollContainer(container).step(1);

            expect(container.scrollBy).toHaveBeenCalledExactlyOnceWith({ left: -400, behavior: 'smooth' });
        });

        it('moves most of a page down along the block axis', () => {
            sizeScroller(container, { clientHeight: 400 });

            new ScrollContainer(container, 'block').step(1);

            expect(container.scrollBy).toHaveBeenCalledExactlyOnceWith({ top: 320, behavior: 'smooth' });
        });

        it('moves most of a page up along the block axis', () => {
            sizeScroller(container, { clientHeight: 400 });

            new ScrollContainer(container, 'block').step(-1);

            expect(container.scrollBy).toHaveBeenCalledExactlyOnceWith({ top: -320, behavior: 'smooth' });
        });

        it('falls back to a page when the selector matches nothing', () => {
            sizeScroller(container, { clientWidth: 500 });

            new ScrollContainer(container).step(1, { stops: '.stop' });

            expect(container.scrollBy).toHaveBeenCalledExactlyOnceWith({ left: 400, behavior: 'smooth' });
        });

        it('reports that it started moving', () => {
            sizeScroller(container, { clientWidth: 500 });

            expect(new ScrollContainer(container).step(1)).toBe(true);
        });
    });

    describe('stepping between stops', () => {
        // Three columns of 100px past a 120px frozen region, in a container scrolled to its start.
        const rect = (element, left, right) => { element.getBoundingClientRect = () => ({ left, right, top: 0, bottom: 40 }); };
        const options = { stops: '.stop', frozen: '.frozen' };

        const shiftStops = distance => {
            for (const stop of container.querySelectorAll('.stop')) {
                const { left, right } = stop.getBoundingClientRect();
                rect(stop, left - distance, right - distance);
            }
        };

        beforeEach(() => {
            container.innerHTML = '<span class="frozen" id="f"></span><span class="stop" id="a"></span><span class="stop" id="b"></span><span class="stop" id="c"></span><span class="stop" id="c2"></span>';
            container.scrollTo = vi.fn();
            rect(container, 0, 300);
            rect(container.querySelector('#f'), 0, 120);
            rect(container.querySelector('#a'), 120, 220);
            rect(container.querySelector('#b'), 220, 320);
            rect(container.querySelector('#c'), 320, 420);
            rect(container.querySelector('#c2'), 320, 420);
            sizeScroller(container, { scrollLeft: 0, scrollWidth: 420, clientWidth: 300 });
        });

        it('brings the next stop up to the frozen region', () => {
            new ScrollContainer(container).step(1, options);

            expect(container.scrollTo).toHaveBeenCalledExactlyOnceWith({ left: 100, behavior: 'smooth' });
        });

        it('measures the frozen region as the step runs', () => {
            rect(container.querySelector('#f'), 0, 70);

            new ScrollContainer(container).step(1, options);

            expect(container.scrollTo).toHaveBeenCalledExactlyOnceWith({ left: 50, behavior: 'smooth' });
        });

        it('brings each stop to the start where nothing is frozen', () => {
            new ScrollContainer(container).step(1, { stops: '.stop' });

            expect(container.scrollTo).toHaveBeenCalledExactlyOnceWith({ left: 120, behavior: 'smooth' });
        });

        it('brings the previous stop back to the frozen region', () => {
            sizeScroller(container, { scrollLeft: 200 });
            shiftStops(200);

            new ScrollContainer(container).step(-1, options);

            expect(container.scrollTo).toHaveBeenCalledExactlyOnceWith({ left: 100, behavior: 'smooth' });
        });

        it('goes on to the end past the last stop', () => {
            sizeScroller(container, { scrollLeft: 100, scrollWidth: 420, clientWidth: 300 });
            shiftStops(100);

            new ScrollContainer(container).step(1, options);

            expect(container.scrollTo).toHaveBeenCalledExactlyOnceWith({ left: 120, behavior: 'smooth' });
        });

        it('stays put at the end, reporting that it did not move', () => {
            sizeScroller(container, { scrollLeft: 120, scrollWidth: 420, clientWidth: 300 });
            shiftStops(120);

            const isMoving = new ScrollContainer(container).step(1, options);

            expect(isMoving).toBe(false);
            expect(container.scrollTo).not.toHaveBeenCalled();
        });

        it('comes back to the start from before the first stop', () => {
            sizeScroller(container, { scrollLeft: 50, scrollWidth: 420, clientWidth: 300 });
            shiftStops(50);

            new ScrollContainer(container).step(-1, options);

            expect(container.scrollTo).toHaveBeenCalledExactlyOnceWith({ left: 0, behavior: 'smooth' });
        });

        it('mirrors the stops in a right-to-left container', () => {
            container.style.direction = 'rtl';
            rect(container.querySelector('#f'), 180, 300);
            rect(container.querySelector('#a'), 80, 180);
            rect(container.querySelector('#b'), -20, 80);
            rect(container.querySelector('#c'), -120, -20);
            rect(container.querySelector('#c2'), -120, -20);

            new ScrollContainer(container).step(1, options);

            expect(container.scrollTo).toHaveBeenCalledExactlyOnceWith({ left: -100, behavior: 'smooth' });
        });

        it('steps down to the next stop along the block axis', () => {
            const row = (element, top, bottom) => { element.getBoundingClientRect = () => ({ left: 0, right: 300, top, bottom }); };
            row(container, 0, 300);
            row(container.querySelector('#f'), 0, 40);
            row(container.querySelector('#a'), 40, 140);
            row(container.querySelector('#b'), 140, 240);
            row(container.querySelector('#c'), 240, 340);
            row(container.querySelector('#c2'), 240, 340);
            sizeScroller(container, { scrollTop: 0, scrollHeight: 600, clientHeight: 300 });

            new ScrollContainer(container, 'block').step(1, options);

            expect(container.scrollTo).toHaveBeenCalledExactlyOnceWith({ top: 100, behavior: 'smooth' });
        });
    });

    describe('measuring', () => {
        it('takes where the content stands as one snapshot', () => {
            sizeScroller(container, { scrollLeft: 300, scrollWidth: 900, clientWidth: 300 });

            expect(new ScrollContainer(container).measure()).toEqual({ canScrollBack: true, canScrollForward: true, frozenExtent: 0, cornerExtent: 0, viewportExtent: null });
        });

        it('reads its visible extent only where it is asked for', () => {
            sizeScroller(container, { scrollTop: 0, scrollHeight: 900, clientHeight: 300, clientWidth: 50 });

            expect(new ScrollContainer(container, 'block').measure({ viewportExtent: true })).toEqual(expect.objectContaining({ canScrollForward: true, viewportExtent: 300 }));
        });

        it('reads a right-to-left offset the same as its mirror', () => {
            container.style.direction = 'rtl';
            sizeScroller(container, { scrollLeft: -600, scrollWidth: 900, clientWidth: 300 });

            expect(new ScrollContainer(container).measure()).toEqual(expect.objectContaining({ canScrollBack: true, canScrollForward: false }));
        });

        it('takes a sub-pixel remainder as nothing to scroll', () => {
            sizeScroller(container, { scrollLeft: 0, scrollWidth: 300.5, clientWidth: 300 });
            const scrollContainer = new ScrollContainer(container);

            expect(scrollContainer.isScrollable).toBe(false);
            expect(scrollContainer.measure()).toEqual(expect.objectContaining({ canScrollBack: false, canScrollForward: false }));
        });
    });

    describe('moving by a distance', () => {
        beforeEach(() => {
            container.scrollTo = vi.fn(({ left, top }) => {
                if (left !== undefined) container.scrollLeft = left;
                if (top !== undefined) container.scrollTop = top;
            });
            sizeScroller(container, { scrollLeft: 0, scrollWidth: 500, clientWidth: 200, scrollTop: 0, scrollHeight: 500, clientHeight: 200 });
        });

        it('moves the content at once toward its end', () => {
            const isMoving = new ScrollContainer(container).moveBy(120);

            expect(isMoving).toBe(true);
            expect(container.scrollTo).toHaveBeenCalledExactlyOnceWith({ left: 120, behavior: 'instant' });
        });

        it('stops at the end', () => {
            container.scrollLeft = 250;

            new ScrollContainer(container).moveBy(120);

            expect(container.scrollLeft).toBe(300);
        });

        it('reports that it did not move at the start', () => {
            expect(new ScrollContainer(container).moveBy(-120)).toBe(false);
            expect(container.scrollTo).not.toHaveBeenCalled();
        });

        it('mirrors the movement in a right-to-left container', () => {
            container.style.direction = 'rtl';

            new ScrollContainer(container).moveBy(120);

            expect(container.scrollLeft).toBe(-120);
        });

        it('moves along the block axis', () => {
            new ScrollContainer(container, 'block').moveBy(120);

            expect(container.scrollTop).toBe(120);
        });
    });
});

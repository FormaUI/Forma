import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { attach, step, toCenter } from '#browser/behaviors/Scroll.js';
import { fakeOwner } from '#support/owner.js';
import { sizeScroller, stubAnimationFrames, stubIntersectionObserver, stubMutationObserver, stubResizeObserver } from '#support/platform.js';

describe('Scroll watching a boundary', () => {
    /** @type {ReturnType<typeof stubAnimationFrames>} */
    let frames;
    /** @type {ReturnType<typeof stubResizeObserver>} */
    let resizes;
    /** @type {ReturnType<typeof stubMutationObserver>} */
    let mutations;
    /** @type {HTMLElement} */
    let track;
    /** @type {ReturnType<typeof fakeOwner>} */
    let owner;

    beforeEach(() => {
        frames = stubAnimationFrames();
        resizes = stubResizeObserver();
        mutations = stubMutationObserver();
        document.body.innerHTML = '<div id="track"><button>One</button><button>Two</button></div>';
        track = document.querySelector('#track');
        owner = fakeOwner();
    });

    afterEach(() => {
        frames.restore();
        resizes.restore();
        mutations.restore();
    });

    const reported = () => expect(owner.invokeMethodAsync);

    it('stays quiet once the track has left the document', () => {
        sizeScroller(track, { scrollLeft: 0, scrollWidth: 900, clientWidth: 300 });
        attach(track, { boundary: { owner } });
        frames.flush();

        track.remove();
        sizeScroller(track, { scrollLeft: 0, scrollWidth: 0, clientWidth: 0 });
        resizes.latest().deliver([]);
        frames.flush();

        expect(owner.invokeMethodAsync).toHaveBeenCalledTimes(1);
    });

    it('reports nothing scrollable when everything fits', () => {
        sizeScroller(track, { scrollLeft: 0, scrollWidth: 300, clientWidth: 300 });
        attach(track, { boundary: { owner } });

        frames.flush();

        reported().toHaveBeenLastCalledWith('HandleOverflowAsync', expect.objectContaining({ canScrollBack: false, canScrollForward: false, frozenExtent: 0, cornerExtent: 0 }));
    });

    it('reports how much of the track is in view', () => {
        sizeScroller(track, { scrollLeft: 0, scrollWidth: 900, clientWidth: 300 });
        attach(track, { boundary: { owner } });

        frames.flush();

        reported().toHaveBeenLastCalledWith('HandleOverflowAsync', expect.objectContaining({ viewportExtent: 300 }));
    });

    it('reports more ahead when the track starts at its beginning', () => {
        sizeScroller(track, { scrollLeft: 0, scrollWidth: 900, clientWidth: 300 });
        attach(track, { boundary: { owner } });

        frames.flush();

        reported().toHaveBeenLastCalledWith('HandleOverflowAsync', expect.objectContaining({ canScrollBack: false, canScrollForward: true, frozenExtent: 0, cornerExtent: 0 }));
    });

    it('reports both directions from the middle of the track', () => {
        sizeScroller(track, { scrollLeft: 300, scrollWidth: 900, clientWidth: 300 });
        attach(track, { boundary: { owner } });

        frames.flush();

        reported().toHaveBeenLastCalledWith('HandleOverflowAsync', expect.objectContaining({ canScrollBack: true, canScrollForward: true, frozenExtent: 0, cornerExtent: 0 }));
    });

    it('reports nothing ahead at the end of the track', () => {
        sizeScroller(track, { scrollLeft: 600, scrollWidth: 900, clientWidth: 300 });
        attach(track, { boundary: { owner } });

        frames.flush();

        reported().toHaveBeenLastCalledWith('HandleOverflowAsync', expect.objectContaining({ canScrollBack: true, canScrollForward: false, frozenExtent: 0, cornerExtent: 0 }));
    });

    // A right-to-left track counts scrollLeft downwards from zero.
    it('reads a right-to-left offset the same as its mirror', () => {
        sizeScroller(track, { scrollLeft: -300, scrollWidth: 900, clientWidth: 300 });
        attach(track, { boundary: { owner } });

        frames.flush();

        reported().toHaveBeenLastCalledWith('HandleOverflowAsync', expect.objectContaining({ canScrollBack: true, canScrollForward: true, frozenExtent: 0, cornerExtent: 0 }));
    });

    it('treats a sub-pixel remainder as nothing to scroll', () => {
        sizeScroller(track, { scrollLeft: 0.5, scrollWidth: 300.5, clientWidth: 300 });
        attach(track, { boundary: { owner } });

        frames.flush();

        reported().toHaveBeenLastCalledWith('HandleOverflowAsync', expect.objectContaining({ canScrollBack: false, canScrollForward: false, frozenExtent: 0, cornerExtent: 0 }));
    });

    it('coalesces a burst of scrolling into one report', () => {
        sizeScroller(track, { scrollLeft: 0, scrollWidth: 900, clientWidth: 300 });
        attach(track, { boundary: { owner } });
        frames.flush();

        track.scrollLeft = 300;
        track.dispatchEvent(new Event('scroll'));
        track.dispatchEvent(new Event('scroll'));
        track.dispatchEvent(new Event('scroll'));
        frames.flush();

        reported().toHaveBeenCalledTimes(2);
    });

    it('stays quiet while the answer is unchanged', () => {
        sizeScroller(track, { scrollLeft: 0, scrollWidth: 900, clientWidth: 300 });
        attach(track, { boundary: { owner } });
        frames.flush();

        track.scrollLeft = 0.5;
        track.dispatchEvent(new Event('scroll'));
        frames.flush();

        reported().toHaveBeenCalledTimes(1);
    });

    it('watches each command alongside the track', () => {
        attach(track, { boundary: { owner } });

        expect(resizes.latest().targets).toHaveLength(3);
    });

    it('watches a command added after it attached', () => {
        attach(track, { boundary: { owner } });

        track.appendChild(document.createElement('button'));
        mutations.latest().deliver([]);

        expect(resizes.latest().targets).toHaveLength(4);
    });

    it('re-measures when the track resizes', () => {
        sizeScroller(track, { scrollLeft: 0, scrollWidth: 300, clientWidth: 300 });
        attach(track, { boundary: { owner } });
        frames.flush();

        sizeScroller(track, { scrollWidth: 900 });
        resizes.latest().deliver([]);
        frames.flush();

        reported().toHaveBeenLastCalledWith('HandleOverflowAsync', expect.objectContaining({ canScrollBack: false, canScrollForward: true, frozenExtent: 0, cornerExtent: 0 }));
    });

    it('stops watching once disposed', () => {
        const handle = attach(track, { boundary: { owner } });

        handle.dispose();

        expect(resizes.latest().isConnected).toBe(false);
        expect(mutations.latest().isConnected).toBe(false);
    });

    // The coalesced report is queued on a frame before dispose tears the listeners down.
    it('drops a report queued before disposal', () => {
        sizeScroller(track, { scrollLeft: 0, scrollWidth: 900, clientWidth: 300 });
        const handle = attach(track, { boundary: { owner } });

        handle.dispose();
        frames.flush();

        reported().not.toHaveBeenCalled();
    });

    it('queues nothing once disposed', () => {
        const handle = attach(track, { boundary: { owner } });
        handle.dispose();
        frames.flush();

        track.dispatchEvent(new Event('scroll'));

        expect(frames.pending).toBe(0);
    });

    describe('along the block axis', () => {
        it('reads the offset and slack from the top rather than the side', () => {
            sizeScroller(track, { scrollTop: 100, scrollHeight: 600, clientHeight: 200, scrollLeft: 0, scrollWidth: 200, clientWidth: 200 });
            attach(track, { boundary: { owner, axis: 'block' } });
            frames.flush();

            reported().toHaveBeenLastCalledWith('HandleOverflowAsync', expect.objectContaining({ canScrollBack: true, canScrollForward: true, frozenExtent: 0, cornerExtent: 0 }));
        });

        it('reports nothing ahead at the bottom', () => {
            sizeScroller(track, { scrollTop: 400, scrollHeight: 600, clientHeight: 200 });
            attach(track, { boundary: { owner, axis: 'block' } });
            frames.flush();

            reported().toHaveBeenLastCalledWith('HandleOverflowAsync', expect.objectContaining({ canScrollBack: true, canScrollForward: false, frozenExtent: 0, cornerExtent: 0 }));
        });
    });

    describe('with frozen parts', () => {
        const rect = (element, values) => {
            element.getBoundingClientRect = () => ({ left: 0, right: 0, top: 0, bottom: 0, ...values });
        };

        it('reports how far the frozen parts reach in from the start', () => {
            document.body.innerHTML = '<div id="track"><span class="pinned">A</span><span class="pinned">B</span><span>C</span></div>';
            track = document.querySelector('#track');
            rect(track, { left: 10, right: 310, top: 0, bottom: 40 });
            const [first, second] = track.querySelectorAll('.pinned');
            rect(first, { left: 10, right: 50 });
            rect(second, { left: 50, right: 130 });
            sizeScroller(track, { scrollLeft: 0, scrollWidth: 900, clientWidth: 300 });

            attach(track, { boundary: { owner, frozen: '.pinned' } });
            frames.flush();

            reported().toHaveBeenLastCalledWith('HandleOverflowAsync', expect.objectContaining({ canScrollBack: false, canScrollForward: true, frozenExtent: 120, cornerExtent: 0 }));
        });

        it('reports how far the frozen corner reaches across the axis', () => {
            document.body.innerHTML = '<div id="track"><span class="pinned">Header</span><span class="pinned">Cell</span><span>C</span></div>';
            track = document.querySelector('#track');
            rect(track, { left: 0, right: 300, top: 0, bottom: 200 });
            const [header, cell] = track.querySelectorAll('.pinned');
            rect(header, { left: 0, right: 60, top: 0, bottom: 32 });
            rect(cell, { left: 0, right: 60, top: 32, bottom: 64 });
            sizeScroller(track, { scrollLeft: 0, scrollWidth: 900, clientWidth: 300 });

            attach(track, { boundary: { owner, frozen: '.pinned' } });
            frames.flush();

            reported().toHaveBeenLastCalledWith('HandleOverflowAsync', expect.objectContaining({ canScrollBack: false, canScrollForward: true, frozenExtent: 60, cornerExtent: 32 }));
        });

        it('measures a right-to-left track from its right edge', () => {
            document.body.innerHTML = '<div id="track" style="direction: rtl"><span class="pinned">A</span><span>B</span></div>';
            track = document.querySelector('#track');
            rect(track, { left: 10, right: 310 });
            rect(track.querySelector('.pinned'), { left: 250, right: 310 });
            sizeScroller(track, { scrollLeft: 0, scrollWidth: 900, clientWidth: 300 });

            attach(track, { boundary: { owner, frozen: '.pinned' } });
            frames.flush();

            reported().toHaveBeenLastCalledWith('HandleOverflowAsync', expect.objectContaining({ canScrollBack: false, canScrollForward: true, frozenExtent: 60, cornerExtent: 0 }));
        });

        it('measures a frozen header row from the top', () => {
            document.body.innerHTML = '<div id="track"><div class="pinned">Header</div><div>Row</div></div>';
            track = document.querySelector('#track');
            rect(track, { top: 100, bottom: 400 });
            rect(track.querySelector('.pinned'), { top: 100, bottom: 132 });
            sizeScroller(track, { scrollTop: 0, scrollHeight: 900, clientHeight: 300 });

            attach(track, { boundary: { owner, axis: 'block', frozen: '.pinned' } });
            frames.flush();

            reported().toHaveBeenLastCalledWith('HandleOverflowAsync', expect.objectContaining({ canScrollBack: false, canScrollForward: true, frozenExtent: 32, cornerExtent: 0 }));
        });

        it('reports again when only the frozen extent changes', () => {
            document.body.innerHTML = '<div id="track"><span class="pinned">A</span><span>B</span></div>';
            track = document.querySelector('#track');
            rect(track, { left: 0, right: 300 });
            const pinned = track.querySelector('.pinned');
            rect(pinned, { left: 0, right: 40 });
            sizeScroller(track, { scrollLeft: 0, scrollWidth: 900, clientWidth: 300 });
            attach(track, { boundary: { owner, frozen: '.pinned' } });
            frames.flush();

            rect(pinned, { left: 0, right: 80 });
            track.dispatchEvent(new Event('scroll'));
            frames.flush();

            expect(owner.invokeMethodAsync).toHaveBeenCalledTimes(2);
            reported().toHaveBeenLastCalledWith('HandleOverflowAsync', expect.objectContaining({ canScrollBack: false, canScrollForward: true, frozenExtent: 80, cornerExtent: 0 }));
        });
    });
});

describe('Scroll watching a sentinel', () => {
    /** @type {ReturnType<typeof stubIntersectionObserver>} */
    let observers;
    /** @type {HTMLElement} */
    let sentinel;

    beforeEach(() => {
        observers = stubIntersectionObserver();
        document.body.innerHTML = '<div id="panel"><div id="sentinel"></div></div>';
        sentinel = document.querySelector('#sentinel');
    });

    afterEach(() => observers.restore());

    it('watches the sentinel it was given', () => {
        attach(sentinel, { sentinel: { owner: fakeOwner() } });

        expect(observers.latest().targets).toEqual([sentinel]);
    });

    it('asks for the next page once the sentinel comes into view', () => {
        const owner = fakeOwner();
        attach(sentinel, { sentinel: { owner } });

        observers.latest().deliver([{ isIntersecting: true }]);

        expect(owner.invokeMethodAsync).toHaveBeenCalledExactlyOnceWith('HandleReachedAsync');
    });

    it('stays quiet while the sentinel is out of view', () => {
        const owner = fakeOwner();
        attach(sentinel, { sentinel: { owner } });

        observers.latest().deliver([{ isIntersecting: false }]);

        expect(owner.invokeMethodAsync).not.toHaveBeenCalled();
    });

    it('measures against the scrolling panel when one is given', () => {
        const panel = document.querySelector('#panel');
        attach(sentinel, { sentinel: { owner: fakeOwner(), root: panel } });

        expect(observers.latest().options.root).toBe(panel);
    });

    it('reaches ahead of the sentinel by default', () => {
        attach(sentinel, { sentinel: { owner: fakeOwner() } });

        expect(observers.latest().options.rootMargin).toBe('200px');
    });

    it('stops watching once disposed', () => {
        const handle = attach(sentinel, { sentinel: { owner: fakeOwner() } });

        handle.dispose();

        expect(observers.latest().isConnected).toBe(false);
    });

    // An observation queued before disconnect is delivered after it.
    it('ignores an observation delivered after disposal', () => {
        const owner = fakeOwner();
        const handle = attach(sentinel, { sentinel: { owner } });

        handle.dispose();
        observers.latest().deliver([{ isIntersecting: true }]);

        expect(owner.invokeMethodAsync).not.toHaveBeenCalled();
    });
});

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

    describe('step without stops', () => {
        it('moves most of a page towards the end', () => {
            sizeScroller(container, { clientWidth: 500 });

            step(container, 1);

            expect(container.scrollBy).toHaveBeenCalledExactlyOnceWith({ left: 400, behavior: 'smooth' });
        });

        it('moves most of a page towards the start', () => {
            sizeScroller(container, { clientWidth: 500 });

            step(container, -1);

            expect(container.scrollBy).toHaveBeenCalledExactlyOnceWith({ left: -400, behavior: 'smooth' });
        });

        // A right-to-left container counts scrollLeft downwards from zero.
        it('mirrors the page in a right-to-left container', () => {
            container.style.direction = 'rtl';
            sizeScroller(container, { clientWidth: 500 });

            step(container, 1);

            expect(container.scrollBy).toHaveBeenCalledExactlyOnceWith({ left: -400, behavior: 'smooth' });
        });

        it('moves most of a page down along the block axis', () => {
            sizeScroller(container, { clientHeight: 400 });

            step(container, 1, 'block');

            expect(container.scrollBy).toHaveBeenCalledExactlyOnceWith({ top: 320, behavior: 'smooth' });
        });

        it('moves most of a page up along the block axis', () => {
            sizeScroller(container, { clientHeight: 400 });

            step(container, -1, 'block');

            expect(container.scrollBy).toHaveBeenCalledExactlyOnceWith({ top: -320, behavior: 'smooth' });
        });

        it('falls back to a page when the selector matches nothing', () => {
            sizeScroller(container, { clientWidth: 500 });

            step(container, 1, 'inline', { selector: '.stop', inset: 0 });

            expect(container.scrollBy).toHaveBeenCalledExactlyOnceWith({ left: 400, behavior: 'smooth' });
        });
    });

    describe('step between stops', () => {
        // Three columns of 100px past a 120px frozen region, in a container scrolled to its start.
        const rect = (element, left, right) => { element.getBoundingClientRect = () => ({ left, right, top: 0, bottom: 40 }); };

        beforeEach(() => {
            container.innerHTML = '<span class="stop" id="a"></span><span class="stop" id="b"></span><span class="stop" id="c"></span><span class="stop" id="c2"></span>';
            container.scrollTo = vi.fn();
            rect(container, 0, 300);
            rect(container.querySelector('#a'), 120, 220);
            rect(container.querySelector('#b'), 220, 320);
            rect(container.querySelector('#c'), 320, 420);
            rect(container.querySelector('#c2'), 320, 420);
            sizeScroller(container, { scrollLeft: 0, scrollWidth: 420, clientWidth: 300 });
        });

        it('brings the next stop up to the inset', () => {
            step(container, 1, 'inline', { selector: '.stop', inset: 120 });

            expect(container.scrollTo).toHaveBeenCalledExactlyOnceWith({ left: 100, behavior: 'smooth' });
        });

        it('brings the previous stop back to the inset', () => {
            sizeScroller(container, { scrollLeft: 200 });
            for (const stop of container.querySelectorAll('.stop')) {
                const { left, right } = stop.getBoundingClientRect();
                rect(stop, left - 200, right - 200);
            }

            step(container, -1, 'inline', { selector: '.stop', inset: 120 });

            expect(container.scrollTo).toHaveBeenCalledExactlyOnceWith({ left: 100, behavior: 'smooth' });
        });

        it('goes on to the end past the last stop', () => {
            sizeScroller(container, { scrollLeft: 100, scrollWidth: 420, clientWidth: 300 });
            for (const stop of container.querySelectorAll('.stop')) {
                const { left, right } = stop.getBoundingClientRect();
                rect(stop, left - 100, right - 100);
            }

            step(container, 1, 'inline', { selector: '.stop', inset: 120 });

            expect(container.scrollTo).toHaveBeenCalledExactlyOnceWith({ left: 120, behavior: 'smooth' });
        });

        it('stays put at the end', () => {
            sizeScroller(container, { scrollLeft: 120, scrollWidth: 420, clientWidth: 300 });
            for (const stop of container.querySelectorAll('.stop')) {
                const { left, right } = stop.getBoundingClientRect();
                rect(stop, left - 120, right - 120);
            }

            step(container, 1, 'inline', { selector: '.stop', inset: 120 });

            expect(container.scrollTo).not.toHaveBeenCalled();
        });

        it('comes back to the start from before the first stop', () => {
            sizeScroller(container, { scrollLeft: 50, scrollWidth: 420, clientWidth: 300 });
            for (const stop of container.querySelectorAll('.stop')) {
                const { left, right } = stop.getBoundingClientRect();
                rect(stop, left - 50, right - 50);
            }

            step(container, -1, 'inline', { selector: '.stop', inset: 120 });

            expect(container.scrollTo).toHaveBeenCalledExactlyOnceWith({ left: 0, behavior: 'smooth' });
        });

        it('mirrors the stops in a right-to-left container', () => {
            container.style.direction = 'rtl';
            rect(container.querySelector('#a'), 80, 180);
            rect(container.querySelector('#b'), -20, 80);
            rect(container.querySelector('#c'), -120, -20);
            rect(container.querySelector('#c2'), -120, -20);

            step(container, 1, 'inline', { selector: '.stop', inset: 120 });

            expect(container.scrollTo).toHaveBeenCalledExactlyOnceWith({ left: -100, behavior: 'smooth' });
        });
    });
});

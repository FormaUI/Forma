import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { ScrollStateObserver } from '#browser/scrolling/ScrollStateObserver.mjs';
import { sizeScroller, stubAnimationFrames, stubMutationObserver, stubResizeObserver } from '#support/platform.js';

describe('ScrollStateObserver', () => {
    /** @type {ReturnType<typeof stubAnimationFrames>} */
    let frames;
    /** @type {ReturnType<typeof stubResizeObserver>} */
    let resizes;
    /** @type {ReturnType<typeof stubMutationObserver>} */
    let mutations;
    /** @type {HTMLElement} */
    let track;
    /** @type {ReturnType<typeof vi.fn>} */
    let publish;
    /** @type {ScrollStateObserver[]} */
    let sources;

    beforeEach(() => {
        frames = stubAnimationFrames();
        resizes = stubResizeObserver();
        mutations = stubMutationObserver();
        document.body.innerHTML = '<div id="track"><button>One</button><button>Two</button></div>';
        track = document.querySelector('#track');
        publish = vi.fn();
        sources = [];
    });

    afterEach(() => {
        for (const source of sources) {
            source.dispose();
        }

        frames.restore();
        resizes.restore();
        mutations.restore();
    });

    const watch = (options = {}) => {
        const source = new ScrollStateObserver(track, options, publish);
        sources.push(source);

        return source;
    };

    it('reads nothing scrollable when everything fits', () => {
        sizeScroller(track, { scrollLeft: 0, scrollWidth: 300, clientWidth: 300 });

        expect(watch().read()).toEqual({ canScrollBack: false, canScrollForward: false, frozenExtent: 0, cornerExtent: 0, viewportExtent: null });
    });

    it('reads how much of the track is in view where it is asked for', () => {
        sizeScroller(track, { scrollLeft: 0, scrollWidth: 900, clientWidth: 300 });

        expect(watch({ viewportExtent: true }).read()).toEqual(expect.objectContaining({ viewportExtent: 300 }));
    });

    it('reads more ahead when the track starts at its beginning', () => {
        sizeScroller(track, { scrollLeft: 0, scrollWidth: 900, clientWidth: 300 });

        expect(watch().read()).toEqual(expect.objectContaining({ canScrollBack: false, canScrollForward: true }));
    });

    it('reads both directions from the middle of the track', () => {
        sizeScroller(track, { scrollLeft: 300, scrollWidth: 900, clientWidth: 300 });

        expect(watch().read()).toEqual(expect.objectContaining({ canScrollBack: true, canScrollForward: true }));
    });

    it('reads nothing ahead at the end of the track', () => {
        sizeScroller(track, { scrollLeft: 600, scrollWidth: 900, clientWidth: 300 });

        expect(watch().read()).toEqual(expect.objectContaining({ canScrollBack: true, canScrollForward: false }));
    });

    // A right-to-left track counts scrollLeft downwards from zero.
    it('reads a right-to-left offset the same as its mirror', () => {
        sizeScroller(track, { scrollLeft: -300, scrollWidth: 900, clientWidth: 300 });

        expect(watch().read()).toEqual(expect.objectContaining({ canScrollBack: true, canScrollForward: true }));
    });

    it('treats a sub-pixel remainder as nothing to scroll', () => {
        sizeScroller(track, { scrollLeft: 0.5, scrollWidth: 300.5, clientWidth: 300 });

        expect(watch().read()).toEqual(expect.objectContaining({ canScrollBack: false, canScrollForward: false }));
    });

    it('publishes nothing until the state changes from the one read', () => {
        sizeScroller(track, { scrollLeft: 0, scrollWidth: 900, clientWidth: 300 });
        watch().read();

        track.dispatchEvent(new Event('scroll'));
        frames.flush();

        expect(publish).not.toHaveBeenCalled();
    });

    it('coalesces a burst of scrolling into one publication', () => {
        sizeScroller(track, { scrollLeft: 0, scrollWidth: 900, clientWidth: 300 });
        watch().read();

        track.scrollLeft = 300;
        track.dispatchEvent(new Event('scroll'));
        track.dispatchEvent(new Event('scroll'));
        track.dispatchEvent(new Event('scroll'));
        frames.flush();

        expect(publish).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ canScrollBack: true, canScrollForward: true }));
    });

    it('stays quiet while the answer is unchanged', () => {
        sizeScroller(track, { scrollLeft: 0, scrollWidth: 900, clientWidth: 300 });
        watch().read();

        track.scrollLeft = 0.5;
        track.dispatchEvent(new Event('scroll'));
        frames.flush();

        expect(publish).not.toHaveBeenCalled();
    });

    it('publishes the next measurement after a refresh, unchanged or not', () => {
        sizeScroller(track, { scrollLeft: 0, scrollWidth: 900, clientWidth: 300 });
        const source = watch();
        source.read();

        source.refresh();
        frames.flush();

        expect(publish).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ canScrollBack: false, canScrollForward: true }));
    });

    it('watches each child alongside the track through the shared observer', () => {
        watch();

        expect(resizes.latest().targets).toHaveLength(3);
    });

    it('shares one resize observer among every track', () => {
        document.body.innerHTML += '<div id="other"></div>';
        watch();
        sources.push(new ScrollStateObserver(document.querySelector('#other'), {}, publish));

        expect(resizes.instances).toHaveLength(1);
    });

    it('watches a child added after it was made', () => {
        watch();

        track.appendChild(document.createElement('button'));
        mutations.latest().deliver([]);

        expect(resizes.latest().targets).toHaveLength(4);
    });

    it('measures again when the track resizes', () => {
        sizeScroller(track, { scrollLeft: 0, scrollWidth: 300, clientWidth: 300 });
        watch().read();

        sizeScroller(track, { scrollWidth: 900 });
        resizes.latest().deliver([{ target: track }]);
        frames.flush();

        expect(publish).toHaveBeenLastCalledWith(expect.objectContaining({ canScrollBack: false, canScrollForward: true }));
    });

    it('stays quiet once the track has left the document', () => {
        sizeScroller(track, { scrollLeft: 0, scrollWidth: 900, clientWidth: 300 });
        watch().read();

        track.remove();
        sizeScroller(track, { scrollLeft: 0, scrollWidth: 0, clientWidth: 0 });
        resizes.latest().deliver([{ target: track }]);
        frames.flush();

        expect(publish).not.toHaveBeenCalled();
    });

    it('stops watching once disposed', () => {
        const source = watch();

        source.dispose();

        expect(resizes.latest().isConnected).toBe(false);
        expect(mutations.latest().isConnected).toBe(false);
    });

    // The coalesced measurement is queued on a frame before dispose tears the listeners down.
    it('drops a measurement queued before disposal', () => {
        sizeScroller(track, { scrollLeft: 0, scrollWidth: 900, clientWidth: 300 });
        const source = watch();
        track.dispatchEvent(new Event('scroll'));

        source.dispose();
        frames.flush();

        expect(publish).not.toHaveBeenCalled();
    });

    it('queues nothing once disposed', () => {
        const source = watch();
        source.dispose();

        track.dispatchEvent(new Event('scroll'));

        expect(frames.pending).toBe(0);
    });

    describe('along the block axis', () => {
        it('reads the offset and slack from the top rather than the side', () => {
            sizeScroller(track, { scrollTop: 100, scrollHeight: 600, clientHeight: 200, scrollLeft: 0, scrollWidth: 200, clientWidth: 200 });

            expect(watch({ axis: 'block' }).read()).toEqual(expect.objectContaining({ canScrollBack: true, canScrollForward: true }));
        });

        it('reads nothing ahead at the bottom', () => {
            sizeScroller(track, { scrollTop: 400, scrollHeight: 600, clientHeight: 200 });

            expect(watch({ axis: 'block' }).read()).toEqual(expect.objectContaining({ canScrollBack: true, canScrollForward: false }));
        });

        it('reads its visible height as the extent in view', () => {
            sizeScroller(track, { scrollTop: 0, scrollHeight: 600, clientHeight: 200, clientWidth: 50 });

            expect(watch({ axis: 'block', viewportExtent: true }).read()).toEqual(expect.objectContaining({ viewportExtent: 200 }));
        });
    });

    describe('with frozen parts', () => {
        const rect = (element, values) => {
            element.getBoundingClientRect = () => ({ left: 0, right: 0, top: 0, bottom: 0, ...values });
        };

        it('reads how far the frozen parts reach in from the start', () => {
            document.body.innerHTML = '<div id="track"><span class="pinned">A</span><span class="pinned">B</span><span>C</span></div>';
            track = document.querySelector('#track');
            rect(track, { left: 10, right: 310, top: 0, bottom: 40 });
            const [first, second] = track.querySelectorAll('.pinned');
            rect(first, { left: 10, right: 50 });
            rect(second, { left: 50, right: 130 });
            sizeScroller(track, { scrollLeft: 0, scrollWidth: 900, clientWidth: 300 });

            expect(watch({ frozenSelector: '.pinned' }).read()).toEqual(expect.objectContaining({ frozenExtent: 120, cornerExtent: 0 }));
        });

        it('reads how far the frozen corner reaches across the axis', () => {
            document.body.innerHTML = '<div id="track"><span class="pinned">Header</span><span class="pinned">Cell</span><span>C</span></div>';
            track = document.querySelector('#track');
            rect(track, { left: 0, right: 300, top: 0, bottom: 200 });
            const [header, cell] = track.querySelectorAll('.pinned');
            rect(header, { left: 0, right: 60, top: 0, bottom: 32 });
            rect(cell, { left: 0, right: 60, top: 32, bottom: 64 });
            sizeScroller(track, { scrollLeft: 0, scrollWidth: 900, clientWidth: 300 });

            expect(watch({ frozenSelector: '.pinned' }).read()).toEqual(expect.objectContaining({ frozenExtent: 60, cornerExtent: 32 }));
        });

        it('measures a right-to-left track from its right edge', () => {
            document.body.innerHTML = '<div id="track" style="direction: rtl"><span class="pinned">A</span><span>B</span></div>';
            track = document.querySelector('#track');
            rect(track, { left: 10, right: 310 });
            rect(track.querySelector('.pinned'), { left: 250, right: 310 });
            sizeScroller(track, { scrollLeft: 0, scrollWidth: 900, clientWidth: 300 });

            expect(watch({ frozenSelector: '.pinned' }).read()).toEqual(expect.objectContaining({ frozenExtent: 60, cornerExtent: 0 }));
        });

        it('measures a frozen header row from the top', () => {
            document.body.innerHTML = '<div id="track"><div class="pinned">Header</div><div>Row</div></div>';
            track = document.querySelector('#track');
            rect(track, { top: 100, bottom: 400 });
            rect(track.querySelector('.pinned'), { top: 100, bottom: 132 });
            sizeScroller(track, { scrollTop: 0, scrollHeight: 900, clientHeight: 300 });

            expect(watch({ axis: 'block', frozenSelector: '.pinned' }).read()).toEqual(expect.objectContaining({ frozenExtent: 32, cornerExtent: 0 }));
        });

        it('publishes again when only the frozen extent changes', () => {
            document.body.innerHTML = '<div id="track"><span class="pinned">A</span><span>B</span></div>';
            track = document.querySelector('#track');
            rect(track, { left: 0, right: 300 });
            const pinned = track.querySelector('.pinned');
            rect(pinned, { left: 0, right: 40 });
            sizeScroller(track, { scrollLeft: 0, scrollWidth: 900, clientWidth: 300 });
            watch({ frozenSelector: '.pinned' }).read();

            rect(pinned, { left: 0, right: 80 });
            track.dispatchEvent(new Event('scroll'));
            frames.flush();

            expect(publish).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ frozenExtent: 80, cornerExtent: 0 }));
        });

        it('measures again when a frozen part moves by a change of its style, which resizes nothing', () => {
            document.body.innerHTML = '<div id="track"><span class="pinned">A</span><span class="pinned">B</span><span>C</span></div>';
            track = document.querySelector('#track');
            rect(track, { left: 0, right: 300 });
            const [, second] = track.querySelectorAll('.pinned');
            rect(second, { left: 32, right: 219 });
            sizeScroller(track, { scrollLeft: 0, scrollWidth: 900, clientWidth: 300 });
            watch({ frozenSelector: '.pinned' }).read();

            rect(second, { left: 48, right: 235 });
            mutations.latest().deliver([{ type: 'attributes', target: second, attributeName: 'style' }]);
            frames.flush();

            expect(publish).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ frozenExtent: 235 }));
        });

        it('watches no styles where nothing is frozen', () => {
            watch();

            expect(mutations.instances).toHaveLength(1);
        });
    });
});

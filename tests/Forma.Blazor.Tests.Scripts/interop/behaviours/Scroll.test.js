import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { attach, step, toCenter } from '#interop/behaviours/Scroll.js';
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

    // Commands can be added or removed without the track's own box changing.
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

    // Its coalesced report was already queued against listeners that dispose only tore down.
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

describe('Scroll answering a wheel', () => {
    /** @type {HTMLElement} */
    let track;

    beforeEach(() => {
        document.body.innerHTML = '<div id="track"><button>One</button><button>Two</button></div>';
        track = document.querySelector('#track');
        track.scrollLeft = 0;
    });

    /** A track wider than its box, so there is something to scroll along. */
    function overflowing({ scrollWidth = 500, clientWidth = 200 } = {}) {
        sizeScroller(track, { scrollWidth, clientWidth });
    }

    // The test DOM drops the modifier keys a browser puts on a wheel event, so a held key is defined on the event itself.
    function wheel({ ctrlKey, shiftKey, ...properties }) {
        const event = new WheelEvent('wheel', { bubbles: true, cancelable: true, ...properties });

        if (ctrlKey !== undefined) {
            Object.defineProperty(event, 'ctrlKey', { value: ctrlKey });
        }

        if (shiftKey !== undefined) {
            Object.defineProperty(event, 'shiftKey', { value: shiftKey });
        }

        track.dispatchEvent(event);

        return event;
    }

    it('scrolls the track along when the wheel reports down', () => {
        overflowing();
        attach(track, { wheel: {} });

        const event = wheel({ deltaY: 120 });

        expect(track.scrollLeft).toBe(120);
        expect(event.defaultPrevented).toBe(true);
    });

    it('scrolls back when the wheel reports up', () => {
        overflowing();
        track.scrollLeft = 200;
        attach(track, { wheel: {} });

        wheel({ deltaY: -120 });

        expect(track.scrollLeft).toBe(80);
    });

    it('keeps the turn from the page even once the track has nothing left to give', () => {
        overflowing();
        track.scrollLeft = 300;
        attach(track, { wheel: {} });

        const event = wheel({ deltaY: 120 });

        // 500 - 200 is the whole slack, so the track is already at its end; the page still stays put.
        expect(track.scrollLeft).toBe(300);
        expect(event.defaultPrevented).toBe(true);
    });

    it('leaves the turn to the page when nothing overflows', () => {
        sizeScroller(track, { scrollWidth: 200, clientWidth: 200 });
        attach(track, { wheel: {} });

        const event = wheel({ deltaY: 120 });

        expect(track.scrollLeft).toBe(0);
        expect(event.defaultPrevented).toBe(false);
    });

    it('leaves a sideways turn alone, which the browser already scrolls', () => {
        overflowing();
        attach(track, { wheel: {} });

        const event = wheel({ deltaX: 40, deltaY: 0 });

        expect(event.defaultPrevented).toBe(false);
    });

    it('reads a wheel that reports in lines rather than pixels', () => {
        overflowing();
        attach(track, { wheel: {} });

        wheel({ deltaY: 3, deltaMode: WheelEvent.DOM_DELTA_LINE });

        expect(track.scrollLeft).toBe(48);
    });

    it('answers a turn over the bar around the track, moving the track', () => {
        document.body.innerHTML = '<div id="bar"><button id="arrow">&lt;</button><div id="track"><button>One</button></div></div>';
        track = document.querySelector('#track');
        const bar = document.querySelector('#bar');
        overflowing();
        attach(bar, { wheel: { track } });

        const event = new WheelEvent('wheel', { bubbles: true, cancelable: true, deltaY: 120 });
        document.querySelector('#arrow').dispatchEvent(event);

        expect(track.scrollLeft).toBe(120);
        expect(event.defaultPrevented).toBe(true);
    });

    it('leaves a turn over a popover opened from the bar to the popover', () => {
        document.body.innerHTML = '<div id="track"><button>One</button><div id="popup" popover="manual"><p>picker</p></div></div>';
        track = document.querySelector('#track');
        overflowing();
        attach(track, { wheel: {} });

        const event = new WheelEvent('wheel', { bubbles: true, cancelable: true, deltaY: 120 });
        document.querySelector('#popup p').dispatchEvent(event);

        expect(track.scrollLeft).toBe(0);
        expect(event.defaultPrevented).toBe(false);
    });

    it('leaves a turn over a panel that scrolls on its own to that panel', () => {
        document.body.innerHTML = '<div id="track"><button>One</button><div id="panel" style="overflow-y: auto"><p>long</p></div></div>';
        track = document.querySelector('#track');
        const panel = document.querySelector('#panel');
        sizeScroller(panel, { scrollHeight: 400, clientHeight: 100 });
        overflowing();
        attach(track, { wheel: {} });

        const event = new WheelEvent('wheel', { bubbles: true, cancelable: true, deltaY: 120 });
        panel.querySelector('p').dispatchEvent(event);

        expect(track.scrollLeft).toBe(0);
        expect(event.defaultPrevented).toBe(false);
    });

    it('leaves a turn over a backdrop the bar opened to the page', () => {
        // The picker's backdrop covers the whole page yet sits in the bar's markup, so it must not count as the bar.
        document.body.innerHTML = '<div id="track"><button>One</button><div id="backdrop" style="position: fixed; inset: 0"></div></div>';
        track = document.querySelector('#track');
        overflowing();
        attach(track, { wheel: {} });

        const event = new WheelEvent('wheel', { bubbles: true, cancelable: true, deltaY: 120 });
        document.querySelector('#backdrop').dispatchEvent(event);

        expect(track.scrollLeft).toBe(0);
        expect(event.defaultPrevented).toBe(false);
    });

    it('stops answering once disposed', () => {
        overflowing();
        const handle = attach(track, { wheel: {} });

        handle.dispose();
        const event = wheel({ deltaY: 120 });

        expect(track.scrollLeft).toBe(0);
        expect(event.defaultPrevented).toBe(false);
    });

    /** An owner that records the directions it is told. */
    function owner() {
        const directions = [];

        return { directions, invokeMethodAsync: (name, direction) => { directions.push(name === 'HandleWheelAsync' ? direction : name); return Promise.resolve(); } };
    }

    describe('told to an owner', () => {
        it('reports the direction of every wheel notch and keeps the turn from the page', () => {
            const target = owner();
            attach(track, { wheel: { owner: target } });

            const event = wheel({ deltaY: 100 });
            wheel({ deltaY: 100, timeStamp: 50 });
            wheel({ deltaY: -100 });

            expect(target.directions).toEqual([1, 1, -1]);
            expect(event.defaultPrevented).toBe(true);
        });

        it('reports a wheel that counts in lines as notches too', () => {
            const target = owner();
            attach(track, { wheel: { owner: target } });

            wheel({ deltaY: 3, deltaMode: WheelEvent.DOM_DELTA_LINE });
            wheel({ deltaY: 3, deltaMode: WheelEvent.DOM_DELTA_LINE });

            expect(target.directions).toEqual([1, 1]);
        });

        it('reports a trackpad flick once however many events it sends', () => {
            const target = owner();
            attach(track, { wheel: { owner: target } });

            for (let i = 0; i < 12; i++) {
                wheel({ deltaY: 8 });
            }

            expect(target.directions).toEqual([1]);
        });

        it('reads a sideways turn as a direction rather than leaving it to the browser', () => {
            const target = owner();
            attach(track, { wheel: { owner: target } });

            wheel({ deltaX: -100 });

            expect(target.directions).toEqual([-1]);
        });
    });

    describe('gated on a key', () => {
        it('leaves a turn made without the key to the page', () => {
            const target = owner();
            attach(track, { wheel: { owner: target, modifier: 'ctrl' } });

            const event = wheel({ deltaY: 100 });

            expect(target.directions).toEqual([]);
            expect(event.defaultPrevented).toBe(false);
        });

        it('answers a turn made with the key held', () => {
            const target = owner();
            attach(track, { wheel: { owner: target, modifier: 'ctrl' } });

            const event = wheel({ deltaY: -100, ctrlKey: true });

            expect(target.directions).toEqual([-1]);
            expect(event.defaultPrevented).toBe(true);
        });

        it('gates a track the same way', () => {
            overflowing();
            attach(track, { wheel: { modifier: 'shift' } });

            wheel({ deltaY: 120 });
            expect(track.scrollLeft).toBe(0);

            wheel({ deltaY: 120, shiftKey: true });
            expect(track.scrollLeft).toBe(120);
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

    // Observations queued before disconnect are still delivered afterwards, by which point
    // the owner may already be gone.
    it('ignores an observation delivered after disposal', () => {
        const owner = fakeOwner();
        const handle = attach(sentinel, { sentinel: { owner } });

        handle.dispose();
        observers.latest().deliver([{ isIntersecting: true }]);

        expect(owner.invokeMethodAsync).not.toHaveBeenCalled();
    });
});

describe('Scroll guarding navigation keys', () => {
    /** @type {HTMLElement} */
    let list;

    beforeEach(() => {
        document.body.innerHTML = '<ul id="list"></ul>';
        list = document.querySelector('#list');
    });

    const press = key => {
        const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true });
        list.dispatchEvent(event);

        return event;
    };

    it.each(['ArrowUp', 'ArrowDown', 'Home', 'End'])('stops the page scrolling on %s', key => {
        attach(list, { guard: true });

        expect(press(key).defaultPrevented).toBe(true);
    });

    it.each(['Enter', ' ', 'Tab', 'Escape', 'ArrowLeft', 'ArrowRight'])('leaves %s alone', key => {
        attach(list, { guard: true });

        expect(press(key).defaultPrevented).toBe(false);
    });

    it('stops guarding once disposed', () => {
        const handle = attach(list, { guard: true });

        handle.dispose();

        expect(press('ArrowDown').defaultPrevented).toBe(false);
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

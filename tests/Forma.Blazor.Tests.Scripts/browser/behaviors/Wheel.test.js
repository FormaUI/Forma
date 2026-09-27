import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { attach } from '#browser/behaviors/Wheel.js';
import { configure, register, resolve } from '#input/InputRouter.js';
import { sizeScroller } from '#support/platform.js';

describe('Wheel', () => {
    /** @type {HTMLElement} */
    let track;

    // The router keeps every registration until it is disposed, so each test's are released after it.
    const handles = [];
    const attachWheel = (target, options) => {
        const handle = attach(target, { action: options?.owner ? 'test.turn' : 'test.scroll', ...options });
        handles.push(handle);

        return handle;
    };

    beforeEach(() => {
        document.body.innerHTML = '<div id="track"><button>One</button><button>Two</button></div>';
        track = document.querySelector('#track');
        track.scrollLeft = 0;
    });

    afterEach(() => {
        handles.splice(0).forEach(handle => handle.dispose());
        configure({ bindings: {} });
    });

    /** A track wider than its box, so there is something to scroll along. */
    function overflowing({ scrollWidth = 500, clientWidth = 200 } = {}) {
        sizeScroller(track, { scrollWidth, clientWidth });
    }

    // The test DOM drops the modifier keys a browser puts on a wheel event, so a held key is defined on the event itself.
    function wheel({ ctrlKey, shiftKey, ...properties }, target = track) {
        const event = new WheelEvent('wheel', { bubbles: true, cancelable: true, ...properties });

        if (ctrlKey !== undefined) {
            Object.defineProperty(event, 'ctrlKey', { value: ctrlKey });
        }

        if (shiftKey !== undefined) {
            Object.defineProperty(event, 'shiftKey', { value: shiftKey });
        }

        target.dispatchEvent(event);

        return event;
    }

    it('scrolls the track along when the wheel reports down', () => {
        overflowing();
        attachWheel(track);

        const event = wheel({ deltaY: 120 });

        expect(track.scrollLeft).toBe(120);
        expect(event.defaultPrevented).toBe(true);
    });

    it('declares the turn as a route on the input router under the action it is given', () => {
        overflowing();
        attachWheel(track, { action: 'bar.scroll' });

        const event = wheel({ deltaY: 120 });

        expect(resolve(event)).toMatchObject({ owner: track, action: 'bar.scroll', precedence: 'structural' });
    });

    it('takes the host\'s remap of the turn', () => {
        configure({ bindings: { 'test.scroll': [{ type: 'wheel', modifiers: { shift: true } }] } });
        overflowing();
        attachWheel(track);

        wheel({ deltaY: 120 });
        expect(track.scrollLeft).toBe(0);

        wheel({ deltaY: 120, shiftKey: true });
        expect(track.scrollLeft).toBe(120);
    });

    it('scrolls back when the wheel reports up', () => {
        overflowing();
        track.scrollLeft = 200;
        attachWheel(track);

        wheel({ deltaY: -120 });

        expect(track.scrollLeft).toBe(80);
    });

    it('keeps the turn from the page even once the track has nothing left to give', () => {
        overflowing();
        track.scrollLeft = 300;
        attachWheel(track);

        const event = wheel({ deltaY: 120 });

        // 500 - 200 is the whole slack, so the track is already at its end; the page still stays put.
        expect(track.scrollLeft).toBe(300);
        expect(event.defaultPrevented).toBe(true);
    });

    it('leaves the turn to the page when nothing overflows', () => {
        sizeScroller(track, { scrollWidth: 200, clientWidth: 200 });
        attachWheel(track);

        const event = wheel({ deltaY: 120 });

        expect(track.scrollLeft).toBe(0);
        expect(event.defaultPrevented).toBe(false);
        expect(resolve(event).action).toBeNull();
    });

    it('leaves a sideways turn alone, which the browser already scrolls', () => {
        overflowing();
        attachWheel(track);

        const event = wheel({ deltaX: 40, deltaY: 0 });

        expect(event.defaultPrevented).toBe(false);
    });

    it('reads a wheel that reports in lines rather than pixels', () => {
        overflowing();
        attachWheel(track);

        wheel({ deltaY: 3, deltaMode: WheelEvent.DOM_DELTA_LINE });

        expect(track.scrollLeft).toBe(48);
    });

    it('answers a turn over the bar around the track, moving the track', () => {
        document.body.innerHTML = '<div id="bar"><button id="arrow">&lt;</button><div id="track"><button>One</button></div></div>';
        track = document.querySelector('#track');
        const bar = document.querySelector('#bar');
        overflowing();
        attachWheel(bar, { track });

        const event = wheel({ deltaY: 120 }, document.querySelector('#arrow'));

        expect(track.scrollLeft).toBe(120);
        expect(event.defaultPrevented).toBe(true);
    });

    it('leaves a turn over a popover opened from the bar to the popover', () => {
        document.body.innerHTML = '<div id="track"><button>One</button><div id="popup" popover="manual"><p>picker</p></div></div>';
        track = document.querySelector('#track');
        overflowing();
        attachWheel(track);
        const popup = register(document.querySelector('#popup'), {});
        handles.push(popup);
        popup.activate();

        const event = wheel({ deltaY: 120 }, document.querySelector('#popup p'));

        expect(track.scrollLeft).toBe(0);
        expect(event.defaultPrevented).toBe(false);
        expect(resolve(event)).toMatchObject({ owner: document.querySelector('#popup'), action: null });
    });

    it('leaves a turn over a panel that scrolls on its own to that panel', () => {
        document.body.innerHTML = '<div id="track"><button>One</button><div id="panel" style="overflow-y: auto"><p>long</p></div></div>';
        track = document.querySelector('#track');
        const panel = document.querySelector('#panel');
        sizeScroller(panel, { scrollHeight: 400, clientHeight: 100 });
        overflowing();
        attachWheel(track);

        const event = wheel({ deltaY: 120 }, panel.querySelector('p'));

        expect(track.scrollLeft).toBe(0);
        expect(event.defaultPrevented).toBe(false);
    });

    it('stops answering once disposed', () => {
        overflowing();
        const handle = attachWheel(track);

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
            attachWheel(track, { owner: target });

            const event = wheel({ deltaY: 100 });
            wheel({ deltaY: 100, timeStamp: 50 });
            wheel({ deltaY: -100 });

            expect(target.directions).toEqual([1, 1, -1]);
            expect(event.defaultPrevented).toBe(true);
        });

        it('reports a wheel that counts in lines as notches too', () => {
            const target = owner();
            attachWheel(track, { owner: target });

            wheel({ deltaY: 3, deltaMode: WheelEvent.DOM_DELTA_LINE });
            wheel({ deltaY: 3, deltaMode: WheelEvent.DOM_DELTA_LINE });

            expect(target.directions).toEqual([1, 1]);
        });

        it('reports a trackpad flick once however many events it sends', () => {
            const target = owner();
            attachWheel(track, { owner: target });

            for (let i = 0; i < 12; i++) {
                wheel({ deltaY: 8 });
            }

            expect(target.directions).toEqual([1]);
        });

        it('reads a sideways turn as a direction rather than leaving it to the browser', () => {
            const target = owner();
            attachWheel(track, { owner: target });

            wheel({ deltaX: -100 });

            expect(target.directions).toEqual([-1]);
        });

        it('leaves a turn that moved nothing alone', () => {
            const target = owner();
            attachWheel(track, { owner: target });

            const event = wheel({ deltaX: 0, deltaY: 0 });

            expect(target.directions).toEqual([]);
            expect(event.defaultPrevented).toBe(false);
        });

        it('tells nothing of a remapped turn that moved nothing', () => {
            configure({ bindings: { 'test.turn': [{ type: 'wheel' }] } });
            const target = owner();
            attachWheel(track, { owner: target });

            wheel({ deltaX: 0, deltaY: 0 });

            expect(target.directions).toEqual([]);
        });
    });

    describe('gated on a key', () => {
        it('leaves a turn made without the key to the page', () => {
            const target = owner();
            attachWheel(track, { owner: target, modifier: 'ctrl' });

            const event = wheel({ deltaY: 100 });

            expect(target.directions).toEqual([]);
            expect(event.defaultPrevented).toBe(false);
        });

        it('answers a turn made with the key held', () => {
            const target = owner();
            attachWheel(track, { owner: target, modifier: 'ctrl' });

            const event = wheel({ deltaY: -100, ctrlKey: true });

            expect(target.directions).toEqual([-1]);
            expect(event.defaultPrevented).toBe(true);
        });

        it('gates a track the same way', () => {
            overflowing();
            attachWheel(track, { modifier: 'shift' });

            wheel({ deltaY: 120 });
            expect(track.scrollLeft).toBe(0);

            wheel({ deltaY: 120, shiftKey: true });
            expect(track.scrollLeft).toBe(120);
        });
    });
});

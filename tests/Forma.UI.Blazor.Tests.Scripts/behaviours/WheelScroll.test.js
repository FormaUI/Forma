import { describe, it, expect, beforeEach } from 'vitest';
import { attach } from '#behaviours/WheelScroll.js';
import { sizeScroller } from '#support/platform.js';

describe('WheelScroll', () => {
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

    function wheel(properties) {
        const event = new WheelEvent('wheel', { bubbles: true, cancelable: true, ...properties });
        track.dispatchEvent(event);

        return event;
    }

    it('scrolls the track along when the wheel reports down', () => {
        overflowing();
        attach(track);

        const event = wheel({ deltaY: 120 });

        expect(track.scrollLeft).toBe(120);
        expect(event.defaultPrevented).toBe(true);
    });

    it('scrolls back when the wheel reports up', () => {
        overflowing();
        track.scrollLeft = 200;
        attach(track);

        wheel({ deltaY: -120 });

        expect(track.scrollLeft).toBe(80);
    });

    it('keeps the turn from the page even once the track has nothing left to give', () => {
        overflowing();
        track.scrollLeft = 300;
        attach(track);

        const event = wheel({ deltaY: 120 });

        // 500 - 200 is the whole slack, so the track is already at its end; the page still stays put.
        expect(track.scrollLeft).toBe(300);
        expect(event.defaultPrevented).toBe(true);
    });

    it('leaves the turn to the page when nothing overflows', () => {
        sizeScroller(track, { scrollWidth: 200, clientWidth: 200 });
        attach(track);

        const event = wheel({ deltaY: 120 });

        expect(track.scrollLeft).toBe(0);
        expect(event.defaultPrevented).toBe(false);
    });

    it('leaves a sideways turn alone, which the browser already scrolls', () => {
        overflowing();
        attach(track);

        const event = wheel({ deltaX: 40, deltaY: 0 });

        expect(event.defaultPrevented).toBe(false);
    });

    it('reads a wheel that reports in lines rather than pixels', () => {
        overflowing();
        attach(track);

        wheel({ deltaY: 3, deltaMode: WheelEvent.DOM_DELTA_LINE });

        expect(track.scrollLeft).toBe(48);
    });

    it('answers a turn over the bar around the track, moving the track', () => {
        document.body.innerHTML = '<div id="bar"><button id="arrow">&lt;</button><div id="track"><button>One</button></div></div>';
        track = document.querySelector('#track');
        const bar = document.querySelector('#bar');
        overflowing();
        attach(bar, { track });

        const event = new WheelEvent('wheel', { bubbles: true, cancelable: true, deltaY: 120 });
        document.querySelector('#arrow').dispatchEvent(event);

        expect(track.scrollLeft).toBe(120);
        expect(event.defaultPrevented).toBe(true);
    });

    it('leaves a turn over a popover opened from the bar to the popover', () => {
        document.body.innerHTML = '<div id="track"><button>One</button><div id="popup" popover="manual"><p>picker</p></div></div>';
        track = document.querySelector('#track');
        overflowing();
        attach(track);

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
        attach(track);

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
        attach(track);

        const event = new WheelEvent('wheel', { bubbles: true, cancelable: true, deltaY: 120 });
        document.querySelector('#backdrop').dispatchEvent(event);

        expect(track.scrollLeft).toBe(0);
        expect(event.defaultPrevented).toBe(false);
    });

    it('stops answering once disposed', () => {
        overflowing();
        const handle = attach(track);

        handle.dispose();
        const event = wheel({ deltaY: 120 });

        expect(track.scrollLeft).toBe(0);
        expect(event.defaultPrevented).toBe(false);
    });
});

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { ScrollInteraction } from '#browser/scrolling/ScrollInteraction.mjs';
import { configure, describe as describeScope, register, resolve } from '#input/InputRouter.js';
import { sizeScroller, stubMutationObserver } from '#support/platform.js';

describe('ScrollInteraction', () => {
    /** @type {ReturnType<typeof stubMutationObserver>} */
    let mutations;
    /** @type {HTMLElement} */
    let pane;
    /** @type {HTMLElement} */
    let viewport;
    /** @type {ScrollInteraction[]} */
    let handles;

    const buttons = () => ({ container: pane, back: '.back', forward: '.forward', backAction: 'pane.scroll-back', forwardAction: 'pane.scroll-forward' });

    beforeEach(() => {
        mutations = stubMutationObserver();
        document.body.innerHTML = '<div id="pane"><button class="back"></button><div id="viewport"><span class="stop"></span></div><button class="forward"></button></div>';
        pane = document.querySelector('#pane');
        viewport = document.querySelector('#viewport');
        viewport.scrollBy = vi.fn();
        sizeScroller(viewport, { scrollLeft: 0, scrollWidth: 900, clientWidth: 300, scrollTop: 0, scrollHeight: 900, clientHeight: 300 });
        handles = [];
    });

    afterEach(() => {
        handles.splice(0).forEach(handle => handle.dispose());
        mutations.restore();
    });

    const attachInteraction = options => {
        const handle = new ScrollInteraction(viewport, options);
        handles.push(handle);

        return handle;
    };

    it('routes a wheel turned over the target under the action it names', () => {
        attachInteraction({ wheel: 'pane.scroll' });

        const event = new WheelEvent('wheel', { deltaY: 120, bubbles: true, cancelable: true });
        viewport.dispatchEvent(event);

        expect(resolve(event)).toMatchObject({ owner: viewport, action: 'pane.scroll' });
        expect(viewport.scrollLeft).toBe(120);
    });

    it('declares no wheel route where none is named', () => {
        attachInteraction({ buttons: buttons() });

        expect(describeScope(viewport)).toBeNull();
    });

    it('steps the target along its axis from its buttons', () => {
        attachInteraction({ axis: 'block', buttons: buttons() });

        pane.querySelector('.forward').dispatchEvent(new MouseEvent('click', { bubbles: true, detail: 1 }));

        expect(viewport.scrollBy).toHaveBeenCalledExactlyOnceWith({ top: 240, behavior: 'smooth' });
    });

    it('routes no button where none are named', () => {
        attachInteraction({ wheel: 'pane.scroll' });

        expect(describeScope(pane.querySelector('.back'))).toBeNull();
    });

    it('releases the wheel and the buttons once disposed', () => {
        const handle = attachInteraction({ wheel: 'pane.scroll', buttons: buttons() });

        handle.dispose();

        expect(describeScope(viewport)).toBeNull();
        expect(describeScope(pane.querySelector('.forward'))).toBeNull();
    });
});

describe('ScrollInteraction turned by a wheel', () => {
    /** @type {HTMLElement} */
    let track;

    // The interactions made in a test, whose registrations the router holds until they are disposed after it.
    const handles = [];
    const attachWheel = (target, options) => {
        const handle = new ScrollInteraction(target, { wheel: 'test.scroll', ...options });
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

    /** Makes the track wider than its box. */
    function overflowing({ scrollWidth = 500, clientWidth = 200 } = {}) {
        sizeScroller(track, { scrollWidth, clientWidth });
    }

    // A wheel event with any held key defined on the event, which the test DOM drops from a WheelEvent's init.
    function wheel({ shiftKey, ...properties }, target = track) {
        const event = new WheelEvent('wheel', { bubbles: true, cancelable: true, ...properties });

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
        attachWheel(track, { wheel: 'bar.scroll' });

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

        // 500 - 200 is the whole slack, so the track is already at its end.
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

    it('scrolls a right-to-left track the other way', () => {
        track.style.direction = 'rtl';
        overflowing();
        attachWheel(track);

        wheel({ deltaY: 120 });

        expect(track.scrollLeft).toBe(-120);
    });
});

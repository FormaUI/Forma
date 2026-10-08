import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { attach } from '#browser/behaviors/Wheel.js';
import { configure, resolve } from '#input/InputRouter.js';

describe('Wheel', () => {
    /** @type {HTMLElement} */
    let track;

    // The handles attached in a test, whose registrations the router holds until they are disposed after it.
    const handles = [];
    const attachWheel = (target, options) => {
        const handle = attach(target, { action: 'test.turn', owner: owner(), ...options });
        handles.push(handle);

        return handle;
    };

    beforeEach(() => {
        document.body.innerHTML = '<div id="track"><button>One</button><button>Two</button></div>';
        track = document.querySelector('#track');
    });

    afterEach(() => {
        handles.splice(0).forEach(handle => handle.dispose());
        configure({ bindings: {} });
    });

    // A wheel event with any held key defined on the event, which the test DOM drops from a WheelEvent's init.
    function wheel({ ctrlKey, ...properties }, target = track) {
        const event = new WheelEvent('wheel', { bubbles: true, cancelable: true, ...properties });

        if (ctrlKey !== undefined) {
            Object.defineProperty(event, 'ctrlKey', { value: ctrlKey });
        }

        target.dispatchEvent(event);

        return event;
    }

    it('declares the turn as a route on the input router under the action it is given', () => {
        attachWheel(track, { action: 'bar.turn' });

        const event = wheel({ deltaY: 100 });

        expect(resolve(event)).toMatchObject({ owner: track, action: 'bar.turn', precedence: 'structural' });
    });

    it('stops answering once disposed', () => {
        const target = owner();
        const handle = attachWheel(track, { owner: target });

        handle.dispose();
        const event = wheel({ deltaY: 100 });

        expect(target.directions).toEqual([]);
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
    });
});

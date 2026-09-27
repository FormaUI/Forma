import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { attach } from '#browser/behaviors/PanGesture.js';
import { register, resolve } from '#input/InputRouter.js';
import { pointerEvent, sizeScroller, stubPointerCapture } from '#support/platform.js';

describe('PanGesture', () => {
    /** @type {HTMLElement} */
    let frame;
    /** @type {Set<number>} */
    let captured;

    // The router keeps every registration until it is disposed, so each test's are released after it.
    const handles = [];
    const attachPan = (target, options) => {
        const handle = attach(target, { action: 'test.pan', ...options });
        handles.push(handle);

        return handle;
    };

    beforeEach(() => {
        document.body.innerHTML = '<div id="frame" class="is-zoomed"><img id="picture" alt=""></div>';
        frame = document.querySelector('#frame');
        captured = stubPointerCapture(frame);
        sizeScroller(frame, { scrollLeft: 100, scrollTop: 50 });
    });

    afterEach(() => handles.splice(0).forEach(handle => handle.dispose()));

    const point = (type, properties = {}) => {
        const event = pointerEvent(type, { clientX: 200, clientY: 200, ...properties });
        document.querySelector('#picture').dispatchEvent(event);

        return event;
    };

    it('scrolls the frame against the mouse as it drags', () => {
        attachPan(frame);

        point('pointerdown');
        point('pointermove', { clientX: 230, clientY: 180 });

        expect(frame.scrollLeft).toBe(70);
        expect(frame.scrollTop).toBe(70);
    });

    it('captures the pointer for the drag and gives it back on release', () => {
        attachPan(frame);

        point('pointerdown', { pointerId: 4 });
        expect(captured.has(4)).toBe(true);

        point('pointermove', { pointerId: 4, clientX: 210 });
        point('pointerup', { pointerId: 4, clientX: 210 });

        expect(captured.has(4)).toBe(false);
    });

    it('measures each drag from where its own press began', () => {
        attachPan(frame);

        point('pointerdown');
        point('pointermove', { clientX: 230 });
        point('pointerup', { clientX: 230 });
        expect(frame.scrollLeft).toBe(70);

        point('pointerdown', { clientX: 300 });
        point('pointermove', { clientX: 290 });

        expect(frame.scrollLeft).toBe(80);
    });

    it('leaves a touch to the browser', () => {
        attachPan(frame);

        point('pointerdown', { pointerType: 'touch' });
        point('pointermove', { pointerType: 'touch', clientX: 230 });

        expect(frame.scrollLeft).toBe(100);
        expect(captured.size).toBe(0);
    });

    it('pans only while the frame carries the class it was told to wait for', () => {
        attachPan(frame, { whileClass: 'is-zoomed' });
        frame.classList.remove('is-zoomed');

        point('pointerdown');
        point('pointermove', { clientX: 230 });
        expect(frame.scrollLeft).toBe(100);

        frame.classList.add('is-zoomed');
        point('pointerdown');
        point('pointermove', { clientX: 230 });

        expect(frame.scrollLeft).toBe(70);
    });

    it('declares the press as a route on the input router under the action it is given', () => {
        attachPan(frame, { action: 'viewer.pan' });

        const event = point('pointerdown');
        point('pointerup');

        // Resolved once the gesture it began has ended, since while one is under way the route matches no other press.
        expect(resolve(event)).toMatchObject({ owner: frame, action: 'viewer.pan', precedence: 'structural' });
    });

    it('leaves a press inside a popover over the frame to the popover', () => {
        frame.insertAdjacentHTML('beforeend', '<div id="popup" popover="manual"><button id="option">Option</button></div>');
        attachPan(frame);
        const popup = register(document.querySelector('#popup'), {});
        handles.push(popup);
        popup.activate();

        document.querySelector('#option').dispatchEvent(pointerEvent('pointerdown', { pointerId: 5, clientX: 200, clientY: 200 }));
        point('pointermove', { pointerId: 5, clientX: 230 });

        expect(captured.has(5)).toBe(false);
        expect(frame.scrollLeft).toBe(100);
    });

    it('stops panning once disposed', () => {
        const handle = attachPan(frame);

        handle.dispose();
        point('pointerdown');
        point('pointermove', { clientX: 230 });

        expect(frame.scrollLeft).toBe(100);
    });
});

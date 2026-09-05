import { describe, it, expect, beforeEach } from 'vitest';
import { attach } from '#common/behaviours/PanGesture.js';
import { pointerEvent, sizeScroller, stubPointerCapture } from '#support/platform.js';

describe('PanGesture', () => {
    /** @type {HTMLElement} */
    let frame;
    /** @type {Set<number>} */
    let captured;

    beforeEach(() => {
        document.body.innerHTML = '<div id="frame" class="is-zoomed"><img id="picture" alt=""></div>';
        frame = document.querySelector('#frame');
        captured = stubPointerCapture(frame);
        sizeScroller(frame, { scrollLeft: 100, scrollTop: 50 });
    });

    const point = (type, properties = {}) =>
        document.querySelector('#picture').dispatchEvent(pointerEvent(type, { clientX: 200, clientY: 200, ...properties }));

    it('scrolls the frame against the mouse as it drags', () => {
        attach(frame);

        point('pointerdown');
        point('pointermove', { clientX: 230, clientY: 180 });

        expect(frame.scrollLeft).toBe(70);
        expect(frame.scrollTop).toBe(70);
    });

    it('captures the pointer for the drag and gives it back on release', () => {
        attach(frame);

        point('pointerdown', { pointerId: 4 });
        expect(captured.has(4)).toBe(true);

        point('pointermove', { pointerId: 4, clientX: 210 });
        point('pointerup', { pointerId: 4, clientX: 210 });

        expect(captured.has(4)).toBe(false);
    });

    it('measures each drag from where its own press began', () => {
        attach(frame);

        point('pointerdown');
        point('pointermove', { clientX: 230 });
        point('pointerup', { clientX: 230 });
        expect(frame.scrollLeft).toBe(70);

        point('pointerdown', { clientX: 300 });
        point('pointermove', { clientX: 290 });

        expect(frame.scrollLeft).toBe(80);
    });

    it('leaves a touch to the browser', () => {
        attach(frame);

        point('pointerdown', { pointerType: 'touch' });
        point('pointermove', { pointerType: 'touch', clientX: 230 });

        expect(frame.scrollLeft).toBe(100);
        expect(captured.size).toBe(0);
    });

    it('pans only while the frame carries the class it was told to wait for', () => {
        attach(frame, { whileClass: 'is-zoomed' });
        frame.classList.remove('is-zoomed');

        point('pointerdown');
        point('pointermove', { clientX: 230 });
        expect(frame.scrollLeft).toBe(100);

        frame.classList.add('is-zoomed');
        point('pointerdown');
        point('pointermove', { clientX: 230 });

        expect(frame.scrollLeft).toBe(70);
    });

    it('stops panning once disposed', () => {
        const handle = attach(frame);

        handle.dispose();
        point('pointerdown');
        point('pointermove', { clientX: 230 });

        expect(frame.scrollLeft).toBe(100);
    });
});

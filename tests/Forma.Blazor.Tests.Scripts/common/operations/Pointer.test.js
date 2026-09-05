import { describe, it, expect, beforeEach, vi } from 'vitest';
import { capture, release } from '#common/operations/Pointer.js';

describe('Pointer', () => {
    /** @type {HTMLElement} */
    let handle;

    beforeEach(() => {
        document.body.innerHTML = '<div id="handle"></div>';
        handle = document.querySelector('#handle');

        let captured = null;
        handle.setPointerCapture = vi.fn(pointerId => { captured = pointerId; });
        handle.releasePointerCapture = vi.fn(() => { captured = null; });
        handle.hasPointerCapture = pointerId => captured === pointerId;
    });

    it('routes the pointer to the element', () => {
        capture(handle, 7);

        expect(handle.setPointerCapture).toHaveBeenCalledExactlyOnceWith(7);
    });

    it('gives the pointer back', () => {
        capture(handle, 7);

        release(handle, 7);

        expect(handle.releasePointerCapture).toHaveBeenCalledExactlyOnceWith(7);
    });

    // A gesture the browser already cancelled has released the pointer for us.
    it('does nothing when the element no longer holds the pointer', () => {
        release(handle, 7);

        expect(handle.releasePointerCapture).not.toHaveBeenCalled();
    });

    it('does nothing for a pointer the element never held', () => {
        capture(handle, 7);

        release(handle, 9);

        expect(handle.releasePointerCapture).not.toHaveBeenCalled();
    });
});

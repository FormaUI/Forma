import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { attach } from '#browser/components/Splitter.js';
import { pointerEvent, stubPointerCapture } from '#support/platform.js';

describe('Splitter', () => {
    /** @type {HTMLElement} */
    let bar;
    /** @type {{ invokeMethodAsync: import('vitest').Mock }} */
    let owner;
    /** The frames requested and not yet run. */
    let frames;

    const handles = [];
    const attachSplitter = (axis = 'inline') => {
        const handle = attach(bar, { owner, axis, action: 'splitter.resize' });
        handles.push(handle);

        return handle;
    };

    beforeEach(() => {
        document.body.innerHTML = '<div id="bar" role="separator" tabindex="0"></div>';
        bar = document.querySelector('#bar');
        stubPointerCapture(bar);
        owner = { invokeMethodAsync: vi.fn().mockResolvedValue(undefined) };
        frames = [];
        vi.spyOn(globalThis, 'requestAnimationFrame').mockImplementation(callback => frames.push(callback));
        vi.spyOn(globalThis, 'cancelAnimationFrame').mockImplementation(handle => { frames[handle - 1] = null; });
    });

    afterEach(() => {
        handles.splice(0).forEach(handle => handle.dispose());
        vi.restoreAllMocks();
    });

    const point = (type, properties) => bar.dispatchEvent(pointerEvent(type, properties));
    const runFrames = () => frames.splice(0).forEach(frame => frame?.());
    const told = () => owner.invokeMethodAsync.mock.calls.map(([method, ...args]) => [method, ...args].join(' '));

    it('tells the owner the start, the travel once a frame, and the end as the bar is let go', () => {
        attachSplitter();

        point('pointerdown', { clientX: 100, clientY: 50 });
        point('pointermove', { clientX: 110, clientY: 50 });
        point('pointermove', { clientX: 132, clientY: 50 });
        runFrames();
        point('pointerup', { clientX: 132, clientY: 50 });

        expect(told()).toEqual(['HandleResizeStartAsync', 'HandleResizeAsync 32', 'HandleResizeEndAsync false']);
    });

    it('measures the travel down the block axis for a horizontal bar', () => {
        attachSplitter('block');

        point('pointerdown', { clientX: 100, clientY: 50 });
        point('pointermove', { clientX: 140, clientY: 74 });
        runFrames();

        expect(told()).toContain('HandleResizeAsync 24');
    });

    it('tells the travel still waiting for its frame before the end', () => {
        attachSplitter();

        point('pointerdown', { clientX: 100, clientY: 50 });
        point('pointermove', { clientX: 140, clientY: 50 });
        point('pointerup', { clientX: 140, clientY: 50 });
        runFrames();

        expect(told()).toEqual(['HandleResizeStartAsync', 'HandleResizeAsync 40', 'HandleResizeEndAsync false']);
    });

    it('ends the drag as canceled on Escape, dropping the travel still waiting', () => {
        attachSplitter();

        point('pointerdown', { clientX: 100, clientY: 50 });
        point('pointermove', { clientX: 140, clientY: 50 });
        bar.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true }));
        runFrames();

        expect(told()).toEqual(['HandleResizeStartAsync', 'HandleResizeEndAsync true']);
    });

    it('tells the owner nothing once disposed', () => {
        attachSplitter().dispose();

        point('pointerdown', { clientX: 100, clientY: 50 });
        point('pointermove', { clientX: 140, clientY: 50 });
        runFrames();

        expect(told()).toEqual([]);
    });
});

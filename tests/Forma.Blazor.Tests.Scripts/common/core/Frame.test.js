import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { Frame } from '#common/core/Frame.js';
import { stubAnimationFrames } from '#support/platform.js';

describe('Frame', () => {
    /** @type {ReturnType<typeof stubAnimationFrames>} */
    let frames;

    beforeEach(() => {
        frames = stubAnimationFrames();
    });

    afterEach(() => frames.restore());

    it('runs the work in the next frame', () => {
        const frame = new Frame();
        const work = vi.fn();

        frame.request(work);
        expect(work).not.toHaveBeenCalled();

        frames.flush();

        expect(work).toHaveBeenCalledOnce();
    });

    it('holds work requested several times to one run', () => {
        const frame = new Frame();
        const work = vi.fn();

        frame.request(work);
        frame.request(work);
        frame.request(work);
        frames.flush();

        expect(work).toHaveBeenCalledOnce();
        expect(frames.pending).toBe(0);
    });

    it('is free again once the frame has run, so the work inside it can request the next', () => {
        const frame = new Frame();
        let runs = 0;
        const loop = () => {
            runs += 1;
            if (runs < 3) {
                frame.request(loop);
            }
        };

        frame.request(loop);
        frames.flush();
        frames.flush();
        frames.flush();

        expect(runs).toBe(3);
        expect(frame.isPending).toBe(false);
    });

    it('drops the work when cancelled before the frame', () => {
        const frame = new Frame();
        const work = vi.fn();

        frame.request(work);
        expect(frame.isPending).toBe(true);
        frame.cancel();
        frames.flush();

        expect(work).not.toHaveBeenCalled();
        expect(frame.isPending).toBe(false);
    });

    it('cancels nothing when nothing is pending', () => {
        const frame = new Frame();

        expect(() => frame.cancel()).not.toThrow();
    });
});

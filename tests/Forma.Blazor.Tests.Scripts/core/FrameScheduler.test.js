import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { FrameScheduler } from '#core/FrameScheduler.js';
import { stubAnimationFrames } from '#support/platform.js';

describe('FrameScheduler', () => {
    /** @type {ReturnType<typeof stubAnimationFrames>} */
    let frames;

    beforeEach(() => {
        frames = stubAnimationFrames();
    });

    afterEach(() => frames.restore());

    it('runs the work in the next frame', () => {
        const scheduler = new FrameScheduler();
        const work = vi.fn();

        scheduler.request(work);
        expect(work).not.toHaveBeenCalled();

        frames.flush();

        expect(work).toHaveBeenCalledOnce();
    });

    it('holds work requested several times to one run', () => {
        const scheduler = new FrameScheduler();
        const work = vi.fn();

        scheduler.request(work);
        scheduler.request(work);
        scheduler.request(work);
        frames.flush();

        expect(work).toHaveBeenCalledOnce();
        expect(frames.pending).toBe(0);
    });

    it('is free again once the frame has run, so the work inside it can request the next', () => {
        const scheduler = new FrameScheduler();
        let runs = 0;
        const loop = () => {
            runs += 1;
            if (runs < 3) {
                scheduler.request(loop);
            }
        };

        scheduler.request(loop);
        frames.flush();
        frames.flush();
        frames.flush();

        expect(runs).toBe(3);
        expect(scheduler.isPending).toBe(false);
    });

    it('drops the work when canceled before the frame', () => {
        const scheduler = new FrameScheduler();
        const work = vi.fn();

        scheduler.request(work);
        expect(scheduler.isPending).toBe(true);
        scheduler.cancel();
        frames.flush();

        expect(work).not.toHaveBeenCalled();
        expect(scheduler.isPending).toBe(false);
    });

    it('cancels nothing when nothing is pending', () => {
        const scheduler = new FrameScheduler();

        expect(() => scheduler.cancel()).not.toThrow();
    });
});

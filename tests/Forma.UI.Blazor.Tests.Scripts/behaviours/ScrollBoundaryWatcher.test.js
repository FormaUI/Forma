import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { attach } from '#behaviours/ScrollBoundaryWatcher.js';
import { fakeOwner } from '#support/owner.js';
import { sizeScroller, stubAnimationFrames, stubMutationObserver, stubResizeObserver } from '#support/platform.js';

describe('ScrollBoundaryWatcher', () => {
    /** @type {ReturnType<typeof stubAnimationFrames>} */
    let frames;
    /** @type {ReturnType<typeof stubResizeObserver>} */
    let resizes;
    /** @type {ReturnType<typeof stubMutationObserver>} */
    let mutations;
    /** @type {HTMLElement} */
    let track;
    /** @type {ReturnType<typeof fakeOwner>} */
    let owner;

    beforeEach(() => {
        frames = stubAnimationFrames();
        resizes = stubResizeObserver();
        mutations = stubMutationObserver();
        document.body.innerHTML = '<div id="track"><button>One</button><button>Two</button></div>';
        track = document.querySelector('#track');
        owner = fakeOwner();
    });

    afterEach(() => {
        frames.restore();
        resizes.restore();
        mutations.restore();
    });

    const reported = () => expect(owner.invokeMethodAsync);

    it('reports nothing scrollable when everything fits', () => {
        sizeScroller(track, { scrollLeft: 0, scrollWidth: 300, clientWidth: 300 });
        attach(track, { owner });

        frames.flush();

        reported().toHaveBeenLastCalledWith('HandleOverflowAsync', false, false);
    });

    it('reports more ahead when the track starts at its beginning', () => {
        sizeScroller(track, { scrollLeft: 0, scrollWidth: 900, clientWidth: 300 });
        attach(track, { owner });

        frames.flush();

        reported().toHaveBeenLastCalledWith('HandleOverflowAsync', false, true);
    });

    it('reports both directions from the middle of the track', () => {
        sizeScroller(track, { scrollLeft: 300, scrollWidth: 900, clientWidth: 300 });
        attach(track, { owner });

        frames.flush();

        reported().toHaveBeenLastCalledWith('HandleOverflowAsync', true, true);
    });

    it('reports nothing ahead at the end of the track', () => {
        sizeScroller(track, { scrollLeft: 600, scrollWidth: 900, clientWidth: 300 });
        attach(track, { owner });

        frames.flush();

        reported().toHaveBeenLastCalledWith('HandleOverflowAsync', true, false);
    });

    // A right-to-left track counts scrollLeft downwards from zero.
    it('reads a right-to-left offset the same as its mirror', () => {
        sizeScroller(track, { scrollLeft: -300, scrollWidth: 900, clientWidth: 300 });
        attach(track, { owner });

        frames.flush();

        reported().toHaveBeenLastCalledWith('HandleOverflowAsync', true, true);
    });

    it('treats a sub-pixel remainder as nothing to scroll', () => {
        sizeScroller(track, { scrollLeft: 0.5, scrollWidth: 300.5, clientWidth: 300 });
        attach(track, { owner });

        frames.flush();

        reported().toHaveBeenLastCalledWith('HandleOverflowAsync', false, false);
    });

    it('coalesces a burst of scrolling into one report', () => {
        sizeScroller(track, { scrollLeft: 0, scrollWidth: 900, clientWidth: 300 });
        attach(track, { owner });
        frames.flush();

        track.scrollLeft = 300;
        track.dispatchEvent(new Event('scroll'));
        track.dispatchEvent(new Event('scroll'));
        track.dispatchEvent(new Event('scroll'));
        frames.flush();

        reported().toHaveBeenCalledTimes(2);
    });

    it('stays quiet while the answer is unchanged', () => {
        sizeScroller(track, { scrollLeft: 0, scrollWidth: 900, clientWidth: 300 });
        attach(track, { owner });
        frames.flush();

        track.scrollLeft = 0.5;
        track.dispatchEvent(new Event('scroll'));
        frames.flush();

        reported().toHaveBeenCalledTimes(1);
    });

    it('watches each command alongside the track', () => {
        attach(track, { owner });

        expect(resizes.latest().targets).toHaveLength(3);
    });

    // Commands can be added or removed without the track's own box changing.
    it('watches a command added after it attached', () => {
        attach(track, { owner });

        track.appendChild(document.createElement('button'));
        mutations.latest().deliver([]);

        expect(resizes.latest().targets).toHaveLength(4);
    });

    it('re-measures when the track resizes', () => {
        sizeScroller(track, { scrollLeft: 0, scrollWidth: 300, clientWidth: 300 });
        attach(track, { owner });
        frames.flush();

        sizeScroller(track, { scrollWidth: 900 });
        resizes.latest().deliver([]);
        frames.flush();

        reported().toHaveBeenLastCalledWith('HandleOverflowAsync', false, true);
    });

    it('stops watching once disposed', () => {
        const handle = attach(track, { owner });

        handle.dispose();

        expect(resizes.latest().isConnected).toBe(false);
        expect(mutations.latest().isConnected).toBe(false);
    });

    // Its coalesced report was already queued against listeners that dispose only tore down.
    it('drops a report queued before disposal', () => {
        sizeScroller(track, { scrollLeft: 0, scrollWidth: 900, clientWidth: 300 });
        const handle = attach(track, { owner });

        handle.dispose();
        frames.flush();

        reported().not.toHaveBeenCalled();
    });

    it('queues nothing once disposed', () => {
        const handle = attach(track, { owner });
        handle.dispose();
        frames.flush();

        track.dispatchEvent(new Event('scroll'));

        expect(frames.pending).toBe(0);
    });
});

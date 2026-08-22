import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { attach } from '#behaviours/InfiniteScroll.js';
import { fakeOwner } from '#support/owner.js';
import { stubIntersectionObserver } from '#support/platform.js';

describe('InfiniteScroll', () => {
    /** @type {ReturnType<typeof stubIntersectionObserver>} */
    let observers;
    /** @type {HTMLElement} */
    let sentinel;

    beforeEach(() => {
        observers = stubIntersectionObserver();
        document.body.innerHTML = '<div id="panel"><div id="sentinel"></div></div>';
        sentinel = document.querySelector('#sentinel');
    });

    afterEach(() => observers.restore());

    it('watches the sentinel it was given', () => {
        attach(sentinel, { owner: fakeOwner() });

        expect(observers.latest().targets).toEqual([sentinel]);
    });

    it('asks for the next page once the sentinel comes into view', () => {
        const owner = fakeOwner();
        attach(sentinel, { owner });

        observers.latest().deliver([{ isIntersecting: true }]);

        expect(owner.invokeMethodAsync).toHaveBeenCalledExactlyOnceWith('HandleReachedAsync');
    });

    it('stays quiet while the sentinel is out of view', () => {
        const owner = fakeOwner();
        attach(sentinel, { owner });

        observers.latest().deliver([{ isIntersecting: false }]);

        expect(owner.invokeMethodAsync).not.toHaveBeenCalled();
    });

    it('measures against the scrolling panel when one is given', () => {
        const panel = document.querySelector('#panel');
        attach(sentinel, { owner: fakeOwner(), root: panel });

        expect(observers.latest().options.root).toBe(panel);
    });

    it('reaches ahead of the sentinel by default', () => {
        attach(sentinel, { owner: fakeOwner() });

        expect(observers.latest().options.rootMargin).toBe('200px');
    });

    it('stops watching once disposed', () => {
        const handle = attach(sentinel, { owner: fakeOwner() });

        handle.dispose();

        expect(observers.latest().isConnected).toBe(false);
    });

    // Observations queued before disconnect are still delivered afterwards, by which point
    // the owner may already be gone.
    it('ignores an observation delivered after disposal', () => {
        const owner = fakeOwner();
        const handle = attach(sentinel, { owner });

        handle.dispose();
        observers.latest().deliver([{ isIntersecting: true }]);

        expect(owner.invokeMethodAsync).not.toHaveBeenCalled();
    });
});

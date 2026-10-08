import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { attach, toCenter } from '#browser/behaviors/Scroll.js';
import { fakeOwner } from '#support/owner.js';
import { sizeScroller, stubIntersectionObserver } from '#support/platform.js';

describe('Scroll watching a sentinel', () => {
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
        attach(sentinel, { sentinel: { owner: fakeOwner() } });

        expect(observers.latest().targets).toEqual([sentinel]);
    });

    it('asks for the next page once the sentinel comes into view', () => {
        const owner = fakeOwner();
        attach(sentinel, { sentinel: { owner } });

        observers.latest().deliver([{ isIntersecting: true }]);

        expect(owner.invokeMethodAsync).toHaveBeenCalledExactlyOnceWith('HandleReachedAsync');
    });

    it('stays quiet while the sentinel is out of view', () => {
        const owner = fakeOwner();
        attach(sentinel, { sentinel: { owner } });

        observers.latest().deliver([{ isIntersecting: false }]);

        expect(owner.invokeMethodAsync).not.toHaveBeenCalled();
    });

    it('measures against the scrolling panel when one is given', () => {
        const panel = document.querySelector('#panel');
        attach(sentinel, { sentinel: { owner: fakeOwner(), root: panel } });

        expect(observers.latest().options.root).toBe(panel);
    });

    it('reaches ahead of the sentinel by default', () => {
        attach(sentinel, { sentinel: { owner: fakeOwner() } });

        expect(observers.latest().options.rootMargin).toBe('200px');
    });

    it('stops watching once disposed', () => {
        const handle = attach(sentinel, { sentinel: { owner: fakeOwner() } });

        handle.dispose();

        expect(observers.latest().isConnected).toBe(false);
    });

    // An observation queued before disconnect is delivered after it.
    it('ignores an observation delivered after disposal', () => {
        const owner = fakeOwner();
        const handle = attach(sentinel, { sentinel: { owner } });

        handle.dispose();
        observers.latest().deliver([{ isIntersecting: true }]);

        expect(owner.invokeMethodAsync).not.toHaveBeenCalled();
    });
});

describe('Scroll moving a container', () => {
    /** @type {HTMLElement} */
    let container;

    beforeEach(() => {
        document.body.innerHTML = '<div id="track"></div>';
        container = document.querySelector('#track');
        container.scrollBy = vi.fn();
    });

    describe('toCenter', () => {
        it('puts the middle of the content in view on both axes', () => {
            sizeScroller(container, { scrollWidth: 900, clientWidth: 300, scrollHeight: 500, clientHeight: 100 });

            toCenter(container);

            expect([container.scrollLeft, container.scrollTop]).toEqual([300, 200]);
        });

        it('leaves a container whose content fits at its origin', () => {
            sizeScroller(container, { scrollWidth: 300, clientWidth: 300, scrollHeight: 100, clientHeight: 100 });

            toCenter(container);

            expect([container.scrollLeft, container.scrollTop]).toEqual([0, 0]);
        });

        // A right-to-left container counts scrollLeft downwards from zero and clamps a positive position to its start.
        it('counts the inline middle downwards in a right-to-left container', () => {
            container.style.direction = 'rtl';
            sizeScroller(container, { scrollWidth: 900, clientWidth: 300, scrollHeight: 500, clientHeight: 100 });

            toCenter(container);

            expect([container.scrollLeft, container.scrollTop]).toEqual([-300, 200]);
        });
    });
});

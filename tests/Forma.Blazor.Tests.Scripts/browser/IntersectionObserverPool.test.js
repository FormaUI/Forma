import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { IntersectionObserverPool } from '#browser/IntersectionObserverPool.mjs';
import { stubIntersectionObserver } from '#support/platform.js';

describe('IntersectionObserverPool', () => {
    /** @type {ReturnType<typeof stubIntersectionObserver>} */
    let observers;
    /** @type {Array<{ dispose(): void }>} */
    let subscriptions;
    /** @type {HTMLElement} */
    let panel;
    /** @type {HTMLElement} */
    let first;
    /** @type {HTMLElement} */
    let second;

    beforeEach(() => {
        observers = stubIntersectionObserver();
        subscriptions = [];
        document.body.innerHTML = '<div id="panel"><div id="first"></div><div id="second"></div></div><div id="other"></div>';
        panel = document.querySelector('#panel');
        first = document.querySelector('#first');
        second = document.querySelector('#second');
    });

    afterEach(() => {
        subscriptions.splice(0).forEach(subscription => subscription.dispose());
        observers.restore();
    });

    const observe = (target, options, listener = vi.fn()) => {
        const subscription = IntersectionObserverPool.observe(target, options, listener);
        subscriptions.push(subscription);

        return { listener, subscription };
    };

    it('shares one observer among targets watched against the same root, margin and threshold', () => {
        observe(first, { root: panel, rootMargin: 200 });
        observe(second, { root: panel, rootMargin: 200 });

        expect(observers.instances).toHaveLength(1);
        expect(observers.latest().targets).toEqual([first, second]);
        expect(observers.latest().options).toEqual({ root: panel, rootMargin: '200px', threshold: 0 });
    });

    it('gives each different root, margin or threshold an observer of its own', () => {
        observe(first, { root: panel, rootMargin: 200 });
        observe(first, { root: null, rootMargin: 200 });
        observe(first, { root: panel, rootMargin: 0 });
        observe(first, { root: panel, rootMargin: 200, threshold: 0.5 });

        expect(observers.instances).toHaveLength(4);
        expect(IntersectionObserverPool.size).toBe(4);
    });

    it('calls each listener of the target an entry reports, and no other', () => {
        const { listener: firstListener } = observe(first, { root: panel });
        const { listener: secondListener } = observe(second, { root: panel });
        const entry = { target: first, isIntersecting: true };

        observers.latest().deliver([entry]);

        expect(firstListener).toHaveBeenCalledExactlyOnceWith(entry);
        expect(secondListener).not.toHaveBeenCalled();
    });

    it('keeps the shared observer running for the subscriber that remains', () => {
        const { subscription } = observe(first, { root: panel });
        const { listener } = observe(second, { root: panel });

        subscription.dispose();
        observers.latest().deliver([{ target: second, isIntersecting: true }]);

        expect(observers.latest().isConnected).toBe(true);
        expect(observers.latest().targets).toEqual([second]);
        expect(listener).toHaveBeenCalledOnce();
    });

    it('disconnects an observer once its last target is let go', () => {
        const { subscription } = observe(first, { root: panel });

        subscription.dispose();

        expect(observers.latest().isConnected).toBe(false);
        expect(IntersectionObserverPool.size).toBe(0);
    });

    it('observes the target afresh when asked to remeasure it', () => {
        const { subscription } = observe(first, { root: panel });
        const observer = observers.latest();
        observe(second, { root: panel });

        subscription.remeasure();

        expect(observer.targets).toEqual([second, first]);
    });
});

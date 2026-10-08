import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { ResizeObserverPool } from '#browser/ResizeObserverPool.mjs';
import { stubResizeObserver } from '#support/platform.js';

describe('ResizeObserverPool', () => {
    /** @type {ReturnType<typeof stubResizeObserver>} */
    let resizes;
    /** @type {Array<{ dispose(): void }>} */
    let subscriptions;
    /** @type {HTMLElement} */
    let first;
    /** @type {HTMLElement} */
    let second;

    beforeEach(() => {
        resizes = stubResizeObserver();
        subscriptions = [];
        document.body.innerHTML = '<div id="first"></div><div id="second"></div>';
        first = document.querySelector('#first');
        second = document.querySelector('#second');
    });

    afterEach(() => {
        subscriptions.splice(0).forEach(subscription => subscription.dispose());
        resizes.restore();
    });

    const observe = (target, listener = vi.fn()) => {
        subscriptions.push(ResizeObserverPool.observe(target, listener));

        return listener;
    };

    it('observes every target through one resize observer', () => {
        observe(first);
        observe(second);

        expect(resizes.instances).toHaveLength(1);
        expect(resizes.latest().targets).toEqual([first, second]);
    });

    it('observes a target once however many listen to it', () => {
        observe(first);
        observe(first);

        expect(resizes.latest().targets).toEqual([first]);
    });

    it('calls each listener of the target that resized, and no other', () => {
        const firstListener = observe(first);
        const secondListener = observe(first);
        const otherListener = observe(second);
        const entry = { target: first };

        resizes.latest().deliver([entry]);

        expect(firstListener).toHaveBeenCalledExactlyOnceWith(entry);
        expect(secondListener).toHaveBeenCalledExactlyOnceWith(entry);
        expect(otherListener).not.toHaveBeenCalled();
    });

    it('keeps observing a target while another listener remains', () => {
        observe(first);
        const kept = observe(first);

        subscriptions[0].dispose();
        resizes.latest().deliver([{ target: first }]);

        expect(resizes.latest().targets).toEqual([first]);
        expect(kept).toHaveBeenCalledOnce();
    });

    it('stops observing a target once its last listener leaves', () => {
        observe(first);
        observe(second);

        subscriptions[0].dispose();

        expect(resizes.latest().targets).toEqual([second]);
    });

    it('disconnects the observer once nothing is observed, and makes another when needed', () => {
        observe(first);
        const observer = resizes.latest();

        subscriptions.splice(0).forEach(subscription => subscription.dispose());
        observe(second);

        expect(observer.isConnected).toBe(false);
        expect(resizes.instances).toHaveLength(2);
    });

    it('takes a second disposal of a subscription as nothing', () => {
        observe(first);
        const kept = observe(first);

        subscriptions[0].dispose();
        subscriptions[0].dispose();
        resizes.latest().deliver([{ target: first }]);

        expect(kept).toHaveBeenCalledOnce();
    });
});

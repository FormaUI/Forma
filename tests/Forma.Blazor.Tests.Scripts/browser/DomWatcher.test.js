import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { attach, detach, refresh, refreshAll, release } from '#browser/DomWatcher.js';
import { DomSessionRegistry } from '#browser/DomSessionRegistry.mjs';
import { ResizeObserverPool } from '#browser/ResizeObserverPool.mjs';
import { fakeOwner } from '#support/owner.js';
import { sizeScroller, stubAnimationFrames, stubIntersectionObserver, stubMutationObserver, stubResizeObserver } from '#support/platform.js';

describe('DomWatcher', () => {
    /** @type {ReturnType<typeof stubAnimationFrames>} */
    let frames;
    /** @type {ReturnType<typeof stubResizeObserver>} */
    let resizes;
    /** @type {ReturnType<typeof stubMutationObserver>} */
    let mutations;
    /** @type {HTMLElement} */
    let track;
    /** @type {ReturnType<typeof fakeOwner>} */
    let receiver;
    /** @type {ReturnType<typeof vi.spyOn>} */
    let reported;

    const owner = 'owner-1';

    beforeEach(() => {
        vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] });
        frames = stubAnimationFrames();
        resizes = stubResizeObserver();
        mutations = stubMutationObserver();
        reported = vi.spyOn(console, 'error').mockImplementation(() => {});
        document.body.innerHTML = '<div id="track"><span>One</span></div>';
        track = document.querySelector('#track');
        sizeScroller(track, { scrollLeft: 0, scrollWidth: 900, clientWidth: 300 });
        receiver = fakeOwner();
    });

    afterEach(() => {
        release(owner);
        release('owner-2');
        reported.mockRestore();
        frames.restore();
        resizes.restore();
        mutations.restore();
        vi.useRealTimers();
    });

    const request = (generation = 1, options = {}, target = track) => ({ token: '1', generation, capability: 'scroll', target, options });

    // Lets the task after an attach run, which opens its delivery.
    const nextTask = () => vi.advanceTimersByTimeAsync(0);

    const scrollTo = offset => {
        track.scrollLeft = offset;
        track.dispatchEvent(new Event('scroll'));
        frames.flush();
    };

    it('returns the first state from the attach that starts the observation', async () => {
        const baseline = await attach(owner, receiver, request());

        expect(baseline).toEqual({ canScrollBack: false, canScrollForward: true, frozenExtent: 0, cornerExtent: 0, viewportExtent: null });
        expect(receiver.invokeMethodAsync).not.toHaveBeenCalled();
    });

    it('sends each later state to the receiver under the token and generation', async () => {
        await attach(owner, receiver, request(4));
        await nextTask();

        scrollTo(300);

        expect(receiver.invokeMethodAsync).toHaveBeenCalledExactlyOnceWith('NotifyAsync', '1', 4, expect.objectContaining({ canScrollBack: true, canScrollForward: true }));
    });

    it('holds a state measured before the task after its attach, sending only the latest', async () => {
        await attach(owner, receiver, request());

        scrollTo(300);
        scrollTo(600);
        expect(receiver.invokeMethodAsync).not.toHaveBeenCalled();

        await nextTask();

        expect(receiver.invokeMethodAsync).toHaveBeenCalledExactlyOnceWith('NotifyAsync', '1', 1, expect.objectContaining({ canScrollBack: true, canScrollForward: false }));
    });

    it('waits for the receiver to take a state before sending the next', async () => {
        let take;
        receiver.invokeMethodAsync.mockImplementationOnce(() => new Promise(resolve => { take = resolve; }));
        await attach(owner, receiver, request());
        await nextTask();

        scrollTo(300);
        scrollTo(600);
        expect(receiver.invokeMethodAsync).toHaveBeenCalledOnce();

        take();
        await nextTask();

        expect(receiver.invokeMethodAsync).toHaveBeenCalledTimes(2);
        expect(receiver.invokeMethodAsync).toHaveBeenLastCalledWith('NotifyAsync', '1', 1, expect.objectContaining({ canScrollForward: false }));
    });

    it('reports a state the receiver failed to take under the owner, the capability and the registration', async () => {
        const failure = new Error('The circuit dropped.');
        receiver.invokeMethodAsync.mockRejectedValueOnce(failure);
        await attach(owner, receiver, request(2));
        await nextTask();

        scrollTo(300);
        await nextTask();

        expect(reported).toHaveBeenCalledExactlyOnceWith(expect.stringContaining(`scroll state of owner ${owner}, registration 1.2`), failure);
    });

    it('sends the state that failed again once refreshed', async () => {
        receiver.invokeMethodAsync.mockRejectedValueOnce(new Error('The circuit dropped.'));
        await attach(owner, receiver, request());
        await nextTask();
        scrollTo(300);
        await nextTask();

        const isRefreshed = refresh(owner, '1', 1);

        expect(isRefreshed).toBe(true);
        expect(receiver.invokeMethodAsync).toHaveBeenCalledTimes(2);
        expect(receiver.invokeMethodAsync).toHaveBeenLastCalledWith('NotifyAsync', '1', 1, expect.objectContaining({ canScrollBack: true }));
    });

    it('measures afresh once refreshed, sending the state unchanged or not', async () => {
        await attach(owner, receiver, request());
        await nextTask();

        refresh(owner, '1', 1);
        frames.flush();

        expect(receiver.invokeMethodAsync).toHaveBeenCalledExactlyOnceWith('NotifyAsync', '1', 1, expect.objectContaining({ canScrollForward: true }));
    });

    it('refreshes the observations named for each owner after a reconnection, sending their state unchanged or not', async () => {
        const other = fakeOwner();
        await attach(owner, receiver, request());
        await attach('owner-2', other, request());
        await nextTask();

        refreshAll([{ owner, registrations: [{ token: '1', generation: 1 }] }, { owner: 'owner-2', registrations: [{ token: '1', generation: 1 }] }]);
        frames.flush();

        expect(receiver.invokeMethodAsync).toHaveBeenCalledExactlyOnceWith('NotifyAsync', '1', 1, expect.objectContaining({ canScrollForward: true }));
        expect(other.invokeMethodAsync).toHaveBeenCalledOnce();
    });

    it('leaves alone an observation named under a generation it no longer holds, and an owner it does not know', async () => {
        await attach(owner, receiver, request(2));
        await nextTask();

        refreshAll([{ owner, registrations: [{ token: '1', generation: 1 }] }, { owner: 'owner-gone', registrations: [{ token: '1', generation: 1 }] }]);
        frames.flush();

        expect(receiver.invokeMethodAsync).not.toHaveBeenCalled();
    });

    it('refreshes nothing for a generation it no longer holds', async () => {
        await attach(owner, receiver, request(1));

        expect(refresh(owner, '1', 2)).toBe(false);
    });

    it('replaces the observation the token holds with a later generation', async () => {
        await attach(owner, receiver, request(1));
        await attach(owner, receiver, request(2, { viewportExtent: true }));
        await nextTask();

        scrollTo(300);

        expect(receiver.invokeMethodAsync).toHaveBeenCalledExactlyOnceWith('NotifyAsync', '1', 2, expect.objectContaining({ viewportExtent: 300 }));
    });

    it('drops an attach that a later generation overtook while its capability loaded', async () => {
        const overtaken = attach(owner, receiver, request(1));
        const latest = attach(owner, receiver, request(2));

        expect(await overtaken).toBeNull();
        expect(await latest).not.toBeNull();
        expect(DomSessionRegistry.find(owner).watcher.size).toBe(1);
    });

    it('drops an attach that a detach overtook while its capability loaded', async () => {
        const overtaken = attach(owner, receiver, request(1));

        detach(owner, '1', 2);

        expect(await overtaken).toBeNull();
        expect(DomSessionRegistry.find(owner).watcher.size).toBe(0);
    });

    it('drops an attach that the owner\'s release overtook while its capability loaded', async () => {
        const overtaken = attach(owner, receiver, request(1));

        release(owner);

        expect(await overtaken).toBeNull();
        expect(ResizeObserverPool.size).toBe(0);
    });

    it('observes whether a target is in view of its root, sending each change the native observer reports', async () => {
        const intersections = stubIntersectionObserver();
        track.getBoundingClientRect = () => ({ top: 0, bottom: 300, left: 0, right: 300 });
        const sentinel = document.createElement('div');
        track.appendChild(sentinel);
        sentinel.getBoundingClientRect = () => ({ top: 500, bottom: 510, left: 0, right: 300 });
        sentinel.getClientRects = () => [{}];

        try {
            const baseline = await attach(owner, receiver, { token: '2', generation: 1, capability: 'intersection', target: sentinel, options: { root: track, rootMargin: 0 } });
            await nextTask();
            intersections.latest().deliver([{ target: sentinel, isIntersecting: true }]);

            expect(baseline).toEqual({ isIntersecting: false });
            expect(receiver.invokeMethodAsync).toHaveBeenCalledExactlyOnceWith('NotifyAsync', '2', 1, { isIntersecting: true });
        } finally {
            release(owner);
            intersections.restore();
        }
    });

    it('answers a target that has gone with no state', async () => {
        expect(await attach(owner, receiver, request(1, {}, null))).toBeNull();
    });

    it('refuses a capability it cannot observe', async () => {
        await expect(attach(owner, receiver, { ...request(), capability: 'weather' })).rejects.toThrow("The capability 'weather' cannot be observed.");
    });

    it('stops observing once detached', async () => {
        await attach(owner, receiver, request(1));
        await nextTask();

        detach(owner, '1', 2);
        scrollTo(300);

        expect(receiver.invokeMethodAsync).not.toHaveBeenCalled();
        expect(ResizeObserverPool.size).toBe(0);
    });

    it('releases an observation whose target left the document', async () => {
        await attach(owner, receiver, request());

        track.remove();
        resizes.latest().deliver([{ target: track }]);
        frames.flush();

        expect(DomSessionRegistry.find(owner).watcher.size).toBe(0);
        expect(ResizeObserverPool.size).toBe(0);
    });

    it('keeps each owner\'s observations apart', async () => {
        const other = fakeOwner();
        await attach(owner, receiver, request());
        await attach('owner-2', other, request());
        await nextTask();

        release('owner-2');
        scrollTo(300);

        expect(receiver.invokeMethodAsync).toHaveBeenCalledOnce();
        expect(other.invokeMethodAsync).not.toHaveBeenCalled();
    });

    it('releases everything the owner holds in one call', async () => {
        await attach(owner, receiver, request());
        await attach(owner, receiver, { ...request(), token: '2' });

        release(owner);

        expect(DomSessionRegistry.find(owner)).toBeNull();
        expect(ResizeObserverPool.size).toBe(0);
        expect(mutations.instances.every(instance => !instance.isConnected)).toBe(true);
    });
});

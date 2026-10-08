import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { attach, detach, release, releaseEngine, run } from '#browser/DomEngine.js';
import { attach as watch, detach as unwatch, refresh } from '#browser/DomWatcher.js';
import { describe as describeScope } from '#input/InputRouter.js';
import { fakeOwner } from '#support/owner.js';
import { sizeScroller, stubAnimationFrames, stubMutationObserver, stubResizeObserver } from '#support/platform.js';

describe('DomEngine', () => {
    /** @type {ReturnType<typeof stubAnimationFrames>} */
    let frames;
    /** @type {ReturnType<typeof stubResizeObserver>} */
    let resizes;
    /** @type {ReturnType<typeof stubMutationObserver>} */
    let mutations;
    /** @type {HTMLElement} */
    let pane;
    /** @type {HTMLElement} */
    let viewport;

    const runtimeKey = 'owner-1';

    beforeEach(() => {
        frames = stubAnimationFrames();
        resizes = stubResizeObserver();
        mutations = stubMutationObserver();
        document.body.innerHTML = '<div id="pane"><button class="back"></button><div id="viewport"></div><button class="forward"></button></div>';
        pane = document.querySelector('#pane');
        viewport = document.querySelector('#viewport');
        viewport.scrollBy = vi.fn();
        sizeScroller(viewport, { scrollLeft: 0, scrollWidth: 900, clientWidth: 300 });
    });

    afterEach(() => {
        release(runtimeKey);
        frames.restore();
        resizes.restore();
        mutations.restore();
    });

    const buttons = () => ({ container: pane, back: '.back', forward: '.forward', backAction: 'pane.scroll-back', forwardAction: 'pane.scroll-forward' });
    const request = (generation = 1, options = { wheel: 'pane.scroll', buttons: buttons() }, target = viewport) => ({ token: '1', generation, capability: 'scroll', target, options });
    const observation = () => ({ token: '2', generation: 1, capability: 'scroll', target: viewport, options: {} });

    const leave = () => {
        pane.remove();
        resizes.latest().deliver([{ target: viewport }]);
        frames.flush();
    };

    it('attaches the interaction the request names', async () => {
        const isAttached = await attach(runtimeKey, request());

        expect(isAttached).toBe(true);
        expect(describeScope(viewport).routes.map(route => route.action)).toEqual(['pane.scroll']);
        expect(describeScope(pane.querySelector('.forward')).routes.map(route => route.action)).toEqual(['pane.scroll-forward']);
    });

    it('attaches a focus interaction the request names', async () => {
        const isAttached = await attach(runtimeKey, { token: '2', generation: 1, capability: 'focus', target: pane, options: { roving: 'horizontal' } });

        expect(isAttached).toBe(true);
        expect(describeScope(pane).routes.map(route => route.action)).toEqual(['focus.next', 'focus.previous', 'focus.first', 'focus.last']);
    });

    it('steps the target from its button with no call to its owner', async () => {
        await attach(runtimeKey, request());

        pane.querySelector('.forward').dispatchEvent(new MouseEvent('click', { bubbles: true, detail: 1 }));

        expect(viewport.scrollBy).toHaveBeenCalledExactlyOnceWith({ left: 240, behavior: 'smooth' });
    });

    it('replaces the interaction the token holds with a later generation', async () => {
        await attach(runtimeKey, request(1));

        await attach(runtimeKey, request(2, { buttons: buttons() }));

        expect(describeScope(viewport)).toBeNull();
        expect(describeScope(pane.querySelector('.back'))).not.toBeNull();
    });

    it('drops an attach that a detach overtook while its capability loaded', async () => {
        const overtaken = attach(runtimeKey, request(1));

        detach(runtimeKey, '1', 2);

        expect(await overtaken).toBe(false);
        expect(describeScope(viewport)).toBeNull();
    });

    it('answers a target that has gone with no interaction', async () => {
        expect(await attach(runtimeKey, request(1, {}, null))).toBe(false);
    });

    it('refuses a capability it has no interaction for', async () => {
        await expect(attach(runtimeKey, { ...request(), capability: 'weather' })).rejects.toThrow("The capability 'weather' has no interaction.");
    });

    it('releases the interaction once detached', async () => {
        await attach(runtimeKey, request());

        detach(runtimeKey, '1', 2);

        expect(describeScope(viewport)).toBeNull();
        expect(describeScope(pane.querySelector('.forward'))).toBeNull();
    });

    it('releases an interaction whose target left the document', async () => {
        await attach(runtimeKey, request());

        leave();

        expect(describeScope(viewport)).toBeNull();
    });

    it('keeps an interaction running, and its target\'s leaving noticed, once an observation of the same target is released', async () => {
        await watch(runtimeKey, fakeOwner(), observation());
        await attach(runtimeKey, request());

        unwatch(runtimeKey, '2', 1);
        expect(describeScope(viewport).routes.map(route => route.action)).toEqual(['pane.scroll']);

        leave();
        expect(describeScope(viewport)).toBeNull();
    });

    it('releases its engine alone, leaving the owner\'s observations, and their target\'s leaving noticed', async () => {
        await watch(runtimeKey, fakeOwner(), observation());
        await attach(runtimeKey, request());

        releaseEngine(runtimeKey);
        expect(describeScope(viewport)).toBeNull();
        expect(refresh(runtimeKey, '2', 1)).toBe(true);

        leave();
        expect(refresh(runtimeKey, '2', 1)).toBe(false);
    });

    it('releases the engine and the watcher together for the owner\'s release', async () => {
        await watch(runtimeKey, fakeOwner(), observation());
        await attach(runtimeKey, request());

        release(runtimeKey);

        expect(describeScope(viewport)).toBeNull();
        expect(refresh(runtimeKey, '2', 1)).toBe(false);
        expect(resizes.latest().isConnected).toBe(false);
    });

    describe('run', () => {
        it('runs a capability\'s command on the target with no interaction attached first', async () => {
            const isMoving = await run('scroll', 'step', viewport, 1, { axis: 'inline' });

            expect(isMoving).toBe(true);
            expect(viewport.scrollBy).toHaveBeenCalledExactlyOnceWith({ left: 240, behavior: 'smooth' });
        });

        it('answers a target that has gone with nothing done', async () => {
            expect(await run('scroll', 'step', null, 1)).toBe(false);
        });

        it('refuses a command the capability lacks', async () => {
            await expect(run('scroll', 'explode', viewport)).rejects.toThrow("The capability 'scroll' has no command 'explode'.");
        });

        it('refuses what every class inherits as a command', async () => {
            await expect(run('scroll', 'call', viewport)).rejects.toThrow("The capability 'scroll' has no command 'call'.");
        });
    });
});

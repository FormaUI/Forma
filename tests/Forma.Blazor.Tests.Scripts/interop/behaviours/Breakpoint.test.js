import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { attach } from '#interop/behaviours/Breakpoint.js';
import { fakeOwner } from '#support/owner.js';
import { stubResizeObserver } from '#support/platform.js';

describe('Breakpoint', () => {
    /** @type {ReturnType<typeof stubResizeObserver>} */
    let resizes;
    /** @type {HTMLElement} */
    let target;
    /** @type {ReturnType<typeof fakeOwner>} */
    let owner;

    beforeEach(() => {
        resizes = stubResizeObserver();
        document.body.innerHTML = '<div id="target"></div>';
        target = document.querySelector('#target');
        target.style.setProperty('--layout', 'wide');
        owner = fakeOwner();
    });

    afterEach(() => {
        resizes.restore();
    });

    it('reports the tier the property names on attach', () => {
        attach(target, { owner, property: '--layout' });

        expect(owner.invokeMethodAsync).toHaveBeenCalledWith('HandleBreakpointAsync', 'wide');
    });

    it('reports again when a layout pass changes the tier', () => {
        attach(target, { owner, property: '--layout' });

        target.style.setProperty('--layout', 'compact');
        resizes.latest().deliver([]);

        expect(owner.invokeMethodAsync).toHaveBeenLastCalledWith('HandleBreakpointAsync', 'compact');
        expect(owner.invokeMethodAsync).toHaveBeenCalledTimes(2);
    });

    it('stays quiet through a layout pass that leaves the tier alone', () => {
        attach(target, { owner, property: '--layout' });

        resizes.latest().deliver([]);
        resizes.latest().deliver([]);

        expect(owner.invokeMethodAsync).toHaveBeenCalledTimes(1);
    });

    it('stays quiet once the target has left the document', () => {
        attach(target, { owner, property: '--layout' });

        target.remove();
        target.style.setProperty('--layout', 'compact');
        resizes.latest().deliver([]);

        expect(owner.invokeMethodAsync).toHaveBeenCalledTimes(1);
    });

    it('stops watching on dispose', () => {
        const handle = attach(target, { owner, property: '--layout' });

        handle.dispose();

        expect(resizes.latest().isConnected).toBe(false);
    });
});

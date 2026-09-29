import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { attach } from '#browser/behaviors/GridColumns.js';
import { fakeOwner } from '#support/owner.js';
import { stubResizeObserver } from '#support/platform.js';

describe('GridColumns', () => {
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
        target.style.gridTemplateColumns = '200px 200px 200px';
        owner = fakeOwner();
    });

    afterEach(() => {
        resizes.restore();
    });

    it('reports how many tracks the grid lays out on attach', () => {
        attach(target, { owner });

        expect(owner.invokeMethodAsync).toHaveBeenCalledWith('HandleGridColumnsAsync', 3);
    });

    it('reports again when a layout pass changes the count', () => {
        attach(target, { owner });

        target.style.gridTemplateColumns = '300px 300px';
        resizes.latest().deliver([]);

        expect(owner.invokeMethodAsync).toHaveBeenLastCalledWith('HandleGridColumnsAsync', 2);
        expect(owner.invokeMethodAsync).toHaveBeenCalledTimes(2);
    });

    it('stays quiet through a layout pass that only resizes the tracks', () => {
        attach(target, { owner });

        target.style.gridTemplateColumns = '210px 210px 210px';
        resizes.latest().deliver([]);

        expect(owner.invokeMethodAsync).toHaveBeenCalledTimes(1);
    });

    it('counts the tracks between line names', () => {
        target.style.gridTemplateColumns = '[start] 100px [middle] 100px [end]';

        attach(target, { owner });

        expect(owner.invokeMethodAsync).toHaveBeenCalledWith('HandleGridColumnsAsync', 2);
    });

    it('reports one column for a target that lays out no grid', () => {
        target.style.gridTemplateColumns = '';

        attach(target, { owner });

        expect(owner.invokeMethodAsync).toHaveBeenCalledWith('HandleGridColumnsAsync', 1);
    });

    it('stays quiet once the target has left the document', () => {
        attach(target, { owner });

        target.remove();
        target.style.gridTemplateColumns = '100px';
        resizes.latest().deliver([]);

        expect(owner.invokeMethodAsync).toHaveBeenCalledTimes(1);
    });

    it('stops watching on dispose', () => {
        const handle = attach(target, { owner });

        handle.dispose();

        expect(resizes.latest().isConnected).toBe(false);
    });
});

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { attach } from '#browser/components/Virtualize.js';
import { fakeOwner } from '#support/owner.js';
import { sizeScroller, stubAnimationFrames, stubMutationObserver, stubResizeObserver } from '#support/platform.js';

describe('Virtualize', () => {
    /** @type {ReturnType<typeof stubAnimationFrames>} */
    let frames;
    /** @type {ReturnType<typeof stubResizeObserver>} */
    let resizes;
    /** @type {ReturnType<typeof stubMutationObserver>} */
    let drawings;
    /** @type {ReturnType<typeof fakeOwner>} */
    let owner;
    /** @type {HTMLElement} */
    let scroller;
    /** @type {HTMLElement} */
    let start;

    const handles = [];
    const attachList = (overrides = {}) => {
        const handle = attach(start, { owner, spacerAttribute: 'data-virtualize-spacer', ...overrides });
        handles.push(handle);

        return handle;
    };

    // The scroller stands 100px down the page with a 300px viewport, scrolled 400px into a list whose start it has passed.
    const place = (element, top, height) => {
        element.getBoundingClientRect = () => ({ top, bottom: top + height, height, left: 0, right: 200, width: 200, x: 0, y: top });
    };

    beforeEach(() => {
        frames = stubAnimationFrames();
        resizes = stubResizeObserver();
        drawings = stubMutationObserver();
        owner = fakeOwner();
        document.body.innerHTML = `
            <div id="scroller" style="overflow-y: auto">
                <div id="list">
                    <div id="start" data-virtualize-spacer="start"></div>
                    <div class="item" id="first">First</div>
                    <div class="item" id="second">Second</div>
                    <div data-virtualize-spacer="gap"></div>
                    <div class="item" id="third">Third</div>
                    <div data-virtualize-spacer="end"></div>
                </div>
            </div>`;
        scroller = document.querySelector('#scroller');
        start = document.querySelector('#start');
        sizeScroller(scroller, { clientHeight: 300, clientTop: 0, scrollTop: 400 });
        place(scroller, 100, 300);
        place(start, -300, 0);
        place(document.querySelector('#first'), -300, 20);
        place(document.querySelector('#second'), -280, 20);
        place(document.querySelector('#third'), 460, 40);
    });

    afterEach(() => {
        handles.splice(0).forEach(handle => handle.dispose());
        frames.restore();
        resizes.restore();
        drawings.restore();
    });

    it('reports how far the viewport has scrolled into the list, its height and the room an item drawn takes', () => {
        attachList();

        frames.flush();

        expect(owner.invokeMethodAsync).toHaveBeenCalledWith('HandleViewportAsync', 400, 300, 20);
    });

    it('counts the space between two items as part of the room an item takes', () => {
        place(document.querySelector('#second'), -268, 20);

        attachList();
        frames.flush();

        expect(owner.invokeMethodAsync).toHaveBeenCalledWith('HandleViewportAsync', 400, 300, 32);
    });

    it('measures the items by their own heights where no two drawn items stand together', () => {
        document.querySelector('#second').remove();

        attachList();
        frames.flush();

        expect(owner.invokeMethodAsync).toHaveBeenCalledWith('HandleViewportAsync', 400, 300, 30);
    });

    it('reports once a frame however many changes arrive in it', () => {
        attachList();
        frames.flush();

        place(start, -350, 0);
        scroller.dispatchEvent(new Event('scroll'));
        scroller.dispatchEvent(new Event('scroll'));
        frames.flush();

        expect(owner.invokeMethodAsync).toHaveBeenCalledTimes(2);
        expect(owner.invokeMethodAsync).toHaveBeenLastCalledWith('HandleViewportAsync', 450, 300, 20);
    });

    it('stays quiet through a change that moves nothing it reports', () => {
        attachList();
        frames.flush();

        resizes.latest().deliver([]);
        drawings.latest().deliver([]);
        frames.flush();

        expect(owner.invokeMethodAsync).toHaveBeenCalledTimes(1);
    });

    it('scrolls at once so the point asked for stands at the top of the viewport', () => {
        const handle = attachList();
        scroller.scrollTo = vi.fn();

        handle.scrollTo(1000);

        expect(scroller.scrollTo).toHaveBeenCalledWith({ top: 1000, behavior: 'instant' });
    });

    it('brings a drawn item into view by the least scroll, and says whether it is in view', () => {
        const handle = attachList();
        const third = document.querySelector('#third');
        third.scrollIntoView = vi.fn(() => place(third, 360, 40));

        const inView = handle.reveal(2, 'nearest');

        expect(third.scrollIntoView).toHaveBeenCalledWith({ block: 'nearest', inline: 'nearest', behavior: 'instant' });
        expect(inView).toBe(true);
    });

    it('aligns a drawn item to the top when asked', () => {
        const handle = attachList();
        const second = document.querySelector('#second');
        second.scrollIntoView = vi.fn();

        handle.reveal(1, 'start');

        expect(second.scrollIntoView).toHaveBeenCalledWith({ block: 'start', inline: 'nearest', behavior: 'instant' });
    });

    it('answers false for an item it does not draw', () => {
        const handle = attachList();

        expect(handle.reveal(3, 'nearest')).toBe(false);
    });

    it('stands in the window where nothing around the list scrolls', () => {
        scroller.style.overflowY = 'visible';
        place(start, -120, 0);

        attachList();
        frames.flush();

        expect(owner.invokeMethodAsync).toHaveBeenCalledWith('HandleViewportAsync', 120, window.innerHeight, 20);
    });

    it('stops reporting once disposed', () => {
        const handle = attachList();
        frames.flush();

        handle.dispose();
        place(start, -500, 0);
        scroller.dispatchEvent(new Event('scroll'));
        frames.flush();

        expect(owner.invokeMethodAsync).toHaveBeenCalledTimes(1);
        expect(resizes.latest().isConnected).toBe(false);
        expect(drawings.latest().isConnected).toBe(false);
    });
});

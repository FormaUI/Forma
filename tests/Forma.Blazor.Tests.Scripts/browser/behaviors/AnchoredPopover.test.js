import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { attach } from '#browser/behaviors/AnchoredPopover.js';
import { stubAnimationFrames, stubResizeObserver } from '#support/platform.js';

describe('AnchoredPopover', () => {
    /** @type {ReturnType<typeof stubResizeObserver>} */
    let resizes;
    /** @type {ReturnType<typeof stubAnimationFrames>} */
    let frames;
    /** @type {HTMLElement} */
    let anchor;
    /** @type {HTMLElement} */
    let popover;

    function setAnchorRect(rect) {
        anchor.getBoundingClientRect = () => rect;
    }

    function setPopupHeight(height) {
        Object.defineProperty(popover, 'offsetHeight', { value: height, configurable: true });
    }

    beforeEach(() => {
        resizes = stubResizeObserver();
        frames = stubAnimationFrames();
        document.body.innerHTML = '<button id="anchor">Open</button><div id="popover"></div>';
        anchor = document.querySelector('#anchor');
        popover = document.querySelector('#popover');

        setAnchorRect({ top: 100, bottom: 130, left: 50, right: 90, width: 40, height: 30 });
        Object.defineProperty(popover, 'offsetWidth', { value: 200, configurable: true });
        setPopupHeight(150);

        globalThis.innerWidth = 1024;
        globalThis.innerHeight = 768;
        popover.showPopover = vi.fn();
        popover.matches = () => false;
    });

    afterEach(() => {
        resizes.restore();
        frames.restore();
    });

    it('places the popover below the anchor when it fits', () => {
        const handle = attach(popover, { anchor });

        handle.open();

        expect(popover.style.left).toBe('50px');
        expect(popover.style.top).toBe('134px');
    });

    it('flips above the anchor when there is no room below', () => {
        setAnchorRect({ top: 700, bottom: 730, left: 50, right: 90, width: 40, height: 30 });
        const handle = attach(popover, { anchor });

        handle.open();

        expect(popover.style.top).toBe('546px');
    });

    it('mirrors the inline start for a right-to-left anchor', () => {
        setAnchorRect({ top: 100, bottom: 130, left: 360, right: 400, width: 40, height: 30 });
        anchor.style.direction = 'rtl';
        const handle = attach(popover, { anchor, align: 'start' });

        handle.open();

        // The anchor's right edge minus the popover's width.
        expect(popover.style.left).toBe('200px');
    });

    // By default the popover extends toward the wider side of the viewport, whichever way the text runs.
    it('extends leftward from an anchor in the right half by default', () => {
        setAnchorRect({ top: 100, bottom: 130, left: 900, right: 940, width: 40, height: 30 });
        const handle = attach(popover, { anchor });

        handle.open();

        expect(popover.style.left).toBe('740px');
    });

    it('extends rightward from a right-to-left anchor in the left half by default', () => {
        setAnchorRect({ top: 100, bottom: 130, left: 360, right: 400, width: 40, height: 30 });
        anchor.style.direction = 'rtl';
        const handle = attach(popover, { anchor });

        handle.open();

        expect(popover.style.left).toBe('360px');
    });

    it('extends toward the reading start from an anchor at dead center', () => {
        setAnchorRect({ top: 100, bottom: 130, left: 492, right: 532, width: 40, height: 30 });
        const leftToRight = attach(popover, { anchor });
        leftToRight.open();
        const leftToRightPosition = popover.style.left;
        leftToRight.close();

        anchor.style.direction = 'rtl';
        const rightToLeft = attach(popover, { anchor });
        rightToLeft.open();

        expect(leftToRightPosition).toBe('332px');
        expect(popover.style.left).toBe('492px');
    });

    it('lines the popover up with the anchor end edge when aligned to the end', () => {
        setAnchorRect({ top: 100, bottom: 130, left: 360, right: 400, width: 40, height: 30 });
        const handle = attach(popover, { anchor, align: 'end' });

        handle.open();

        expect(popover.style.left).toBe('200px');
    });

    it('mirrors the end alignment for a right-to-left anchor', () => {
        anchor.style.direction = 'rtl';
        const handle = attach(popover, { anchor, align: 'end' });

        handle.open();

        // The reading-end edge is the anchor's left edge, so the popover starts there.
        expect(popover.style.left).toBe('50px');
    });

    it('clamps the inline start so the popover never opens off-screen', () => {
        setAnchorRect({ top: 100, bottom: 130, left: 5, right: 45, width: 40, height: 30 });
        anchor.style.direction = 'rtl';
        const handle = attach(popover, { anchor, align: 'start' });

        handle.open();

        // The mirrored position, right minus width, is negative, so the gap floors it.
        expect(popover.style.left).toBe('4px');
    });

    it('recalculates position when the popover itself changes size after opening', () => {
        setAnchorRect({ top: 700, bottom: 730, left: 50, right: 90, width: 40, height: 30 });
        setPopupHeight(30);
        const handle = attach(popover, { anchor });
        handle.open();
        expect(popover.style.top).toBe('734px');

        setPopupHeight(200);
        resizes.latest().deliver([{ target: popover }]);
        frames.flush();

        expect(popover.style.top).toBe('496px');
    });

    it('stops observing once closed', () => {
        const handle = attach(popover, { anchor });
        handle.open();
        const observer = resizes.latest();

        handle.close();

        expect(observer.isConnected).toBe(false);
    });

    it('hides the popover once closed, so an element that stays in the document disappears', () => {
        popover.hidePopover = vi.fn();
        const handle = attach(popover, { anchor });
        handle.open();
        popover.matches = selector => selector === ':popover-open';

        handle.close();

        expect(popover.hidePopover).toHaveBeenCalledOnce();
    });

    it('does not hide a popover that is not showing', () => {
        popover.hidePopover = vi.fn();
        const handle = attach(popover, { anchor });

        handle.close();

        expect(popover.hidePopover).not.toHaveBeenCalled();
    });

    it('returns focus to the trigger on a close that asks for it while focus is inside', () => {
        popover.innerHTML = '<button id="inside">Inside</button>';
        const handle = attach(popover, { anchor });
        handle.open(anchor);
        popover.querySelector('#inside').focus();

        handle.close(true);

        expect(document.activeElement).toBe(anchor);
    });

    it('leaves focus alone on a close that asks for it while focus is elsewhere', () => {
        document.body.insertAdjacentHTML('beforeend', '<button id="elsewhere">Elsewhere</button>');
        const handle = attach(popover, { anchor });
        handle.open(anchor);
        document.querySelector('#elsewhere').focus();

        handle.close(true);

        expect(document.activeElement.id).toBe('elsewhere');
    });

    it('leaves focus alone on a close that does not ask for it', () => {
        popover.innerHTML = '<button id="inside">Inside</button>';
        const handle = attach(popover, { anchor });
        handle.open(anchor);
        popover.querySelector('#inside').focus();

        handle.close();

        expect(document.activeElement.id).toBe('inside');
    });

    it('repositions when the browser window scrolls or resizes', () => {
        const handle = attach(popover, { anchor });
        handle.open();

        setAnchorRect({ top: 700, bottom: 730, left: 50, right: 90, width: 40, height: 30 });
        dispatchEvent(new Event('scroll'));
        frames.flush();

        expect(popover.style.top).toBe('546px');
    });
});


describe('AnchoredPopover keeping focus', () => {
    beforeEach(() => {
        stubResizeObserver();
        document.body.innerHTML = '<button id="anchor">Open</button><div id="popover"><button id="one">One</button><button id="two">Two</button><button id="three">Three</button></div><button id="after">After</button>';
    });

    function tab(shiftKey = false) {
        const event = new KeyboardEvent('keydown', { key: 'Tab', shiftKey, bubbles: true, cancelable: true });
        document.activeElement.dispatchEvent(event);

        return event;
    }

    it('moves Tab between the popover controls and wraps at the end', () => {
        const handle = attach(document.querySelector('#popover'), { anchor: document.querySelector('#anchor'), keepsFocus: true });
        handle.open();
        document.querySelector('#three').focus();

        const event = tab();

        expect(document.activeElement.id).toBe('one');
        expect(event.defaultPrevented).toBe(true);
    });

    it('moves Shift+Tab back and wraps at the start', () => {
        const handle = attach(document.querySelector('#popover'), { anchor: document.querySelector('#anchor'), keepsFocus: true });
        handle.open();
        document.querySelector('#one').focus();

        tab(true);

        expect(document.activeElement.id).toBe('three');
    });

    it('leaves Tab to the page when the popover does not keep focus', () => {
        const handle = attach(document.querySelector('#popover'), { anchor: document.querySelector('#anchor') });
        handle.open();
        document.querySelector('#three').focus();

        const event = tab();

        expect(event.defaultPrevented).toBe(false);
    });
});

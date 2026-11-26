import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { attach } from '#browser/behaviors/AnchoredPopover.js';
import { FocusHeirs } from '#browser/focus/FocusHeirs.js';
import { stubAnimationFrames, stubMutationObserver, stubResizeObserver } from '#support/platform.js';

describe('AnchoredPopover', () => {
    /** @type {ReturnType<typeof stubResizeObserver>} */
    let resizes;
    /** @type {ReturnType<typeof stubMutationObserver>} */
    let mutations;
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
        mutations = stubMutationObserver();
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
        mutations.restore();
        frames.restore();
    });

    describe('landing the focus as it opens', () => {
        const fill = () => {
            popover.innerHTML = `
                <input id="search" />
                <button role="menuitem" id="first">First</button>
                <button role="menuitem" id="off" disabled>Off</button>
                <button role="menuitem" id="last">Last</button>`;
        };

        it('focuses the first or the last of the controls it names as it shows, in the same task', () => {
            fill();
            const handle = attach(popover, { anchor, focusSelector: '[role="menuitem"]:not(:disabled)' });

            handle.open(anchor, 'first');
            const first = document.activeElement?.id;
            handle.close();
            handle.open(anchor, 'last');

            expect(first).toBe('first');
            expect(document.activeElement?.id).toBe('last');
        });

        it('focuses the first focusable control where it names none, and leaves the focus where it stands where no end is asked', () => {
            fill();
            anchor.focus();
            const handle = attach(popover, { anchor });

            handle.open(anchor);
            const unmoved = document.activeElement?.id;
            handle.close();
            handle.open(anchor, 'first');

            expect(unmoved).toBe('anchor');
            expect(document.activeElement?.id).toBe('search');
        });
    });

    it('places the popover below the anchor when it fits', () => {
        const handle = attach(popover, { anchor });

        handle.open();

        expect(popover.style.left).toBe('50px');
        expect(popover.style.top).toBe('134px');
    });

    it('places the popover above the anchor where asked and it fits', () => {
        setAnchorRect({ top: 400, bottom: 430, left: 50, right: 90, width: 40, height: 30 });
        const handle = attach(popover, { anchor, side: 'above' });

        handle.open();

        expect(popover.style.top).toBe('246px');
    });

    it('places a popover asked above below the anchor where there is no room above', () => {
        const handle = attach(popover, { anchor, side: 'above' });

        handle.open();

        expect(popover.style.top).toBe('134px');
    });

    it('centers the popover on the anchor where asked', () => {
        setAnchorRect({ top: 400, bottom: 430, left: 300, right: 340, width: 40, height: 30 });
        const handle = attach(popover, { anchor, align: 'center' });

        handle.open();

        expect(popover.style.left).toBe('220px');
    });

    it('publishes the anchor\'s inline size on the popover, again as the anchor widens', () => {
        const handle = attach(popover, { anchor });
        handle.open();
        const opened = popover.style.getPropertyValue('--forma-popover-anchor-inline-size');
        setAnchorRect({ top: 100, bottom: 130, left: 50, right: 370.4, width: 320.4, height: 30 });

        resizes.latest().deliver([{ target: popover }]);
        frames.flush();

        expect(opened).toBe('40px');
        expect(popover.style.getPropertyValue('--forma-popover-anchor-inline-size')).toBe('320px');
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

    it('returns focus to the trigger on a close that asks for it while focus has fallen to the page', () => {
        popover.innerHTML = '<button id="inside">Inside</button>';
        const handle = attach(popover, { anchor });
        handle.open(anchor);
        popover.querySelector('#inside').focus();
        popover.querySelector('#inside').blur();

        handle.close(true);

        expect(document.activeElement).toBe(anchor);
    });

    it('hands the focus to the heir the surface that took the trigger away names on a close that asks for it', () => {
        document.body.insertAdjacentHTML('beforeend', '<button id="next">Next</button>');
        popover.innerHTML = '<button id="inside">Inside</button>';
        const handle = attach(popover, { anchor });
        handle.open(anchor);
        popover.querySelector('#inside').focus();
        const registration = FocusHeirs.register(element => {
            if (element !== anchor) {
                return false;
            }

            document.querySelector('#next').focus();
            return true;
        });
        anchor.remove();

        handle.close(true);
        registration.dispose();

        expect(document.activeElement.id).toBe('next');
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

    it('places the popover again when the direction changes while it is open, with nothing resized or scrolled', () => {
        setAnchorRect({ top: 100, bottom: 130, left: 600, right: 640, width: 40, height: 30 });
        const handle = attach(popover, { anchor, align: 'start' });
        handle.open();
        expect(popover.style.left).toBe('600px');

        anchor.style.direction = 'rtl';
        mutations.latest().deliver([{ type: 'attributes', attributeName: 'dir', target: document.documentElement }]);
        frames.flush();

        expect(popover.style.left).toBe('440px');
    });

    it('reads the direction of the anchor itself over that of the document', () => {
        setAnchorRect({ top: 100, bottom: 130, left: 600, right: 640, width: 40, height: 30 });
        document.documentElement.style.direction = 'rtl';
        anchor.style.direction = 'ltr';

        try {
            attach(popover, { anchor, align: 'start' }).open();

            expect(popover.style.left).toBe('600px');
        } finally {
            document.documentElement.style.direction = '';
        }
    });

    it('watches every dir attribute while open, and stops once closed', () => {
        const handle = attach(popover, { anchor });
        handle.open();
        const observer = mutations.latest();
        expect(observer.targets).toEqual([document.documentElement]);

        handle.close();

        expect(observer.targets).toEqual([]);
        expect(observer.isConnected).toBe(false);
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

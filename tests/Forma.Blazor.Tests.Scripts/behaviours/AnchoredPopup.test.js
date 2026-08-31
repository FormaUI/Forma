import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { attach } from '#behaviours/AnchoredPopup.js';
import { stubResizeObserver } from '#support/platform.js';

describe('AnchoredPopup', () => {
    /** @type {ReturnType<typeof stubResizeObserver>} */
    let resizes;
    /** @type {HTMLElement} */
    let anchor;
    /** @type {HTMLElement} */
    let popup;

    function setAnchorRect(rect) {
        anchor.getBoundingClientRect = () => rect;
    }

    function setPopupHeight(height) {
        Object.defineProperty(popup, 'offsetHeight', { value: height, configurable: true });
    }

    beforeEach(() => {
        resizes = stubResizeObserver();
        document.body.innerHTML = '<button id="anchor">Open</button><div id="popup"></div>';
        anchor = document.querySelector('#anchor');
        popup = document.querySelector('#popup');

        setAnchorRect({ top: 100, bottom: 130, left: 50, right: 90, width: 40, height: 30 });
        Object.defineProperty(popup, 'offsetWidth', { value: 200, configurable: true });
        setPopupHeight(150);

        globalThis.innerWidth = 1024;
        globalThis.innerHeight = 768;
        popup.showPopover = vi.fn();
        popup.matches = () => false;
    });

    afterEach(() => resizes.restore());

    it('places the popup below the anchor when it fits', () => {
        const handle = attach(popup, { anchor });

        handle.open();

        expect(popup.style.left).toBe('50px');
        expect(popup.style.top).toBe('134px');
    });

    it('flips above the anchor when there is no room below', () => {
        setAnchorRect({ top: 700, bottom: 730, left: 50, right: 90, width: 40, height: 30 });
        const handle = attach(popup, { anchor });

        handle.open();

        expect(popup.style.top).toBe('546px');
    });

    it('mirrors the inline start for a right-to-left anchor', () => {
        setAnchorRect({ top: 100, bottom: 130, left: 360, right: 400, width: 40, height: 30 });
        anchor.style.direction = 'rtl';
        const handle = attach(popup, { anchor });

        handle.open();

        // Hangs from the anchor's right edge minus the popup's width, not its left edge.
        expect(popup.style.left).toBe('200px');
    });

    it('clamps the inline start so the popup never opens off-screen', () => {
        setAnchorRect({ top: 100, bottom: 130, left: 5, right: 45, width: 40, height: 30 });
        anchor.style.direction = 'rtl';
        const handle = attach(popup, { anchor });

        handle.open();

        // The mirrored position (right - width) would be negative; the gap floors it instead.
        expect(popup.style.left).toBe('4px');
    });

    // The popup's own content can grow after it opens — a progressively loaded picker, a search
    // result count, a category switch — so its size alone must trigger repositioning, not only
    // a window resize or scroll.
    it('recalculates position when the popup itself changes size after opening', () => {
        setAnchorRect({ top: 700, bottom: 730, left: 50, right: 90, width: 40, height: 30 });
        setPopupHeight(30);
        const handle = attach(popup, { anchor });
        handle.open();
        expect(popup.style.top).toBe('734px');

        // The picker finished loading and is now far taller than the tiny box measured at open time.
        setPopupHeight(200);
        resizes.latest().deliver([{ target: popup }]);

        expect(popup.style.top).toBe('496px');
    });

    it('stops observing once closed', () => {
        const handle = attach(popup, { anchor });
        handle.open();
        const observer = resizes.latest();

        handle.close();

        expect(observer.isConnected).toBe(false);
    });

    it('repositions when the window scrolls or resizes', () => {
        const handle = attach(popup, { anchor });
        handle.open();

        setAnchorRect({ top: 700, bottom: 730, left: 50, right: 90, width: 40, height: 30 });
        dispatchEvent(new Event('scroll'));

        expect(popup.style.top).toBe('546px');
    });
});


describe('AnchoredPopup keeping focus', () => {
    beforeEach(() => {
        stubResizeObserver();
        document.body.innerHTML = '<button id="anchor">Open</button><div id="popup"><button id="one">One</button><button id="two">Two</button><button id="three">Three</button></div><button id="after">After</button>';
    });

    function tab(shiftKey = false) {
        const event = new KeyboardEvent('keydown', { key: 'Tab', shiftKey, bubbles: true, cancelable: true });
        document.activeElement.dispatchEvent(event);

        return event;
    }

    it('moves Tab between the popup controls and wraps at the end', () => {
        const handle = attach(document.querySelector('#popup'), { anchor: document.querySelector('#anchor'), keepsFocus: true });
        handle.open();
        document.querySelector('#three').focus();

        const event = tab();

        expect(document.activeElement.id).toBe('one');
        expect(event.defaultPrevented).toBe(true);
    });

    it('moves Shift+Tab back and wraps at the start', () => {
        const handle = attach(document.querySelector('#popup'), { anchor: document.querySelector('#anchor'), keepsFocus: true });
        handle.open();
        document.querySelector('#one').focus();

        tab(true);

        expect(document.activeElement.id).toBe('three');
    });

    it('leaves Tab to the page when the popup does not keep focus', () => {
        const handle = attach(document.querySelector('#popup'), { anchor: document.querySelector('#anchor') });
        handle.open();
        document.querySelector('#three').focus();

        const event = tab();

        expect(event.defaultPrevented).toBe(false);
    });
});

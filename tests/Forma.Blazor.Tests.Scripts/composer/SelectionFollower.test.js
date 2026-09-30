import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { attach } from '#composer/SelectionFollower.js';
import { stack } from '#input/InputRouter.js';
import { pointerEvent, stubAnimationFrames, stubPointerCapture, stubResizeObserver } from '#support/platform.js';

// The handles attached in a test, whose drag registrations the input router holds until they are disposed after it.
const handles = [];
const attachFollower = (target, owner, options) => {
    const handle = attach(target, owner, options);
    handles.push(handle);

    return handle;
};

afterEach(() => handles.splice(0).forEach(handle => handle.dispose()));

describe('SelectionFollower', () => {
    /** @type {HTMLElement} */
    let editor;
    /** @type {HTMLElement} */
    let menu;
    /** @type {ReturnType<typeof stubResizeObserver>} */
    let resizes;
    /** @type {ReturnType<typeof stubAnimationFrames>} */
    let frames;

    beforeEach(() => {
        resizes = stubResizeObserver();
        frames = stubAnimationFrames();
        document.body.innerHTML = `
            <div id="editor" contenteditable="true">
                <p id="text">Selected content</p>
            </div>
            <div id="menu" class="selection-bar"><button id="handle">Move</button><button id="bold">Bold</button></div>
        `;
        editor = document.querySelector('#editor');
        menu = document.querySelector('#menu');

        menu.getBoundingClientRect = () => ({
            top: 0,
            bottom: 30,
            left: 0,
            right: 120,
            width: 120,
            height: 30,
            x: 0,
            y: 0,
        });

        globalThis.innerWidth = 1024;
        globalThis.innerHeight = 768;
    });

    afterEach(() => frames.restore());

    const selectText = (collapsed = false, outside = false) => {
        const textNode = (outside ? document.body : document.querySelector('#text')).firstChild;
        const range = document.createRange();

        if (collapsed) {
            range.setStart(textNode, 0);
            range.collapse(true);
        } else {
            range.setStart(textNode, 0);
            range.setEnd(textNode, 8);
        }

        range.getBoundingClientRect = () => (collapsed ? {
            top: 100, bottom: 120, left: 200, right: 200, width: 0, height: 20, x: 200, y: 100,
        } : {
            top: 100, bottom: 120, left: 200, right: 280, width: 80, height: 20, x: 200, y: 100,
        });

        const sel = window.getSelection();
        sel.removeAllRanges();
        sel.addRange(range);

        return range;
    };

    const select = () => {
        const range = selectText(false, false);
        document.dispatchEvent(new Event('selectionchange'));
        frames.flush();

        return range;
    };

    it('shows the selection bar beneath a range selected inside the editor', () => {
        attachFollower(menu, editor, { activeClass: 'is-visible' });
        select();

        expect(menu.classList.contains('is-visible')).toBe(true);
        expect(menu.style.top).toBe('128px'); // 120 (bottom) + 8 (gap)
        expect(menu.style.left).toBe('180px'); // 200 + 80 / 2 - 120 / 2
    });

    it('shows the bar at once for a selection that already exists when it attaches', () => {
        selectText(false, false);

        attachFollower(menu, editor, { activeClass: 'is-visible' });

        expect(menu.classList.contains('is-visible')).toBe(true);
        expect(menu.style.top).toBe('128px');
    });

    it('places the bar above the selection when asked to', () => {
        attachFollower(menu, editor, { activeClass: 'is-visible', placement: 'above' });
        select();

        expect(menu.style.top).toBe('62px'); // 100 (top) - 30 (height) - 8 (gap)
    });

    it('hides the menu when selection is collapsed', () => {
        attachFollower(menu, editor, { activeClass: 'is-visible' });
        select();
        expect(menu.classList.contains('is-visible')).toBe(true);

        selectText(true, false);
        document.dispatchEvent(new Event('selectionchange'));
        frames.flush();

        expect(menu.classList.contains('is-visible')).toBe(false);
    });

    it('hides the menu when selection is outside the editor', () => {
        attachFollower(menu, editor, { activeClass: 'is-visible' });
        selectText(false, true);

        document.dispatchEvent(new Event('selectionchange'));
        frames.flush();

        expect(menu.classList.contains('is-visible')).toBe(false);
    });

    it('moves above the selection when the viewport leaves no room beneath', () => {
        attachFollower(menu, editor, { activeClass: 'is-visible' });
        const range = selectText(false, false);
        range.getBoundingClientRect = () => ({
            top: 730, bottom: 750, left: 200, right: 280, width: 80, height: 20, x: 200, y: 730,
        });

        document.dispatchEvent(new Event('selectionchange'));
        frames.flush();

        expect(menu.style.top).toBe('692px'); // 730 (top) - 30 (height) - 8 (gap)
    });

    it('moves beneath the selection when asked above with no room there', () => {
        attachFollower(menu, editor, { activeClass: 'is-visible', placement: 'above' });
        const range = selectText(false, false);
        range.getBoundingClientRect = () => ({
            top: 10, bottom: 30, left: 200, right: 280, width: 80, height: 20, x: 200, y: 10,
        });

        document.dispatchEvent(new Event('selectionchange'));
        frames.flush();

        expect(menu.style.top).toBe('38px'); // 30 (bottom) + 8 (gap)
    });

    it('keeps the displacement the handle was dragged by across selections', () => {
        attachFollower(menu, editor, { activeClass: 'is-visible', handleSelector: '#handle' });
        select();

        const handle = document.querySelector('#handle');
        const pointer = (type, target, clientX, clientY) => target.dispatchEvent(pointerEvent(type, { clientX, clientY }));
        stubPointerCapture(menu);

        pointer('pointerdown', handle, 50, 140);
        pointer('pointermove', menu, 50, 180);
        pointer('pointerup', menu, 50, 180);

        expect(menu.style.top).toBe('168px'); // 128 + 40 dragged

        select();

        expect(menu.style.top).toBe('168px');
    });

    it('settles a drag past the viewport edge at the edge, with no dead distance to drag back', () => {
        attachFollower(menu, editor, { activeClass: 'is-visible', handleSelector: '#handle' });
        select();

        const handle = document.querySelector('#handle');
        const pointer = (type, target, clientX, clientY) => target.dispatchEvent(pointerEvent(type, { clientX, clientY }));
        stubPointerCapture(menu);

        pointer('pointerdown', handle, 50, 140);
        pointer('pointermove', menu, 50, 2000);
        pointer('pointerup', menu, 50, 2000);
        expect(menu.style.top).toBe('730px'); // 768 - 30 (height) - 8 (gap)

        pointer('pointerdown', handle, 50, 140);
        pointer('pointermove', menu, 50, 130);
        pointer('pointerup', menu, 50, 130);

        expect(menu.style.top).toBe('720px');
    });

    it('holds the bar where the pointer has it while the selection changes mid-drag', () => {
        attachFollower(menu, editor, { activeClass: 'is-visible', handleSelector: '#handle' });
        select();

        const handle = document.querySelector('#handle');
        const pointer = (type, target, clientX, clientY) => target.dispatchEvent(pointerEvent(type, { clientX, clientY }));
        stubPointerCapture(menu);

        pointer('pointerdown', handle, 50, 140);
        pointer('pointermove', menu, 50, 180);
        selectText(true, false);
        document.dispatchEvent(new Event('selectionchange'));
        frames.flush();

        expect(menu.classList.contains('is-visible')).toBe(true);
        expect(menu.style.top).toBe('168px');

        pointer('pointerup', menu, 50, 180);
        select();

        expect(menu.style.top).toBe('168px');
    });

    it('keeps the offset a hidden bar was nudged by for its next showing', () => {
        attachFollower(menu, editor, { activeClass: 'is-visible', handleSelector: '#handle' });
        select();
        const shown = menu.getBoundingClientRect;
        selectText(true, false);
        document.dispatchEvent(new Event('selectionchange'));
        frames.flush();
        menu.getBoundingClientRect = () => ({ top: 0, bottom: 0, left: 0, right: 0, width: 0, height: 0, x: 0, y: 0 });

        const handle = document.querySelector('#handle');
        handle.focus();
        handle.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true, cancelable: true }));

        menu.getBoundingClientRect = shown;
        select();

        expect(menu.style.top).toBe('136px'); // 128 + 8
    });

    it('nudges the bar with the arrow keys on the handle', () => {
        attachFollower(menu, editor, { activeClass: 'is-visible', handleSelector: '#handle' });
        select();

        const handle = document.querySelector('#handle');
        handle.focus();
        const event = new KeyboardEvent('keydown', { key: 'ArrowUp', bubbles: true, cancelable: true });
        handle.dispatchEvent(event);

        expect(event.defaultPrevented).toBe(true);
        expect(menu.style.top).toBe('120px'); // 128 - 8
    });

    it('leaves the arrow keys alone away from the handle', () => {
        attachFollower(menu, editor, { activeClass: 'is-visible', handleSelector: '#handle' });
        select();

        const bold = document.querySelector('#bold');
        bold.focus();
        const event = new KeyboardEvent('keydown', { key: 'ArrowUp', bubbles: true, cancelable: true });
        bold.dispatchEvent(event);

        expect(event.defaultPrevented).toBe(false);
        expect(menu.style.top).toBe('128px');
    });

    it('follows the selection when its owner\'s box changes without any event', () => {
        attachFollower(menu, editor, { activeClass: 'is-visible' });
        const range = select();
        expect(menu.style.top).toBe('128px');

        range.getBoundingClientRect = () => ({
            top: 300, bottom: 320, left: 200, right: 280, width: 80, height: 20, x: 200, y: 300,
        });
        resizes.latest().deliver([]);
        frames.flush();

        expect(menu.style.top).toBe('328px'); // 320 (bottom) + 8 (gap)
    });

    it('never shows when the promised owner did not arrive', () => {
        attachFollower(menu, null, { activeClass: 'is-visible', requiresOwner: true });
        select();

        expect(menu.classList.contains('is-visible')).toBe(false);
    });

    it('hides once its owner leaves the document', () => {
        attachFollower(menu, editor, { activeClass: 'is-visible' });
        const range = select();
        expect(menu.classList.contains('is-visible')).toBe(true);

        const text = document.querySelector('#text');
        const getSelection = window.getSelection;
        editor.remove();
        // The selection object still answers with the detached range.
        Object.defineProperty(window, 'getSelection', { value: () => ({ rangeCount: 1, isCollapsed: false, anchorNode: text.firstChild, getRangeAt: () => range }), configurable: true });

        document.dispatchEvent(new Event('selectionchange'));
        frames.flush();

        Object.defineProperty(window, 'getSelection', { value: getSelection, configurable: true });
        expect(menu.classList.contains('is-visible')).toBe(false);
    });

    it('hides and removes listeners once disposed', () => {
        const handle = attachFollower(menu, editor, { activeClass: 'is-visible' });
        select();
        expect(menu.classList.contains('is-visible')).toBe(true);

        handle.dispose();

        expect(menu.classList.contains('is-visible')).toBe(false);

        document.dispatchEvent(new Event('selectionchange'));
        frames.flush();
        expect(menu.classList.contains('is-visible')).toBe(false);
    });
});

describe('SelectionFollower holding focus', () => {
    beforeEach(() => {
        stubResizeObserver();
        document.body.innerHTML = '<div id="editor" contenteditable="true" tabindex="0"><p>Selected content</p></div><div id="bar"><button id="first">First</button><button id="last">Last</button></div><button id="after">After</button>';
    });

    function key(name, shiftKey = false) {
        const event = new KeyboardEvent('keydown', { key: name, shiftKey, bubbles: true, cancelable: true });
        document.activeElement.dispatchEvent(event);

        return event;
    }

    it('wraps Tab from the last control back to the first', () => {
        attachFollower(document.querySelector('#bar'), document.querySelector('#editor'));
        document.querySelector('#last').focus();

        const event = key('Tab');

        expect(document.activeElement.id).toBe('first');
        expect(event.defaultPrevented).toBe(true);
    });

    it('hands focus back to the editor on Escape', () => {
        attachFollower(document.querySelector('#bar'), document.querySelector('#editor'));
        document.querySelector('#first').focus();

        const event = key('Escape');

        expect(document.activeElement.id).toBe('editor');
        expect(event.defaultPrevented).toBe(true);
    });

    it('stands on the router only while it has focus', () => {
        const bar = document.querySelector('#bar');
        attachFollower(bar, document.querySelector('#editor'));

        document.querySelector('#first').focus();
        const focused = stack().map(entry => entry.element);
        document.querySelector('#after').focus();

        expect(focused).toEqual([bar]);
        expect(stack()).toEqual([]);
    });

    it('leaves Escape alone once focus has left it', () => {
        attachFollower(document.querySelector('#bar'), document.querySelector('#editor'));
        document.querySelector('#first').focus();
        document.querySelector('#after').focus();

        const event = key('Escape');

        expect(document.activeElement.id).toBe('after');
        expect(event.defaultPrevented).toBe(false);
    });

    it('leaves Escape alone without an owner to return focus to', () => {
        attachFollower(document.querySelector('#bar'), null);
        document.querySelector('#first').focus();

        const event = key('Escape');

        expect(document.activeElement.id).toBe('first');
        expect(event.defaultPrevented).toBe(false);
        expect(stack()).toEqual([]);
    });

    // The drag activated after the bar stands above it, so one Escape cancels the drag and the next returns focus.
    it('yields Escape to a drag of its handle in flight, and takes the next', () => {
        const bar = document.querySelector('#bar');
        bar.insertAdjacentHTML('afterbegin', '<button id="handle">Move</button>');
        stubPointerCapture(bar);
        attachFollower(bar, document.querySelector('#editor'), { handleSelector: '#handle' });
        const handle = document.querySelector('#handle');
        handle.focus();
        handle.dispatchEvent(pointerEvent('pointerdown', { clientX: 50, clientY: 140 }));
        bar.dispatchEvent(pointerEvent('pointermove', { clientX: 50, clientY: 180 }));

        const first = key('Escape');
        const afterFirst = document.activeElement.id;
        const second = key('Escape');

        expect([first.defaultPrevented, afterFirst]).toEqual([true, 'handle']);
        expect([second.defaultPrevented, document.activeElement.id]).toEqual([true, 'editor']);
    });
});

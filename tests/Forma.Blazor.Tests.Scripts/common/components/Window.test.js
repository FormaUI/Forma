import { describe, it, expect, beforeEach, vi } from 'vitest';
import { attach } from '#common/components/Window.js';
import { pointerEvent, stubPointerCapture } from '#support/platform.js';

describe('Window', () => {
    /** @type {HTMLElement} */
    let box;
    /** @type {{ invokeMethodAsync: import('vitest').Mock }} */
    let owner;

    const options = (overrides = {}) => ({ owner, titleBarClass: 'titlebar', handleClass: 'handle', stepPixels: 16, floating: true, presented: true, ...overrides });

    beforeEach(() => {
        document.body.innerHTML = `
            <div id="box" style="left: 100px; top: 80px; width: 400px; height: 300px; min-width: 200px; min-height: 120px;">
                <div class="titlebar"><span>Folders</span><button id="close">Close</button></div>
                <div class="handle" data-edge="se"></div>
                <div class="handle" data-edge="w"></div>
                <div class="handle" data-edge="n"></div>
            </div>`;
        box = document.querySelector('#box');
        stubPointerCapture(box);
        owner = { invokeMethodAsync: vi.fn().mockResolvedValue(undefined) };

        // A headless DOM lays nothing out, so the box's rectangle is read back from the styles the window writes.
        box.getBoundingClientRect = () => {
            const value = name => parseFloat(box.style[name]);
            const [left, top, width, height] = [value('left'), value('top'), value('width'), value('height')];

            return { left, top, width, height, right: left + width, bottom: top + height };
        };
        Object.defineProperty(window, 'innerWidth', { value: 1000, configurable: true });
        Object.defineProperty(window, 'innerHeight', { value: 800, configurable: true });
    });

    const point = (type, on, properties) => document.querySelector(on).dispatchEvent(pointerEvent(type, properties));
    const drag = (on, from, to) => {
        point('pointerdown', on, from);
        point('pointermove', on, to);
        point('pointerup', on, to);
    };
    const reported = () => owner.invokeMethodAsync.mock.calls.map(([method, ...args]) => ({ method, args }));

    it('moves the window by its title bar and reports where it came to rest', () => {
        attach(box, options());

        drag('.titlebar span', { clientX: 200, clientY: 90 }, { clientX: 230, clientY: 110 });

        expect(box.style.left).toBe('130px');
        expect(box.style.top).toBe('100px');
        expect(reported()).toEqual([{ method: 'ReportPlacementAsync', args: [130, 100, 400, 300] }]);
    });

    it('holds a moved window within the viewport', () => {
        attach(box, options());

        drag('.titlebar span', { clientX: 200, clientY: 90 }, { clientX: 2000, clientY: -900 });

        expect(box.style.left).toBe('600px');
        expect(box.style.top).toBe('0px');
    });

    it('grows the window from a corner handle', () => {
        attach(box, options());

        drag('.handle[data-edge=se]', { clientX: 500, clientY: 380 }, { clientX: 560, clientY: 420 });

        expect(box.style.width).toBe('460px');
        expect(box.style.height).toBe('340px');
        expect(box.style.left).toBe('100px');
    });

    it('pulls a start edge, keeping the far edge in place', () => {
        attach(box, options());

        drag('.handle[data-edge=w]', { clientX: 100, clientY: 200 }, { clientX: 140, clientY: 200 });

        expect(box.style.left).toBe('140px');
        expect(box.style.width).toBe('360px');
    });

    it('never resizes below the minimum the stylesheet sets', () => {
        attach(box, options());

        drag('.handle[data-edge=n]', { clientX: 300, clientY: 80 }, { clientX: 300, clientY: 900 });

        expect(box.style.height).toBe('120px');
        expect(box.style.top).toBe('260px');
    });

    it('holds a pulled start edge at the viewport edge', () => {
        attach(box, options());

        drag('.handle[data-edge=w]', { clientX: 100, clientY: 200 }, { clientX: -400, clientY: 200 });

        expect(box.style.left).toBe('0px');
        expect(box.style.width).toBe('500px');
    });

    it('holds a pulled far edge at the viewport edge', () => {
        attach(box, options());

        drag('.handle[data-edge=se]', { clientX: 500, clientY: 380 }, { clientX: 1500, clientY: 1500 });

        expect(box.style.width).toBe('900px');
        expect(box.style.height).toBe('720px');
    });

    it('never resizes past the maximum the styles set', () => {
        box.style.maxWidth = '500px';
        box.style.maxHeight = '350px';
        attach(box, options());

        drag('.handle[data-edge=se]', { clientX: 500, clientY: 380 }, { clientX: 900, clientY: 900 });

        expect(box.style.width).toBe('500px');
        expect(box.style.height).toBe('350px');
    });

    it('shrinks a remembered box that exceeds its maximum on attach', () => {
        box.style.maxWidth = '300px';

        attach(box, options());

        expect(box.style.width).toBe('300px');
        expect(reported()).toEqual([{ method: 'ReportPlacementAsync', args: [100, 80, 300, 300] }]);
    });

    it('marks the window while a drag is in progress', () => {
        attach(box, options());

        point('pointerdown', '.titlebar span', { clientX: 200, clientY: 90 });
        point('pointermove', '.titlebar span', { clientX: 210, clientY: 90 });
        expect(box.classList.contains('is-dragging')).toBe(true);

        point('pointerup', '.titlebar span', { clientX: 210, clientY: 90 });
        expect(box.classList.contains('is-dragging')).toBe(false);
    });

    it('ignores a press on a control inside the title bar', () => {
        attach(box, options());

        drag('#close', { clientX: 200, clientY: 90 }, { clientX: 260, clientY: 90 });

        expect(box.style.left).toBe('100px');
        expect(reported()).toEqual([]);
    });

    it('neither moves nor resizes while maximized', () => {
        box.classList.add('is-maximized');
        attach(box, options());

        drag('.titlebar span', { clientX: 200, clientY: 90 }, { clientX: 260, clientY: 90 });
        drag('.handle[data-edge=se]', { clientX: 500, clientY: 380 }, { clientX: 560, clientY: 420 });

        expect(box.style.left).toBe('100px');
        expect(box.style.width).toBe('400px');
    });

    it('reports where the window stands when measured', () => {
        const handle = attach(box, options());

        handle.measure();

        expect(reported()).toEqual([{ method: 'ReportPlacementAsync', args: [100, 80, 400, 300] }]);
    });

    it('steps the window with an arrow on the focused title bar, and resizes it with Shift, without scrolling the page', () => {
        attach(box, options());
        const titleBar = document.querySelector('.titlebar');
        const press = (key, shiftKey = false) => {
            const event = new KeyboardEvent('keydown', { key, shiftKey, bubbles: true, cancelable: true });
            titleBar.dispatchEvent(event);
            return event.defaultPrevented;
        };

        expect(press('ArrowRight')).toBe(true);
        expect(press('ArrowDown')).toBe(true);
        expect(press('ArrowRight', true)).toBe(true);

        expect(box.style.left).toBe('116px');
        expect(box.style.top).toBe('96px');
        expect(box.style.width).toBe('416px');
        expect(reported()).toHaveLength(3);
    });

    it('leaves other keys, and arrows pressed inside the title bar, to their own handlers', () => {
        attach(box, options());
        const press = (on, key) => {
            const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true });
            document.querySelector(on).dispatchEvent(event);
            return event.defaultPrevented;
        };

        expect(press('.titlebar', 'Tab')).toBe(false);
        expect(press('#close', 'ArrowRight')).toBe(false);
        expect(box.style.left).toBe('100px');
    });

    it('does not resize from the keyboard while it has no handles', () => {
        document.querySelectorAll('.handle').forEach(handle => handle.remove());
        attach(box, options());

        const event = new KeyboardEvent('keydown', { key: 'ArrowRight', shiftKey: true, bubbles: true, cancelable: true });
        document.querySelector('.titlebar').dispatchEvent(event);

        expect(event.defaultPrevented).toBe(true);
        expect(box.style.width).toBe('400px');
    });

    it('nudges the window by a step and reports it', () => {
        const handle = attach(box, options());

        handle.nudge(16, 0, false);
        handle.nudge(0, 16, true);

        expect(box.style.left).toBe('116px');
        expect(box.style.height).toBe('316px');
        expect(reported()).toHaveLength(2);
    });

    it('raises itself into the top layer where the platform has one, and leaves it when disposed', () => {
        let open = false;
        box.showPopover = vi.fn(() => { open = true; });
        box.hidePopover = vi.fn(() => { open = false; });
        box.matches = selector => selector === ':popover-open' && open;

        const handle = attach(box, options());
        expect(box.showPopover).toHaveBeenCalledOnce();

        handle.dispose();
        expect(box.hidePopover).toHaveBeenCalledOnce();
    });

    it('fits a remembered box into the viewport on attach and reports the change', () => {
        box.style.left = '800px';
        box.style.width = '600px';

        attach(box, options());

        expect(box.style.left).toBe('400px');
        expect(box.style.width).toBe('600px');
        expect(reported()).toEqual([{ method: 'ReportPlacementAsync', args: [400, 80, 600, 300] }]);
    });

    it('leaves a box that already fits alone on attach', () => {
        attach(box, options());

        expect(reported()).toEqual([]);
    });

    it('keeps the window within a viewport that shrinks', () => {
        attach(box, options());

        Object.defineProperty(window, 'innerWidth', { value: 450, configurable: true });
        Object.defineProperty(window, 'innerHeight', { value: 250, configurable: true });
        window.dispatchEvent(new Event('resize'));

        expect(box.style.left).toBe('50px');
        expect(box.style.top).toBe('0px');
        expect(box.style.height).toBe('250px');
    });

    it('neither shows nor moves a docked window', () => {
        box.showPopover = vi.fn();
        box.matches = () => false;
        attach(box, options({ floating: false }));

        drag('.titlebar span', { clientX: 200, clientY: 90 }, { clientX: 260, clientY: 90 });

        expect(box.showPopover).not.toHaveBeenCalled();
        expect(box.style.left).toBe('100px');
        expect(reported()).toEqual([]);
    });

    it('stays out of the top layer while not presented, and enters it when presented', () => {
        let open = false;
        box.showPopover = vi.fn(() => { open = true; });
        box.hidePopover = vi.fn(() => { open = false; });
        box.matches = selector => selector === ':popover-open' && open;

        const handle = attach(box, options({ presented: false }));
        expect(box.showPopover).not.toHaveBeenCalled();

        handle.present(true);
        expect(open).toBe(true);

        handle.present(false);
        expect(open).toBe(false);
    });

    const popoverOf = element => {
        let open = false;
        element.showPopover = vi.fn(() => { open = true; });
        element.hidePopover = vi.fn(() => { open = false; });
        element.matches = selector => selector === ':popover-open' && open;
        return () => open;
    };

    it('raises a presented window that another was shown after by showing it again on top', () => {
        const isOpen = popoverOf(box);
        const handle = attach(box, options());
        document.body.insertAdjacentHTML('beforeend', '<div id="other"></div>');
        const other = document.querySelector('#other');
        popoverOf(other);
        stubPointerCapture(other);
        attach(other, options());

        handle.raise();

        expect(box.hidePopover).toHaveBeenCalledOnce();
        expect(box.showPopover).toHaveBeenCalledTimes(2);
        expect(isOpen()).toBe(true);
    });

    it('leaves the window shown last alone when raised', () => {
        popoverOf(box);
        const handle = attach(box, options());

        handle.raise();

        expect(box.hidePopover).not.toHaveBeenCalled();
        expect(box.showPopover).toHaveBeenCalledOnce();
    });

    it('waits for a drag to end before raising', () => {
        popoverOf(box);
        const handle = attach(box, options());
        document.body.insertAdjacentHTML('beforeend', '<div id="other"></div>');
        const other = document.querySelector('#other');
        popoverOf(other);
        stubPointerCapture(other);
        attach(other, options());

        point('pointerdown', '.titlebar span', { clientX: 200, clientY: 90 });
        point('pointermove', '.titlebar span', { clientX: 220, clientY: 90 });
        handle.raise();
        const hiddenDuringDrag = box.hidePopover.mock.calls.length;
        point('pointerup', '.titlebar span', { clientX: 220, clientY: 90 });

        expect(hiddenDuringDrag).toBe(0);
        expect(box.hidePopover).toHaveBeenCalledOnce();
        expect(box.showPopover).toHaveBeenCalledTimes(2);
    });

    it('takes focus back to where it last was inside the window', () => {
        document.body.insertAdjacentHTML('beforeend', '<button id="outside">Outside</button>');
        box.querySelector('.titlebar').insertAdjacentHTML('beforeend', '<input id="field">');
        const handle = attach(box, options());
        const field = document.querySelector('#field');

        field.focus();
        document.querySelector('#outside').focus();
        handle.focus();

        expect(document.activeElement).toBe(field);
    });

    it('focuses the first control when nothing inside was focused before', () => {
        document.body.insertAdjacentHTML('beforeend', '<button id="outside">Outside</button>');
        const handle = attach(box, options());
        document.querySelector('#outside').focus();

        handle.focus();

        expect(document.activeElement).toBe(document.querySelector('#close'));
    });

    it('moves focus from the window itself to the control last focused inside it', () => {
        box.setAttribute('tabindex', '-1');
        box.querySelector('.titlebar').insertAdjacentHTML('beforeend', '<input id="field">');
        const handle = attach(box, options());
        document.querySelector('#field').focus();
        box.focus();

        handle.focus();

        expect(document.activeElement).toBe(document.querySelector('#field'));
    });

    it('leaves focus alone when it is already inside the window', () => {
        box.querySelector('.titlebar').insertAdjacentHTML('beforeend', '<input id="field">');
        const handle = attach(box, options());
        document.querySelector('#field').focus();

        handle.focus();

        expect(document.activeElement).toBe(document.querySelector('#field'));
    });

    it('stops listening once disposed', () => {
        const handle = attach(box, options());
        handle.dispose();

        drag('.titlebar span', { clientX: 200, clientY: 90 }, { clientX: 260, clientY: 90 });
        Object.defineProperty(window, 'innerWidth', { value: 450, configurable: true });
        window.dispatchEvent(new Event('resize'));

        expect(box.style.left).toBe('100px');
        expect(reported()).toEqual([]);
    });
});

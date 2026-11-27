import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { install, hide, findTooltip } from '#browser/components/Tooltip.js';
import { configure } from '#input/InputRouter.js';
import { pointerEvent, stubAnimationFrames, stubResizeObserver } from '#support/platform.js';

describe('Tooltip', () => {
    /** @type {ReturnType<typeof stubResizeObserver>} */
    let resizes;
    /** @type {ReturnType<typeof stubAnimationFrames>} */
    let frames;
    /** @type {HTMLElement} */
    let control;
    /** @type {HTMLElement} */
    let tooltip;

    // The popover the headless DOM lacks, its open state kept on the element as the browser keeps it.
    const stubPopover = element => {
        let isOpen = false;
        const matches = element.matches.bind(element);
        element.showPopover = vi.fn(() => { isOpen = true; });
        element.hidePopover = vi.fn(() => { isOpen = false; });
        element.matches = selector => selector === ':popover-open' ? isOpen : matches(selector);

        return () => isOpen;
    };

    let isShown;

    const point = (type, target, { pointerType = 'mouse', relatedTarget = null } = {}) =>
        target.dispatchEvent(Object.assign(pointerEvent(type, { pointerType }), { relatedTarget }));

    beforeEach(() => {
        vi.useFakeTimers();
        resizes = stubResizeObserver();
        frames = stubAnimationFrames();
        document.body.innerHTML = `
            <button id="save" aria-describedby="hint save-tip">Save</button>
            <p id="hint">Saves the draft.</p>
            <span id="save-tip" class="forma-tooltip" role="tooltip" popover="manual">Saved drafts are kept for a month</span>
            <button id="elsewhere">Elsewhere</button>`;
        control = document.querySelector('#save');
        tooltip = document.querySelector('#save-tip');
        isShown = stubPopover(tooltip);
        install();
    });

    afterEach(() => {
        hide();
        configure({ bindings: {} });
        resizes.restore();
        frames.restore();
        vi.useRealTimers();
    });

    it('finds the tooltip a control names among its descriptions', () => {
        const found = findTooltip(control);

        expect(found?.control).toBe(control);
        expect(found?.tooltip).toBe(tooltip);
    });

    it('finds no tooltip for a control describing itself by other text alone', () => {
        control.setAttribute('aria-describedby', 'hint');

        expect(findTooltip(control)).toBeNull();
    });

    it('shows the tooltip once a pointer has rested on its control', () => {
        point('pointerover', control);

        vi.advanceTimersByTime(499);
        expect(isShown()).toBe(false);
        vi.advanceTimersByTime(1);

        expect(isShown()).toBe(true);
    });

    it('keeps the tooltip while the pointer moves from the control onto it', () => {
        point('pointerover', control);
        vi.advanceTimersByTime(500);

        point('pointerout', control, { relatedTarget: tooltip });

        expect(isShown()).toBe(true);
    });

    it('hides the tooltip once the pointer leaves the control and the tooltip', () => {
        point('pointerover', control);
        vi.advanceTimersByTime(500);

        point('pointerout', control, { relatedTarget: document.querySelector('#elsewhere') });

        expect(isShown()).toBe(false);
    });

    it('shows nothing for a pointer that leaves before the delay passes', () => {
        point('pointerover', control);
        point('pointerout', control, { relatedTarget: document.querySelector('#elsewhere') });

        vi.advanceTimersByTime(500);

        expect(isShown()).toBe(false);
    });

    it('shows the tooltip at once on a tap on its control', () => {
        point('pointerdown', control, { pointerType: 'touch' });

        expect(isShown()).toBe(true);
    });

    it('hides the tooltip on Escape', () => {
        point('pointerdown', control, { pointerType: 'touch' });

        document.activeElement.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true }));

        expect(isShown()).toBe(false);
    });

    it('hides the tooltip on a tap elsewhere', () => {
        point('pointerdown', control, { pointerType: 'touch' });

        point('pointerdown', document.querySelector('#elsewhere'), { pointerType: 'touch' });

        expect(isShown()).toBe(false);
    });

    it('hides the tooltip once a render takes its control away', async () => {
        point('pointerdown', control, { pointerType: 'touch' });

        control.remove();
        await Promise.resolve();

        expect(tooltip.hidePopover).toHaveBeenCalled();
    });

    it('keeps a press on a control disabled with its reason from acting', () => {
        control.setAttribute('aria-disabled', 'true');
        const click = new MouseEvent('click', { bubbles: true, cancelable: true });

        control.dispatchEvent(click);

        expect(click.defaultPrevented).toBe(true);
    });

    it('leaves a press on an enabled control to act', () => {
        const click = new MouseEvent('click', { bubbles: true, cancelable: true });

        control.dispatchEvent(click);

        expect(click.defaultPrevented).toBe(false);
    });

    it('keeps the keys and a press from moving a range disabled with its reason, Tab still moving on', () => {
        document.body.insertAdjacentHTML('beforeend', '<input id="volume" type="range" aria-disabled="true" aria-describedby="save-tip" />');
        const range = document.querySelector('#volume');
        const key = name => {
            const event = new KeyboardEvent('keydown', { key: name, bubbles: true, cancelable: true });
            range.dispatchEvent(event);
            return event.defaultPrevented;
        };
        const press = pointerEvent('pointerdown');

        range.dispatchEvent(press);

        expect([key('ArrowRight'), key('End'), key('Tab')]).toEqual([true, true, false]);
        expect(press.defaultPrevented).toBe(true);
    });

    it('places the tooltip above its control', () => {
        control.getBoundingClientRect = () => ({ top: 300, bottom: 330, left: 100, right: 180, width: 80, height: 30 });
        Object.defineProperty(tooltip, 'offsetWidth', { value: 120, configurable: true });
        Object.defineProperty(tooltip, 'offsetHeight', { value: 24, configurable: true });
        globalThis.innerWidth = 1024;
        globalThis.innerHeight = 768;

        point('pointerdown', control, { pointerType: 'touch' });
        frames.flush();

        expect(tooltip.style.top).toBe('272px');
        expect(tooltip.style.left).toBe('80px');
    });

    // The last two, since the script each loads afresh keeps its listeners on the document for any test after it.
    it('shows the tooltip of a control the keyboard reached before the script arrived', async () => {
        control.focus();
        vi.resetModules();
        const arriving = await import('#browser/components/Tooltip.js');

        arriving.install();

        expect(control.matches(':focus-visible')).toBe(true);
        expect(isShown()).toBe(true);
        arriving.hide();
    });

    it('shows the tooltip of a control the pointer rests on as the script arrives, once the delay passes', async () => {
        const matches = control.matches.bind(control);
        control.matches = selector => selector === ':hover' || matches(selector);
        vi.resetModules();
        const arriving = await import('#browser/components/Tooltip.js');

        arriving.install();

        vi.advanceTimersByTime(499);
        expect(isShown()).toBe(false);
        vi.advanceTimersByTime(1);
        expect(isShown()).toBe(true);
        arriving.hide();
    });
});

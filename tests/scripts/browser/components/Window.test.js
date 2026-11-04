import { describe, it, expect, beforeEach, vi } from 'vitest';
import { attach } from '#browser/components/Window.js';
import { pointerEvent } from '#support/platform.js';

describe('Window', () => {
    /** @type {HTMLElement} */
    let box;

    const options = (overrides = {}) => ({ presented: true, modal: false, scrim: null, ...overrides });

    const popoverOf = element => {
        let open = false;
        element.showPopover = vi.fn(() => { open = true; });
        element.hidePopover = vi.fn(() => { open = false; });
        element.matches = selector => selector === ':popover-open' && open;
        return () => open;
    };

    beforeEach(() => {
        document.body.innerHTML = `
            <div id="box">
                <div class="titlebar"><span>Folders</span><button id="close">Close</button></div>
            </div>`;
        box = document.querySelector('#box');
    });

    it('raises itself into the top layer where the platform has one, and leaves it when disposed', () => {
        popoverOf(box);

        const handle = attach(box, options());
        expect(box.showPopover).toHaveBeenCalledOnce();

        handle.dispose();
        expect(box.hidePopover).toHaveBeenCalledOnce();
    });

    it('stays out of the top layer while not presented, and enters it when presented', () => {
        const isOpen = popoverOf(box);

        const handle = attach(box, options({ presented: false }));
        expect(box.showPopover).not.toHaveBeenCalled();

        handle.present(true);
        expect(isOpen()).toBe(true);

        handle.present(false);
        expect(isOpen()).toBe(false);
    });

    it('raises a presented window that another was shown after by showing it again on top', () => {
        const isOpen = popoverOf(box);
        const handle = attach(box, options());
        document.body.insertAdjacentHTML('beforeend', '<div id="other"></div>');
        const other = document.querySelector('#other');
        popoverOf(other);
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

    it('waits for a press inside it to end before raising', () => {
        popoverOf(box);
        const handle = attach(box, options());
        document.body.insertAdjacentHTML('beforeend', '<div id="other"></div>');
        const other = document.querySelector('#other');
        popoverOf(other);
        attach(other, options());

        document.querySelector('.titlebar span').dispatchEvent(pointerEvent('pointerdown', { clientX: 200, clientY: 90 }));
        handle.raise();
        const hiddenDuringPress = box.hidePopover.mock.calls.length;
        window.dispatchEvent(pointerEvent('pointerup', { clientX: 220, clientY: 90 }));

        expect(hiddenDuringPress).toBe(0);
        expect(box.hidePopover).toHaveBeenCalledOnce();
        expect(box.showPopover).toHaveBeenCalledTimes(2);
    });

    it('tells its owner of each press inside it, leaving the press to what it lands on', () => {
        const owner = { invokeMethodAsync: vi.fn().mockResolvedValue(undefined) };
        attach(box, options({ owner }));

        const press = pointerEvent('pointerdown', { clientX: 200, clientY: 90 });
        document.querySelector('.titlebar span').dispatchEvent(press);

        expect(owner.invokeMethodAsync).toHaveBeenCalledExactlyOnceWith('HandlePressAsync');
        expect(press.defaultPrevented).toBe(false);
    });

    it('tells its owner of no press once disposed', () => {
        const owner = { invokeMethodAsync: vi.fn().mockResolvedValue(undefined) };
        attach(box, options({ owner })).dispose();

        document.querySelector('.titlebar span').dispatchEvent(pointerEvent('pointerdown', { clientX: 200, clientY: 90 }));

        expect(owner.invokeMethodAsync).not.toHaveBeenCalled();
    });

    it('treats the browser window losing focus as the end of a press, and raises then', () => {
        popoverOf(box);
        const handle = attach(box, options());
        document.body.insertAdjacentHTML('beforeend', '<div id="other"></div>');
        const other = document.querySelector('#other');
        popoverOf(other);
        attach(other, options());

        document.querySelector('.titlebar span').dispatchEvent(pointerEvent('pointerdown', { clientX: 200, clientY: 90 }));
        handle.raise();
        const hiddenDuringPress = box.hidePopover.mock.calls.length;
        window.dispatchEvent(new Event('blur'));

        expect(hiddenDuringPress).toBe(0);
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

    it('shows its scrim beneath it and hides both together', () => {
        const shown = [];
        const tracked = (element, name) => {
            let open = false;
            element.showPopover = vi.fn(() => { open = true; shown.push(name); });
            element.hidePopover = vi.fn(() => { open = false; });
            element.matches = selector => selector === ':popover-open' && open;
        };
        tracked(box, 'window');
        document.body.insertAdjacentHTML('beforeend', '<div id="scrim"></div>');
        const scrim = document.querySelector('#scrim');
        tracked(scrim, 'scrim');

        const handle = attach(box, options({ modal: true, scrim }));
        handle.present(false);

        expect(shown).toEqual(['scrim', 'window']);
        expect(scrim.hidePopover).toHaveBeenCalledOnce();
        expect(box.hidePopover).toHaveBeenCalledOnce();
    });

    it('keeps Tab inside a modal window', () => {
        box.querySelector('.titlebar').insertAdjacentHTML('beforeend', '<button id="last">Last</button>');
        document.body.insertAdjacentHTML('beforeend', '<button id="after">After</button>');
        attach(box, options({ modal: true }));
        document.querySelector('#last').focus();

        const event = new KeyboardEvent('keydown', { key: 'Tab', bubbles: true, cancelable: true });
        document.activeElement.dispatchEvent(event);

        expect(event.defaultPrevented).toBe(true);
        expect(box.contains(document.activeElement)).toBe(true);
    });
});

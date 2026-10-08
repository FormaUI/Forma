import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { ScrollButtons } from '#browser/scrolling/ScrollButtons.mjs';
import { configure, describe as describeScope, resolve } from '#input/InputRouter.js';
import { stubMutationObserver } from '#support/platform.js';

describe('ScrollButtons', () => {
    /** @type {ReturnType<typeof stubMutationObserver>} */
    let mutations;
    /** @type {HTMLElement} */
    let pane;
    /** @type {ReturnType<typeof vi.fn>} */
    let step;
    /** @type {ScrollButtons[]} */
    let handles;

    const options = { back: '.back', forward: '.forward', backAction: 'pane.scroll-back', forwardAction: 'pane.scroll-forward' };

    beforeEach(() => {
        mutations = stubMutationObserver();
        document.body.innerHTML = '<div id="pane"><button class="back"><svg></svg></button><div class="viewport"><button>Inside</button></div><button class="forward">&gt;</button></div>';
        pane = document.querySelector('#pane');
        step = vi.fn();
        handles = [];
    });

    afterEach(() => {
        handles.splice(0).forEach(handle => handle.dispose());
        configure({ bindings: {} });
        mutations.restore();
    });

    const attachButtons = () => {
        const handle = new ScrollButtons(pane, options, step);
        handles.push(handle);

        return handle;
    };

    // A click from a pointer carries how many presses it followed; one a key raises carries none.
    const click = (target, { detail = 1, shiftKey = false } = {}) => {
        const event = new MouseEvent('click', { bubbles: true, cancelable: true, detail, shiftKey });
        target.dispatchEvent(event);

        return event;
    };

    it('steps back for a click on the back button and forward for one on the forward button', () => {
        attachButtons();

        click(pane.querySelector('.back'));
        click(pane.querySelector('.forward'));

        expect(step.mock.calls).toEqual([[-1], [1]]);
    });

    it('routes a click on what the button holds as the button\'s', () => {
        attachButtons();

        const event = click(pane.querySelector('.back svg'));

        expect(resolve(event)).toMatchObject({ owner: pane.querySelector('.back'), action: 'pane.scroll-back' });
        expect(step).toHaveBeenCalledExactlyOnceWith(-1);
    });

    it('takes one step for each press', () => {
        attachButtons();

        click(pane.querySelector('.forward'));

        expect(step).toHaveBeenCalledTimes(1);
    });

    // Enter and Space are the native button's own keys, which press it with a click carrying no press count.
    it('leaves Enter to the button, stepping once for the click it raises', () => {
        attachButtons();
        const forward = pane.querySelector('.forward');

        const key = new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true });
        forward.dispatchEvent(key);
        click(forward, { detail: 0 });

        expect(key.defaultPrevented).toBe(false);
        expect(step).toHaveBeenCalledExactlyOnceWith(1);
    });

    it('leaves Space to the button, stepping once for the click it raises', () => {
        attachButtons();
        const back = pane.querySelector('.back');

        const key = new KeyboardEvent('keydown', { key: ' ', bubbles: true, cancelable: true });
        back.dispatchEvent(key);
        click(back, { detail: 0 });

        expect(key.defaultPrevented).toBe(false);
        expect(step).toHaveBeenCalledExactlyOnceWith(-1);
    });

    it('steps nothing for a disabled button', () => {
        attachButtons();
        const back = pane.querySelector('.back');
        back.disabled = true;

        click(back);

        expect(step).not.toHaveBeenCalled();
    });

    it('takes the host\'s remap of a button\'s action', () => {
        configure({ bindings: { 'pane.scroll-forward': [{ type: 'click', modifiers: { shift: true } }] } });
        attachButtons();
        const forward = pane.querySelector('.forward');

        click(forward);
        expect(step).not.toHaveBeenCalled();

        click(forward, { shiftKey: true });
        expect(step).toHaveBeenCalledExactlyOnceWith(1);
    });

    it('leaves a click inside the viewport alone', () => {
        attachButtons();

        click(pane.querySelector('.viewport button'));

        expect(step).not.toHaveBeenCalled();
    });

    it('routes a button that joins the container later', () => {
        pane.querySelector('.forward').remove();
        attachButtons();

        const forward = document.createElement('button');
        forward.className = 'forward';
        pane.appendChild(forward);
        mutations.latest().deliver([]);
        click(forward);

        expect(step).toHaveBeenCalledExactlyOnceWith(1);
    });

    it('releases a button that leaves the container', () => {
        attachButtons();
        const back = pane.querySelector('.back');

        back.remove();
        mutations.latest().deliver([]);

        expect(describeScope(back)).toBeNull();
    });

    it('releases every button and stops watching once disposed', () => {
        const handle = attachButtons();

        handle.dispose();

        expect(describeScope(pane.querySelector('.back'))).toBeNull();
        expect(describeScope(pane.querySelector('.forward'))).toBeNull();
        expect(mutations.latest().isConnected).toBe(false);
    });
});

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { FocusStateObserver } from '#browser/focus/FocusStateObserver.mjs';

describe('FocusStateObserver', () => {
    /** @type {HTMLElement} */
    let surface;
    /** @type {ReturnType<typeof vi.fn>} */
    let publish;
    /** @type {FocusStateObserver[]} */
    let observers;

    beforeEach(() => {
        vi.useFakeTimers();
        document.body.innerHTML = '<div id="surface"><button id="first">First</button><button id="second">Second</button><span id="label">Label</span><div id="popup" popover="manual"><button id="inside-popup">Inside</button></div></div><button id="after">After</button><p id="page">Page</p>';
        surface = document.querySelector('#surface');
        publish = vi.fn();
        observers = [];
    });

    afterEach(() => {
        observers.forEach(observer => observer.dispose());
        vi.restoreAllMocks();
        vi.useRealTimers();
    });

    const focus = id => document.querySelector(`#${id}`).focus();

    /** A press on an element, as the browser raises it before the focus moves. */
    const press = id => document.querySelector(`#${id}`).dispatchEvent(new Event('pointerdown', { bubbles: true }));

    /** Observes the surface and takes its first state, as an attach does. */
    const watch = () => {
        const observer = new FocusStateObserver(surface, {}, publish);
        observers.push(observer);

        return { observer, first: observer.read() };
    };

    const published = () => publish.mock.calls.map(([state]) => state.isWithin);

    it('reads the reader within the surface while its focus is inside', () => {
        focus('first');

        expect(watch().first).toEqual({ isWithin: true });
    });

    it('reads the reader outside the surface without publishing a leave', () => {
        focus('after');

        expect(watch().first).toEqual({ isWithin: false });
        expect(publish).not.toHaveBeenCalled();
    });

    it('publishes the reader outside once the focus moves to an element outside the surface', () => {
        focus('first');
        watch();

        focus('after');

        expect(published()).toEqual([false]);
    });

    it('publishes nothing while the focus moves inside the surface', () => {
        focus('first');
        watch();

        focus('second');
        vi.runAllTimers();

        expect(publish).not.toHaveBeenCalled();
    });

    it('counts a popover rendered inside the surface as within', () => {
        focus('first');
        watch();

        focus('inside-popup');
        vi.runAllTimers();

        expect(publish).not.toHaveBeenCalled();
    });

    it('publishes the reader within again once the focus returns after leaving', () => {
        focus('first');
        watch();

        focus('after');
        focus('second');

        expect(published()).toEqual([false, true]);
    });

    it('publishes the reader outside once a focus that fell to the page has settled there', () => {
        focus('first');
        watch();

        document.querySelector('#first').blur();
        const before = published();
        vi.runAllTimers();

        expect(before).toEqual([]);
        expect(published()).toEqual([false]);
    });

    it('keeps the reader within when the focus goes with an element a render takes away', () => {
        focus('first');
        watch();

        const first = document.querySelector('#first');
        first.blur();
        first.remove();
        vi.runAllTimers();

        expect(publish).not.toHaveBeenCalled();
    });

    it('keeps the reader within when the focus leaves the window with the reader', () => {
        focus('first');
        watch();
        vi.spyOn(document, 'hasFocus').mockReturnValue(false);

        document.querySelector('#first').blur();
        vi.runAllTimers();

        expect(publish).not.toHaveBeenCalled();
    });

    it('keeps the reader within when a press inside the surface drops the focus to the page', () => {
        focus('first');
        watch();

        press('label');
        document.querySelector('#first').blur();
        vi.runAllTimers();

        expect(publish).not.toHaveBeenCalled();
    });

    it('publishes the reader outside once the reader presses outside after a press inside dropped the focus', () => {
        focus('first');
        watch();
        press('label');
        document.querySelector('#first').blur();
        vi.runAllTimers();

        press('page');

        expect(published()).toEqual([false]);
    });

    it('publishes the reader outside once the focus lands outside after a press inside dropped it', () => {
        focus('first');
        watch();
        press('label');
        document.querySelector('#first').blur();
        vi.runAllTimers();

        focus('after');

        expect(published()).toEqual([false]);
    });

    it('keeps the reader within when the focus comes back inside after a press inside dropped it', () => {
        focus('first');
        watch();
        press('label');
        document.querySelector('#first').blur();
        vi.runAllTimers();

        press('second');
        focus('second');
        press('page');

        expect(publish).not.toHaveBeenCalled();
    });

    it('publishes the reader outside once the reader presses outside after a refresh while a press inside held the focus', () => {
        focus('first');
        const { observer } = watch();
        press('label');
        document.querySelector('#first').blur();
        vi.runAllTimers();
        observer.refresh();

        press('page');

        expect(published()).toEqual([true, false]);
    });

    it('publishes the state it holds once refreshed, unchanged or not', () => {
        focus('first');
        const { observer } = watch();

        observer.refresh();

        expect(published()).toEqual([true]);
    });

    it('publishes nothing once disposed', () => {
        focus('first');
        const { observer } = watch();

        document.querySelector('#first').blur();
        observer.dispose();
        focus('after');
        vi.runAllTimers();

        expect(publish).not.toHaveBeenCalled();
    });
});

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { attach } from '#browser/behaviors/DragGesture.js';
import { configure, register, requestDismiss, resolve } from '#input/InputRouter.js';
import { pointerEvent, stubPointerCapture } from '#support/platform.js';

describe('DragGesture', () => {
    /** @type {HTMLElement} */
    let container;
    /** @type {Set<number>} */
    let captured;
    /** @type {{ stage: string, clientY: number }[]} */
    let reported;

    beforeEach(() => {
        document.body.innerHTML = '<div id="container"><p class="item" id="one">One</p><p id="loose">Loose</p></div>';
        container = document.querySelector('#container');
        captured = stubPointerCapture(container);
        reported = [];
    });

    // The router keeps every registration until it is disposed, so each test's are released after it.
    const handles = [];

    afterEach(() => {
        handles.splice(0).forEach(handle => handle.dispose());
        configure({ bindings: {} });
        vi.useRealTimers();
    });

    const start = (options = {}) => {
        const handle = attach(container, {
            autoScroll: false,
            findSubject: event => event.target.closest('.item'),
            onStart: ({ clientY }) => reported.push({ stage: 'start', clientY }),
            onMove: ({ clientY }) => reported.push({ stage: 'move', clientY }),
            onDrop: ({ clientY }) => reported.push({ stage: 'drop', clientY }),
            onCancel: ({ clientY }) => reported.push({ stage: 'cancel', clientY }),
            ...options,
        });
        handles.push(handle);

        return handle;
    };

    const escape = (target = container) => {
        const event = new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true });
        target.dispatchEvent(event);

        return event;
    };

    const point = (type, properties) => {
        const event = pointerEvent(type, properties);
        (document.querySelector(properties?.on ?? '#one')).dispatchEvent(event);

        return event;
    };

    const press = properties => point('pointerdown', { clientY: 300, ...properties });
    const move = properties => point('pointermove', properties);
    const lift = properties => point('pointerup', properties);
    const stages = () => reported.map(entry => entry.stage);

    describe('with a mouse', () => {
        it('captures the pointer for the rest of the gesture', () => {
            start();

            press({ pointerId: 7 });

            expect(captured.has(7)).toBe(true);
        });

        it('holds the press without calling it a drag', () => {
            start();

            press();

            expect(reported).toEqual([]);
        });

        // A press that never travels is a click, and the caller hears nothing about it.
        it('reports nothing when a press lifts where it landed', () => {
            start();

            press();
            move({ clientY: 302 });
            lift({ clientY: 302 });

            expect(reported).toEqual([]);
        });

        it('swallows the click a drag ends in, and leaves the click of a press that never travelled', () => {
            start();
            const clicked = vi.fn();
            container.addEventListener('click', clicked);
            const click = () => document.querySelector('#one').dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true, detail: 1 }));

            press();
            move({ clientY: 306 });
            lift({ clientY: 306 });
            click();
            press();
            lift();
            click();

            expect(clicked).toHaveBeenCalledOnce();
        });

        it('starts once the press has travelled far enough', () => {
            start();

            press();
            move({ clientY: 306 });

            expect(reported[0]).toEqual({ stage: 'start', clientY: 306 });
        });

        it('honours a threshold the caller set', () => {
            start({ threshold: 20 });

            press();
            move({ clientY: 310 });

            expect(reported).toEqual([]);
        });

        // The threshold measures the axis the gesture reorders along, so sideways travel
        // alone leaves the press a click.
        it('ignores travel across the axis it drags along', () => {
            start();

            press({ clientX: 0 });
            move({ clientX: 400, clientY: 300 });

            expect(reported).toEqual([]);
        });

        it('counts travel in any direction when asked to', () => {
            start({ axis: 'any' });

            press({ clientX: 0 });
            move({ clientX: 400, clientY: 300 });

            expect(stages()).toEqual(['start', 'move']);
        });

        it('reports each move once the drag is under way', () => {
            start();

            press();
            move({ clientY: 306 });
            move({ clientY: 340 });

            expect(reported.at(-1)).toEqual({ stage: 'move', clientY: 340 });
        });

        // The browser would otherwise select the text the pointer sweeps across.
        it('claims a move it is dragging with', () => {
            start();
            press();

            const event = move({ clientY: 340 });

            expect(event.defaultPrevented).toBe(true);
        });

        it('reports the drop where the pointer lifted', () => {
            start();

            press();
            move({ clientY: 340 });
            lift({ clientY: 360 });

            expect(reported.at(-1)).toEqual({ stage: 'drop', clientY: 360 });
        });

        it('gives the pointer back when the drag ends', () => {
            start();

            press({ pointerId: 7 });
            move({ clientY: 340, pointerId: 7 });
            lift({ clientY: 340, pointerId: 7 });

            expect(captured.size).toBe(0);
        });

        it('ignores a press on nothing it can drag', () => {
            start();

            press({ on: '#loose' });
            move({ clientY: 340, on: '#loose' });

            expect(reported).toEqual([]);
        });

        it('ignores a press from a pointer type it was not asked for', () => {
            start({ pointerType: 'mouse' });

            press({ pointerId: 9, pointerType: 'touch' });
            move({ pointerId: 9, pointerType: 'touch', clientY: 340 });

            expect(captured.has(9)).toBe(false);
            expect(reported).toEqual([]);
        });

        it('installs no touch listener when it answers the mouse alone', () => {
            const listening = vi.spyOn(container, 'addEventListener');

            start({ pointerType: 'mouse' });

            expect(listening.mock.calls.map(([type]) => type)).not.toContain('touchmove');
        });

        it('installs its touch listener when it answers every pointer', () => {
            const listening = vi.spyOn(container, 'addEventListener');

            start();

            expect(listening.mock.calls.map(([type]) => type)).toContain('touchmove');
        });

        it('makes the first movement a drag when the threshold is zero', () => {
            start({ threshold: 0 });

            press();
            move({ clientY: 301 });

            expect(stages()).toEqual(['start', 'move']);
        });

        it('ignores a press from a button other than the primary', () => {
            start();

            press({ button: 2 });
            move({ clientY: 340 });

            expect(reported).toEqual([]);
        });

        it('ignores a second pointer while one gesture is under way', () => {
            start();

            press({ pointerId: 1 });
            move({ clientY: 340, pointerId: 2 });

            expect(reported).toEqual([]);
        });
    });

    describe('abandoning a drag', () => {
        it('cancels on Escape rather than dropping', () => {
            start();
            press();
            move({ clientY: 340 });

            const event = escape();

            expect(stages()).toEqual(['start', 'move', 'cancel']);
            expect(event.defaultPrevented).toBe(true);
        });

        // The drag holds the pointer, not the focus, so the key is answered from wherever it is pressed.
        it('cancels on Escape pressed outside the container', () => {
            start();
            press();
            move({ clientY: 340 });

            escape(document.body);

            expect(stages()).toEqual(['start', 'move', 'cancel']);
        });

        it('leaves Escape alone when no drag is under way', () => {
            start();
            press();

            const event = escape();

            expect(event.defaultPrevented).toBe(false);
        });

        it('reports nothing further after a canceled drag lifts', () => {
            start();
            press();
            move({ clientY: 340 });
            escape();

            lift({ clientY: 340 });

            expect(stages()).toEqual(['start', 'move', 'cancel']);
        });

        it('cancels on a platform close request', () => {
            start();
            press();
            move({ clientY: 340 });

            requestDismiss(container);

            expect(stages()).toEqual(['start', 'move', 'cancel']);
        });

        it('cancels when the browser takes the pointer away', () => {
            start();
            press();
            move({ clientY: 340 });

            point('pointercancel', {});

            expect(stages().at(-1)).toBe('cancel');
        });

        it('abandons a drag in flight when disposed', () => {
            const handle = start();
            press();
            move({ clientY: 340 });

            handle.dispose();

            expect(stages().at(-1)).toBe('cancel');
            expect(captured.size).toBe(0);
        });

        it('stops listening once disposed', () => {
            const handle = start();

            handle.dispose();
            press();
            move({ clientY: 340 });

            expect(reported).toEqual([]);
        });
    });

    // A touch that could equally have been a scroll has to rest before it lifts.
    describe('with a touch that must rest first', () => {
        beforeEach(() => vi.useFakeTimers());

        const touch = (type, properties) => point(type, { pointerType: 'touch', ...properties });

        it('leaves the pointer uncaptured while the press is still resting', () => {
            start({ holdDelay: 400 });

            touch('pointerdown', { clientY: 300, pointerId: 7 });

            expect(captured.size).toBe(0);
        });

        it('starts the drag once the press has rested', () => {
            start({ holdDelay: 400 });

            touch('pointerdown', { clientY: 300 });
            vi.advanceTimersByTime(400);

            expect(stages()).toEqual(['start']);
        });

        it('captures the pointer once the press has rested', () => {
            start({ holdDelay: 400 });

            touch('pointerdown', { clientY: 300, pointerId: 7 });
            vi.advanceTimersByTime(400);

            expect(captured.has(7)).toBe(true);
        });

        // A rested press has already made its intent clear, so it drags from where it is.
        it('drags from where it rested without travelling first', () => {
            start({ holdDelay: 400 });

            touch('pointerdown', { clientY: 300 });
            vi.advanceTimersByTime(400);
            touch('pointerup', { clientY: 301 });

            expect(stages()).toEqual(['start', 'drop']);
        });

        // Otherwise every swipe across the list would drag instead of scrolling it.
        it('abandons a press that moves before it has rested', () => {
            start({ holdDelay: 400 });

            touch('pointerdown', { clientY: 300 });
            touch('pointermove', { clientY: 340 });
            vi.advanceTimersByTime(400);
            touch('pointerup', { clientY: 340 });

            expect(reported).toEqual([]);
        });

        it('leaves a swipe to the browser by never capturing the pointer', () => {
            start({ holdDelay: 400 });

            touch('pointerdown', { clientY: 300, pointerId: 7 });
            touch('pointermove', { clientY: 340, pointerId: 7 });
            vi.advanceTimersByTime(400);

            expect(captured.size).toBe(0);
        });

        it('lets a press rest through a tremor smaller than the threshold', () => {
            start({ holdDelay: 400 });

            touch('pointerdown', { clientY: 300 });
            touch('pointermove', { clientY: 302 });
            vi.advanceTimersByTime(400);

            expect(stages()).toEqual(['start']);
        });

        // A mouse has no scroll to compete with, so it drags on travel as it always does.
        it('makes a mouse press wait for travel rather than for rest', () => {
            start({ holdDelay: 400 });

            press();
            move({ clientY: 340 });

            expect(stages()).toEqual(['start', 'move']);
        });

        it('drops the pending rest when disposed', () => {
            const handle = start({ holdDelay: 400 });
            touch('pointerdown', { clientY: 300 });

            handle.dispose();
            vi.advanceTimersByTime(400);

            expect(reported).toEqual([]);
        });
    });

    // The popup around the container is active while it is open, as a popover is, and the drag stands above it.
    describe('inside an active popup', () => {
        /** @type {import('vitest').Mock} */
        let dismissed;

        beforeEach(() => {
            document.body.innerHTML = '<div id="popup"><div id="container"><p class="item" id="one">One</p></div></div><button id="outside">Outside</button>';
            container = document.querySelector('#container');
            captured = stubPointerCapture(container);
            dismissed = vi.fn();
            const popup = register(document.querySelector('#popup'), { onDismiss: dismissed });
            popup.activate();
            handles.push(popup);
        });

        it('stands above the popup while a drag is under way, so Escape cancels the drag and leaves the popup', () => {
            start();
            press();
            move({ clientY: 340 });

            const event = escape();

            expect(stages()).toEqual(['start', 'move', 'cancel']);
            expect(dismissed).not.toHaveBeenCalled();
            expect(event.defaultPrevented).toBe(true);
        });

        it('stands down once the drag is canceled, so the next Escape reaches the popup', () => {
            start();
            press();
            move({ clientY: 340 });
            escape();

            escape();

            expect(dismissed).toHaveBeenCalledWith('dismiss', 'escape');
        });

        it('stands down once the drag has dropped', () => {
            start();
            press();
            move({ clientY: 340 });
            lift({ clientY: 340 });

            escape();

            expect(dismissed).toHaveBeenCalledOnce();
        });

        it('stands up only once the press has become a drag', () => {
            start();
            press();

            escape();

            expect(dismissed).toHaveBeenCalledOnce();
        });

        it('lets a press elsewhere dismiss nothing while a drag is under way', () => {
            start();
            press({ pointerId: 1 });
            move({ clientY: 340, pointerId: 1 });

            press({ pointerId: 2, on: '#outside' });

            expect(dismissed).not.toHaveBeenCalled();
            expect(stages()).toEqual(['start', 'move']);
        });

        it('stands down when disposed mid-drag', () => {
            const handle = start();
            press();
            move({ clientY: 340 });
            handle.dispose();

            escape();

            expect(dismissed).toHaveBeenCalledOnce();
        });
    });

    describe('started by the router', () => {
        const startRouted = (options = {}) => start({ action: 'test.drag', ...options });

        it('declares the press as a route on the container under the action it is given', () => {
            startRouted();

            const event = press({ pointerId: 7 });
            const wasCaptured = captured.has(7);
            lift({ pointerId: 7 });

            // Resolved once the gesture it began has ended, since while one is under way the route matches no other press.
            expect(wasCaptured).toBe(true);
            expect(resolve(event)).toMatchObject({ owner: container, action: 'test.drag', precedence: 'structural' });
        });

        it('leaves a press on nothing it can drag unowned', () => {
            startRouted();

            const event = press({ on: '#loose' });

            expect(resolve(event).action).toBeNull();
            expect(captured.size).toBe(0);
        });

        it('matches only the primary button and the pointer type it answers', () => {
            startRouted({ pointerType: 'mouse' });

            press({ pointerId: 2, button: 2 });
            press({ pointerId: 9, pointerType: 'touch' });

            expect(captured.size).toBe(0);
        });

        it('leaves a press a nested scope routes to that scope', () => {
            container.insertAdjacentHTML('beforeend', '<p class="item" id="inner"><button id="deep">Deep</button></p>');
            startRouted();
            const inner = register(document.querySelector('#inner'), { routes: [{ action: 'test.inner', bindings: [{ type: 'pointerdown' }], handle: () => {} }] });
            handles.push(inner);

            press({ pointerId: 3, on: '#deep' });
            move({ pointerId: 3, clientY: 340, on: '#deep' });

            expect(captured.has(3)).toBe(false);
            expect(reported).toEqual([]);
        });

        it('starts nothing from a remapped press while a gesture is under way', () => {
            configure({ bindings: { 'test.drag': [{ type: 'pointerdown' }] } });
            startRouted();

            press({ pointerId: 1 });
            press({ pointerId: 2 });

            expect(captured.has(1)).toBe(true);
            expect(captured.has(2)).toBe(false);
        });

        it('releases the route once disposed', () => {
            const handle = startRouted();
            handle.dispose();

            const event = press({ pointerId: 4 });

            expect(resolve(event).action).toBeNull();
            expect(captured.has(4)).toBe(false);
        });
    });
});

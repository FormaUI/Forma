import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { attach } from '#behaviours/DragGesture.js';
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

    afterEach(() => vi.useRealTimers());

    const start = (options = {}) => attach(container, {
        autoScroll: false,
        findSubject: event => event.target.closest('.item'),
        onStart: ({ clientY }) => reported.push({ stage: 'start', clientY }),
        onMove: ({ clientY }) => reported.push({ stage: 'move', clientY }),
        onDrop: ({ clientY }) => reported.push({ stage: 'drop', clientY }),
        onCancel: ({ clientY }) => reported.push({ stage: 'cancel', clientY }),
        ...options,
    });

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

            container.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true }));

            expect(stages()).toEqual(['start', 'move', 'cancel']);
        });

        it('leaves Escape alone when no drag is under way', () => {
            start();
            press();

            const event = new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true });
            container.dispatchEvent(event);

            expect(event.defaultPrevented).toBe(false);
        });

        it('reports nothing further after a cancelled drag lifts', () => {
            start();
            press();
            move({ clientY: 340 });
            container.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true }));

            lift({ clientY: 340 });

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
});

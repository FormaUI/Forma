import { describe, it, expect, beforeEach, vi } from 'vitest';
import { attach } from '#browser/components/Frame.js';
import { pointerEvent, stubPointerCapture, stubResizeObserver } from '#support/platform.js';

describe('Frame', () => {
    /** @type {HTMLElement} */
    let box;
    /** @type {{ invokeMethodAsync: import('vitest').Mock }} */
    let owner;

    const options = (overrides = {}) => ({ owner, box: 'self', dragRegionClass: 'grip', handleClass: 'handle', stepPixels: 16, ...overrides });

    // A headless DOM lays nothing out, so an element's rectangle is read back from the styles the frame writes.
    const measureFromStyles = element => {
        element.getBoundingClientRect = () => {
            const value = name => parseFloat(element.style[name]);
            const [left, top, width, height] = [value('left'), value('top'), value('width'), value('height')];

            return { left, top, width, height, right: left + width, bottom: top + height };
        };
    };

    beforeEach(() => {
        document.body.innerHTML = `
            <div id="box" class="is-movable" style="left: 100px; top: 80px; width: 400px; height: 300px; min-width: 200px; min-height: 120px;">
                <div class="grip"><span>Folders</span><button id="close">Close</button></div>
                <div class="handle" data-edge="se"></div>
                <div class="handle" data-edge="w"></div>
                <div class="handle" data-edge="n"></div>
            </div>`;
        box = document.querySelector('#box');
        stubPointerCapture(box);
        owner = { invokeMethodAsync: vi.fn().mockResolvedValue(undefined) };
        measureFromStyles(box);
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
    const press = (on, key, shiftKey = false) => {
        const event = new KeyboardEvent('keydown', { key, shiftKey, bubbles: true, cancelable: true });
        document.querySelector(on).dispatchEvent(event);
        return event.defaultPrevented;
    };

    it('moves the box by its drag region and reports where it came to rest', () => {
        attach(box, options());

        drag('.grip span', { clientX: 200, clientY: 90 }, { clientX: 230, clientY: 110 });

        expect(box.style.left).toBe('130px');
        expect(box.style.top).toBe('100px');
        expect(reported()).toEqual([{ method: 'ReportPlacementAsync', args: [130, 100, 400, 300] }]);
    });

    it('holds a moved box within the viewport', () => {
        attach(box, options());

        drag('.grip span', { clientX: 200, clientY: 90 }, { clientX: 2000, clientY: -900 });

        expect(box.style.left).toBe('600px');
        expect(box.style.top).toBe('0px');
    });

    it('grows the box from a corner handle', () => {
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

    it('marks the frame while a drag is in progress', () => {
        attach(box, options());

        point('pointerdown', '.grip span', { clientX: 200, clientY: 90 });
        point('pointermove', '.grip span', { clientX: 210, clientY: 90 });
        expect(box.classList.contains('is-dragging')).toBe(true);

        point('pointerup', '.grip span', { clientX: 210, clientY: 90 });
        expect(box.classList.contains('is-dragging')).toBe(false);
    });

    it('ignores a press on a control inside the drag region', () => {
        attach(box, options());

        drag('#close', { clientX: 200, clientY: 90 }, { clientX: 260, clientY: 90 });

        expect(box.style.left).toBe('100px');
        expect(reported()).toEqual([]);
    });

    it('leaves the drag region alone while the frame cannot move, and still resizes', () => {
        box.classList.remove('is-movable');
        attach(box, options());

        drag('.grip span', { clientX: 200, clientY: 90 }, { clientX: 260, clientY: 90 });
        expect(press('.grip', 'ArrowRight')).toBe(true);
        expect(press('.grip', 'ArrowRight', true)).toBe(true);

        expect(box.style.left).toBe('100px');
        expect(box.style.width).toBe('416px');
    });

    it('neither moves, resizes nor steps while locked', () => {
        box.classList.add('is-locked');
        const handle = attach(box, options());

        drag('.grip span', { clientX: 200, clientY: 90 }, { clientX: 260, clientY: 90 });
        drag('.handle[data-edge=se]', { clientX: 500, clientY: 380 }, { clientX: 560, clientY: 420 });
        handle.nudge(16, 0, false);

        expect(box.style.left).toBe('100px');
        expect(box.style.width).toBe('400px');
        expect(reported()).toEqual([]);
    });

    it('neither moves nor resizes while maximized', () => {
        box.classList.add('is-maximized');
        attach(box, options());

        drag('.grip span', { clientX: 200, clientY: 90 }, { clientX: 260, clientY: 90 });
        drag('.handle[data-edge=se]', { clientX: 500, clientY: 380 }, { clientX: 560, clientY: 420 });

        expect(box.style.left).toBe('100px');
        expect(box.style.width).toBe('400px');
    });

    it('reports where the box stands when measured', () => {
        const handle = attach(box, options());

        handle.measure();

        expect(reported()).toEqual([{ method: 'ReportPlacementAsync', args: [100, 80, 400, 300] }]);
    });

    it('steps the box with an arrow on the focused drag region, and resizes it with Shift, without scrolling the page', () => {
        attach(box, options());

        expect(press('.grip', 'ArrowRight')).toBe(true);
        expect(press('.grip', 'ArrowDown')).toBe(true);
        expect(press('.grip', 'ArrowRight', true)).toBe(true);

        expect(box.style.left).toBe('116px');
        expect(box.style.top).toBe('96px');
        expect(box.style.width).toBe('416px');
        expect(reported()).toHaveLength(3);
    });

    it('leaves other keys, and arrows pressed inside the drag region, to their own handlers', () => {
        attach(box, options());

        expect(press('.grip', 'Tab')).toBe(false);
        expect(press('#close', 'ArrowRight')).toBe(false);
        expect(box.style.left).toBe('100px');
    });

    it('does not resize from the keyboard while it has no handles', () => {
        document.querySelectorAll('.handle').forEach(handle => handle.remove());
        attach(box, options());

        expect(press('.grip', 'ArrowRight', true)).toBe(true);
        expect(box.style.width).toBe('400px');
    });

    it('nudges the box by a step and reports it', () => {
        const handle = attach(box, options());

        handle.nudge(16, 0, false);
        handle.nudge(0, 16, true);

        expect(box.style.left).toBe('116px');
        expect(box.style.height).toBe('316px');
        expect(reported()).toHaveLength(2);
    });

    it('fits a remembered box into the viewport on attach and reports the change', () => {
        box.style.left = '800px';
        box.style.width = '600px';

        attach(box, options());

        expect(box.style.left).toBe('400px');
        expect(box.style.width).toBe('600px');
        expect(reported()).toEqual([{ method: 'ReportPlacementAsync', args: [400, 80, 600, 300] }]);
    });

    it('leaves a box that is not laid out alone', () => {
        box.getBoundingClientRect = () => ({ left: 0, top: 0, width: 0, height: 0, right: 0, bottom: 0 });

        const handle = attach(box, options());
        handle.fit();

        expect(box.style.left).toBe('100px');
        expect(box.style.width).toBe('400px');
        expect(reported()).toEqual([]);
    });

    it('leaves a box that already fits alone on attach', () => {
        attach(box, options());

        expect(reported()).toEqual([]);
    });

    it('keeps the box within a viewport that shrinks', () => {
        attach(box, options());

        Object.defineProperty(window, 'innerWidth', { value: 450, configurable: true });
        Object.defineProperty(window, 'innerHeight', { value: 250, configurable: true });
        window.dispatchEvent(new Event('resize'));

        expect(box.style.left).toBe('50px');
        expect(box.style.top).toBe('0px');
        expect(box.style.height).toBe('250px');
    });

    it('fits the box again when the frame is laid out again', () => {
        const observers = stubResizeObserver();
        try {
            attach(box, options());
            expect(observers.latest().targets).toContain(box);

            box.style.left = '800px';
            observers.latest().deliver([]);

            expect(box.style.left).toBe('600px');
            expect(reported()).toHaveLength(1);
        } finally {
            observers.restore();
        }
    });

    const boundsAt = rect => {
        document.body.insertAdjacentHTML('beforeend', '<div id="arena"></div>');
        const arena = document.querySelector('#arena');
        arena.getBoundingClientRect = () => ({ ...rect, right: rect.left + rect.width, bottom: rect.top + rect.height });
        return arena;
    };

    it('holds a moved box within its bounding element', () => {
        attach(box, options({ bounds: boundsAt({ left: 50, top: 40, width: 600, height: 500 }) }));

        drag('.grip span', { clientX: 200, clientY: 90 }, { clientX: -500, clientY: -500 });

        expect(box.style.left).toBe('50px');
        expect(box.style.top).toBe('40px');
    });

    it('resizes no further than its bounding element', () => {
        attach(box, options({ bounds: boundsAt({ left: 50, top: 40, width: 600, height: 500 }) }));

        drag('.handle[data-edge=se]', { clientX: 500, clientY: 380 }, { clientX: 1500, clientY: 1500 });

        expect(box.style.width).toBe('550px');
        expect(box.style.height).toBe('460px');
    });

    it('opens an unplaced box at the center of its bounding element', () => {
        box.style.left = '';
        box.style.top = '';
        box.getBoundingClientRect = () => ({ left: 300, top: 250, width: 400, height: 300, right: 700, bottom: 550 });

        attach(box, options({ bounds: boundsAt({ left: 50, top: 40, width: 600, height: 500 }) }));

        expect(box.style.left).toBe('150px');
        expect(box.style.top).toBe('140px');
        expect(reported()).toHaveLength(1);
    });

    it('fills its bounding element when maximized', () => {
        box.classList.add('is-maximized');
        const handle = attach(box, options({ bounds: boundsAt({ left: 50, top: 40, width: 600, height: 500 }) }));

        handle.fit();

        expect([box.style.left, box.style.top, box.style.width, box.style.height]).toEqual(['50px', '40px', '600px', '500px']);
    });

    describe('driving its parent', () => {
        /** @type {HTMLElement} */
        let frame;

        beforeEach(() => {
            document.body.innerHTML = `
                <div id="box" style="left: 100px; top: 80px; width: 400px; height: 300px; min-width: 150px; min-height: 100px;">
                    <div id="frame" class="is-movable" style="min-width: 200px; min-height: 120px; max-width: 700px;">
                        <div class="grip"><span>Folders</span></div>
                        <div class="handle" data-edge="se"></div>
                    </div>
                </div>`;
            box = document.querySelector('#box');
            frame = document.querySelector('#frame');
            stubPointerCapture(frame);
            measureFromStyles(box);
        });

        it('moves the parent by the drag region and reports the parent box', () => {
            attach(frame, options({ box: 'parent' }));

            drag('.grip span', { clientX: 200, clientY: 90 }, { clientX: 230, clientY: 110 });

            expect(box.style.left).toBe('130px');
            expect(frame.style.left).toBe('');
            expect(reported()).toEqual([{ method: 'ReportPlacementAsync', args: [130, 100, 400, 300] }]);
        });

        it('resizes the parent within the limits of both', () => {
            attach(frame, options({ box: 'parent' }));

            drag('.handle[data-edge=se]', { clientX: 500, clientY: 380 }, { clientX: 1500, clientY: -900 });

            expect(box.style.width).toBe('700px');
            expect(box.style.height).toBe('120px');
        });

        it('marks the frame, not the parent, while a drag is in progress', () => {
            attach(frame, options({ box: 'parent' }));

            point('pointerdown', '.grip span', { clientX: 200, clientY: 90 });
            point('pointermove', '.grip span', { clientX: 210, clientY: 90 });

            expect(frame.classList.contains('is-dragging')).toBe(true);
            expect(box.classList.contains('is-dragging')).toBe(false);
        });
    });

    it('stops listening once disposed', () => {
        const handle = attach(box, options());
        handle.dispose();

        drag('.grip span', { clientX: 200, clientY: 90 }, { clientX: 260, clientY: 90 });
        Object.defineProperty(window, 'innerWidth', { value: 450, configurable: true });
        window.dispatchEvent(new Event('resize'));

        expect(box.style.left).toBe('100px');
        expect(reported()).toEqual([]);
    });
});

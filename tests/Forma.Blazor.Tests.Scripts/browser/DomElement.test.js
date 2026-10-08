import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { DomElement } from '#browser/DomElement.mjs';
import { ResizeObserverPool } from '#browser/ResizeObserverPool.mjs';
import { stubAnimationFrames, stubResizeObserver } from '#support/platform.js';

describe('DomElement', () => {
    /** @type {ReturnType<typeof stubAnimationFrames>} */
    let frames;
    /** @type {ReturnType<typeof stubResizeObserver>} */
    let resizes;
    /** @type {HTMLElement} */
    let target;
    /** @type {DomElement[]} */
    let elements;

    beforeEach(() => {
        frames = stubAnimationFrames();
        resizes = stubResizeObserver();
        document.body.innerHTML = '<div id="list"><div id="target"></div></div><div id="other"></div>';
        target = document.querySelector('#target');
        elements = [];
    });

    afterEach(() => {
        elements.forEach(element => element.dispose());
        frames.restore();
        resizes.restore();
    });

    const createElement = (forget = vi.fn()) => {
        const element = new DomElement(target, forget);
        elements.push(element);

        return element;
    };

    // The element's size changes as it leaves the document, which is what has it checked.
    const resize = () => resizes.latest().deliver([{ target }]);

    it('releases what is held on it a frame after it leaves the document', () => {
        const element = createElement();
        const first = vi.fn();
        const second = vi.fn();
        element.hold(first);
        element.hold(second);

        target.remove();
        resize();
        expect(first).not.toHaveBeenCalled();
        frames.flush();

        expect(first).toHaveBeenCalledOnce();
        expect(second).toHaveBeenCalledOnce();
    });

    it('takes an element moved within the document as still there', () => {
        const element = createElement();
        const release = vi.fn();
        element.hold(release);

        target.remove();
        resize();
        document.querySelector('#other').appendChild(target);
        frames.flush();

        expect(release).not.toHaveBeenCalled();
    });

    it('takes an element that only resized as still there', () => {
        const element = createElement();
        const release = vi.fn();
        element.hold(release);

        resize();
        frames.flush();

        expect(release).not.toHaveBeenCalled();
    });

    it('forgets itself and stops watching once it has left', () => {
        const forget = vi.fn();
        const element = createElement(forget);
        element.hold(() => {});

        target.remove();
        resize();
        frames.flush();

        expect(forget).toHaveBeenCalledOnce();
        expect(ResizeObserverPool.size).toBe(0);
    });

    it('lets a registration go without releasing it', () => {
        const element = createElement();
        const kept = vi.fn();
        const letGo = vi.fn();
        element.hold(kept);
        element.hold(letGo).dispose();

        target.remove();
        resize();
        frames.flush();

        expect(letGo).not.toHaveBeenCalled();
        expect(kept).toHaveBeenCalledOnce();
    });

    it('forgets itself once the last registration is let go', () => {
        const forget = vi.fn();
        const element = createElement(forget);
        const hold = element.hold(() => {});

        hold.dispose();
        hold.dispose();

        expect(forget).toHaveBeenCalledOnce();
        expect(ResizeObserverPool.size).toBe(0);
    });

    it('leaves another watcher\'s element on the same native element watching', () => {
        const disposed = createElement();
        const kept = createElement();
        const release = vi.fn();
        disposed.hold(() => {});
        kept.hold(release);

        disposed.dispose();
        target.remove();
        resize();
        frames.flush();

        expect(release).toHaveBeenCalledOnce();
    });
});

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { DomElements } from '#browser/DomElements.mjs';
import { stubAnimationFrames, stubResizeObserver } from '#support/platform.js';

describe('DomElements', () => {
    /** @type {ReturnType<typeof stubAnimationFrames>} */
    let frames;
    /** @type {ReturnType<typeof stubResizeObserver>} */
    let resizes;
    /** @type {DomElements} */
    let elements;
    /** @type {HTMLElement} */
    let first;
    /** @type {HTMLElement} */
    let second;

    beforeEach(() => {
        frames = stubAnimationFrames();
        resizes = stubResizeObserver();
        document.body.innerHTML = '<div id="first"></div><div id="second"></div>';
        first = document.querySelector('#first');
        second = document.querySelector('#second');
        elements = new DomElements();
    });

    afterEach(() => {
        elements.dispose();
        frames.restore();
        resizes.restore();
    });

    it('gives one DomElement for each native element', () => {
        expect(elements.get(first)).toBe(elements.get(first));
        expect(elements.get(first)).not.toBe(elements.get(second));
    });

    it('gives a new DomElement once the last registration on the old one is let go', () => {
        const element = elements.get(first);

        element.hold(() => {}).dispose();

        expect(elements.get(first)).not.toBe(element);
    });

    it('gives a new DomElement once the old one has left the document', () => {
        const element = elements.get(first);
        const release = vi.fn();
        element.hold(release);

        first.remove();
        resizes.latest().deliver([{ target: first }]);
        frames.flush();

        expect(release).toHaveBeenCalledOnce();
        expect(elements.get(first)).not.toBe(element);
    });

    it('stops watching every element once disposed, letting their registrations go', () => {
        const release = vi.fn();
        elements.get(first).hold(release);
        elements.get(second).hold(release);

        elements.dispose();

        expect(resizes.latest().isConnected).toBe(false);
        expect(release).not.toHaveBeenCalled();
    });
});

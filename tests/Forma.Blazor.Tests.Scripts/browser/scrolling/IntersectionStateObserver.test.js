import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { IntersectionStateObserver } from '#browser/scrolling/IntersectionStateObserver.mjs';
import { stubIntersectionObserver } from '#support/platform.js';

describe('IntersectionStateObserver', () => {
    /** @type {ReturnType<typeof stubIntersectionObserver>} */
    let observers;
    /** @type {HTMLElement} */
    let panel;
    /** @type {HTMLElement} */
    let sentinel;
    /** @type {ReturnType<typeof vi.fn>} */
    let publish;
    /** @type {IntersectionStateObserver[]} */
    let sources;

    const box = (element, rect) => {
        element.getBoundingClientRect = () => ({ left: 0, right: 100, ...rect });
        element.getClientRects = () => [rect];
    };

    beforeEach(() => {
        observers = stubIntersectionObserver();
        document.body.innerHTML = '<div id="panel"><div id="sentinel"></div></div>';
        panel = document.querySelector('#panel');
        sentinel = document.querySelector('#sentinel');
        box(panel, { top: 0, bottom: 300 });
        publish = vi.fn();
        sources = [];
    });

    afterEach(() => {
        sources.forEach(source => source.dispose());
        observers.restore();
    });

    const watch = (options = { root: panel }) => {
        const source = new IntersectionStateObserver(sentinel, options, publish);
        sources.push(source);

        return source;
    };

    const report = isIntersecting => observers.latest().deliver([{ target: sentinel, isIntersecting }]);

    it('watches the sentinel against the root and margin it was given', () => {
        watch({ root: panel, rootMargin: 200 });

        expect(observers.latest().targets).toEqual([sentinel]);
        expect(observers.latest().options).toEqual(expect.objectContaining({ root: panel, rootMargin: '200px' }));
    });

    it('reads the sentinel in view where it overlaps the root', () => {
        box(sentinel, { top: 250, bottom: 260 });

        expect(watch().read()).toEqual({ isIntersecting: true });
    });

    it('reads the sentinel out of view below the root', () => {
        box(sentinel, { top: 450, bottom: 460 });

        expect(watch().read()).toEqual({ isIntersecting: false });
    });

    it('reads the sentinel in view within the margin around the root', () => {
        box(sentinel, { top: 450, bottom: 460 });

        expect(watch({ root: panel, rootMargin: 200 }).read()).toEqual({ isIntersecting: true });
    });

    it('reads a sentinel that has no box as out of view', () => {
        sentinel.getBoundingClientRect = () => ({ top: 0, bottom: 0, left: 0, right: 0 });
        sentinel.getClientRects = () => [];

        expect(watch().read()).toEqual({ isIntersecting: false });
    });

    it('publishes the observer\'s report where it corrects the reading', () => {
        box(sentinel, { top: 450, bottom: 460 });
        watch().read();

        report(true);

        expect(publish).toHaveBeenCalledExactlyOnceWith({ isIntersecting: true });
    });

    it('stays quiet while the observer reports what was read', () => {
        box(sentinel, { top: 450, bottom: 460 });
        watch().read();

        report(false);

        expect(publish).not.toHaveBeenCalled();
    });

    it('publishes the observer\'s next report after a refresh, unchanged or not', () => {
        box(sentinel, { top: 250, bottom: 260 });
        const source = watch();
        source.read();

        source.refresh();
        report(true);

        expect(publish).toHaveBeenCalledExactlyOnceWith({ isIntersecting: true });
    });

    it('stops watching once disposed', () => {
        watch().dispose();

        expect(observers.latest().isConnected).toBe(false);
    });

    // A report queued before the observer disconnects is still delivered.
    it('ignores a report delivered after disposal', () => {
        const source = watch();
        source.read();
        const observer = observers.latest();

        source.dispose();
        observer.deliver([{ target: sentinel, isIntersecting: true }]);

        expect(publish).not.toHaveBeenCalled();
    });
});

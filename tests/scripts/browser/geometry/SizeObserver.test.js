import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { SizeObserver } from '#browser/geometry/SizeObserver.js';
import { stubResizeObserver } from '#support/platform.js';

describe('SizeObserver', () => {
    /** @type {ReturnType<typeof stubResizeObserver>} */
    let resizes;
    /** @type {HTMLElement} */
    let panel;
    /** @type {ReturnType<typeof vi.fn>} */
    let publish;
    /** @type {SizeObserver[]} */
    let sources;

    // The panel's box as drawn, which a transform scales.
    const draw = (width, height) => {
        panel.getBoundingClientRect = () => ({ width, height, top: 0, left: 0, right: width, bottom: height });
    };

    // The resize observer reporting the panel's border box as laid out, along its writing mode.
    const resize = (inlineSize, blockSize, observer = resizes.latest()) =>
        observer.deliver([{ target: panel, borderBoxSize: [{ inlineSize, blockSize }] }]);

    beforeEach(() => {
        resizes = stubResizeObserver();
        document.body.innerHTML = '<div id="panel"></div>';
        panel = document.querySelector('#panel');
        draw(320, 48);
        publish = vi.fn();
        sources = [];
    });

    afterEach(() => {
        sources.forEach(source => source.dispose());
        resizes.restore();
    });

    const watch = () => {
        const source = new SizeObserver(panel, {}, publish);
        sources.push(source);

        return source;
    };

    it('reads its border box as the inline and block sizes of a horizontal writing mode', () => {
        expect(watch().read()).toEqual({ inlineSize: 320, blockSize: 48 });
        expect(resizes.latest().targets).toEqual([panel]);
    });

    it.each(['vertical-rl', 'sideways-lr'])('reads the height as the inline size in the %s writing mode', writingMode => {
        panel.style.writingMode = writingMode;

        expect(watch().read()).toEqual({ inlineSize: 48, blockSize: 320 });
    });

    it('has the resize observer measure the target again as it reads it', () => {
        const source = watch();
        const observe = vi.spyOn(resizes.latest(), 'observe');

        source.read();

        expect(observe).toHaveBeenCalledExactlyOnceWith(panel);
    });

    it('publishes a size the layout changed, and nothing for a resize that left it as it was', () => {
        watch().read();

        resize(320, 48);
        resize(200, 48);

        expect(publish.mock.calls).toEqual([[{ inlineSize: 200, blockSize: 48 }]]);
    });

    it('corrects a size a transform scaled as it was read with the size the layout gave it', () => {
        draw(160, 24);
        watch().read();

        resize(320, 48);

        expect(publish.mock.calls).toEqual([[{ inlineSize: 320, blockSize: 48 }]]);
    });

    it('takes a change below a hundredth of a pixel for the layout rounding and publishes nothing', () => {
        draw(320.001, 48);
        watch().read();

        resize(320.004, 48);

        expect(publish).not.toHaveBeenCalled();
    });

    it('publishes the size again when refreshed, though it has not changed, and has the resize observer measure it', () => {
        const source = watch();
        source.read();
        const observe = vi.spyOn(resizes.latest(), 'observe');

        source.refresh();

        expect(publish.mock.calls).toEqual([[{ inlineSize: 320, blockSize: 48 }]]);
        expect(observe).toHaveBeenCalledExactlyOnceWith(panel);
    });

    it('stops observing once disposed, a resize reported before then publishing nothing', () => {
        const source = watch();
        source.read();
        const observer = resizes.latest();

        source.dispose();
        resize(100, 48, observer);

        expect(publish).not.toHaveBeenCalled();
        expect(observer.targets).toEqual([]);
    });
});

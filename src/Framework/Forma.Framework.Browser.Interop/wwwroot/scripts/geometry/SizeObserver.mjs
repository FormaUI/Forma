/**
 * SizeObserver — watches a target's size along its inline and block axes and publishes each change
 *
 * Each size is the target's border box as laid out, which the resize observer measures along the target's writing mode
 * and a transform leaves as it is; a read takes the box as drawn at once and has the resize observer measure it again,
 * so a size a transform scaled is corrected as the measurement arrives.
 */

import { ResizeObserverPool } from '../ResizeObserverPool.mjs';

/** @typedef {{ inlineSize: number, blockSize: number }} SizeState */

// To the hundredth of a pixel, so the layout's rounding reports no change.
const round = value => Math.round(value * 100) / 100;

export class SizeObserver {
    #target;
    #publish;
    #subscription;
    #lastState = null;
    #isDisposed = false;

    /**
     * @param {Element} target The element watched.
     * @param {object} options The watch takes no options.
     * @param {(state: SizeState) => void} publish Publishes each changed state.
     */
    constructor(target, options, publish) {
        this.#target = target;
        this.#publish = publish;
        this.#subscription = ResizeObserverPool.observe(target, entry => this.#take(SizeObserver.#laidOut(entry)));
    }

    /** Reads the target's size now. */
    read() {
        this.#lastState = this.#drawn();
        this.#subscription.remeasure();

        return this.#lastState;
    }

    /** Measures and publishes the target's size again. */
    refresh() {
        this.#lastState = null;
        this.#take(this.#drawn());
        this.#subscription.remeasure();
    }

    dispose() {
        this.#isDisposed = true;
        this.#subscription.dispose();
    }

    #take(state) {
        // Reports queued before disposal still arrive.
        if (this.#isDisposed || (this.#lastState?.inlineSize === state.inlineSize && this.#lastState?.blockSize === state.blockSize)) {
            return;
        }

        this.#lastState = state;
        this.#publish(state);
    }

    // The border box as drawn, its width the inline size in a horizontal writing mode and its height in a vertical or
    // sideways one.
    #drawn() {
        const { width, height } = this.#target.getBoundingClientRect();
        const isVertical = /^(vertical|sideways)/.test(getComputedStyle(this.#target).writingMode);

        return isVertical
            ? { inlineSize: round(height), blockSize: round(width) }
            : { inlineSize: round(width), blockSize: round(height) };
    }

    // The border box the resize observer measured, already along the target's writing mode.
    static #laidOut(entry) {
        const [box] = entry.borderBoxSize;

        return { inlineSize: round(box.inlineSize), blockSize: round(box.blockSize) };
    }
}

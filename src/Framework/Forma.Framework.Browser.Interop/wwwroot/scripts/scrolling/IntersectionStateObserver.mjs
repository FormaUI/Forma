/**
 * IntersectionStateObserver — watches whether a target is in view of a root and publishes each change
 */

import { IntersectionObserverPool } from '../IntersectionObserverPool.mjs';

/** @typedef {{ isIntersecting: boolean }} IntersectionState */

export class IntersectionStateObserver {
    #target;
    #root;
    #rootMargin;
    #publish;
    #subscription;
    #lastState = null;
    #isDisposed = false;

    /**
     * @param {Element} target The element watched.
     * @param {{ root?: Element | null, rootMargin?: number }} options The root it comes into view of, null for the viewport, and the margin around the root in pixels.
     * @param {(state: IntersectionState) => void} publish Publishes each changed state.
     */
    constructor(target, { root = null, rootMargin = 0 } = {}, publish) {
        this.#target = target;
        this.#root = root;
        this.#rootMargin = rootMargin;
        this.#publish = publish;
        this.#subscription = IntersectionObserverPool.observe(target, { root, rootMargin }, entry => this.#take(entry.isIntersecting));
    }

    /** Reads whether the target is in view now. */
    read() {
        this.#lastState = { isIntersecting: this.#isIntersecting() };

        return this.#lastState;
    }

    /** Measures and publishes whether the target is in view again. */
    refresh() {
        this.#lastState = null;
        this.#subscription.remeasure();
    }

    dispose() {
        this.#isDisposed = true;
        this.#subscription.dispose();
    }

    #take(isIntersecting) {
        // Reports queued before disposal still arrive.
        if (this.#isDisposed || this.#lastState?.isIntersecting === isIntersecting) {
            return;
        }

        this.#lastState = { isIntersecting };
        this.#publish(this.#lastState);
    }

    #isIntersecting() {
        if (this.#target.getClientRects().length === 0) {
            return false;
        }

        const target = this.#target.getBoundingClientRect();
        const root = this.#root
            ? this.#root.getBoundingClientRect()
            : { top: 0, left: 0, bottom: document.documentElement.clientHeight, right: document.documentElement.clientWidth };
        const margin = this.#rootMargin;

        return target.top <= root.bottom + margin
            && target.bottom >= root.top - margin
            && target.left <= root.right + margin
            && target.right >= root.left - margin;
    }
}

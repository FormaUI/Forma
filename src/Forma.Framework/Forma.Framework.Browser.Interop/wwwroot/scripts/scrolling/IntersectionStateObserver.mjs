/**
 * IntersectionStateObserver — watches whether a target is in view of a root and publishes each change
 *
 * The state is read at once from the boxes of the target and the root, the root grown by its margin,
 * since the native observer reports only later; each report the observer then makes is compared with
 * the last state published, and only one that differs is published, so a reading the observer
 * corrects is published as a change. A target with no box, as when it is not displayed, is out of view.
 */

import { IntersectionObserverPool } from '../IntersectionObserverPool.mjs';

/** @typedef {{ isIntersecting: boolean }} IntersectionState */

export class IntersectionStateObserver {
    #target;
    #root;
    #rootMargin;
    #publish;
    #subscription;
    #last = null;
    #isDisposed = false;

    /**
     * @param {Element} target The element watched.
     * @param {{ root?: Element | null, rootMargin?: number }} options The root it comes into view of, null for the viewport, and the margin around the root in pixels.
     * @param {(state: IntersectionState) => void} publish Takes each state that differs from the last.
     */
    constructor(target, { root = null, rootMargin = 0 } = {}, publish) {
        this.#target = target;
        this.#root = root;
        this.#rootMargin = rootMargin;
        this.#publish = publish;
        this.#subscription = IntersectionObserverPool.observe(target, { root, rootMargin }, entry => this.#take(entry.isIntersecting));
    }

    /** Reads whether the target is in view now, taking the reading as the last state published. */
    read() {
        this.#last = { isIntersecting: this.#isIntersecting() };

        return this.#last;
    }

    /** Has the native observer report the target afresh, publishing its report even where it equals the last. */
    refresh() {
        this.#last = null;
        this.#subscription.remeasure();
    }

    dispose() {
        this.#isDisposed = true;
        this.#subscription.dispose();
    }

    #take(isIntersecting) {
        // Reports queued before disposal still arrive.
        if (this.#isDisposed || this.#last?.isIntersecting === isIntersecting) {
            return;
        }

        this.#last = { isIntersecting };
        this.#publish(this.#last);
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

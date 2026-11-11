/**
 * ScrollStateObserver — watches a scroll container's state and publishes each change
 */

import { FrameScheduler } from '../../../core/scripts/FrameScheduler.js';
import { ResizeObserverPool } from '../ResizeObserverPool.mjs';
import { ScrollContainer } from './ScrollContainer.mjs';

/** @typedef {import('./ScrollContainer.mjs').ScrollState} ScrollState */

export class ScrollStateObserver {
    #track;
    #container;
    #measureOptions;
    #publish;
    #controller = new AbortController();
    #mutations;
    #restyling = null;
    #trackSize;
    #childSizes = [];
    #lastState = null;
    #scheduler = new FrameScheduler();
    #isDisposed = false;

    /**
     * @param {HTMLElement} track The scroll container.
     * @param {{ axis?: 'inline' | 'block', frozenSelector?: string | null, viewportExtent?: boolean }} options
     *   The axis it scrolls along, the selector of its frozen parts, and whether its visible extent is measured.
     * @param {(state: ScrollState) => void} publish Publishes each changed state.
     */
    constructor(track, { axis = 'inline', frozenSelector = null, viewportExtent = false } = {}, publish) {
        this.#track = track;
        this.#container = new ScrollContainer(track, axis);
        this.#measureOptions = { frozenSelector, viewportExtent };
        this.#publish = publish;

        track.addEventListener('scroll', () => this.#schedule(), { signal: this.#controller.signal, passive: true });
        this.#trackSize = ResizeObserverPool.observe(track, () => this.#schedule());
        this.#watchChildren();

        this.#mutations = new MutationObserver(() => {
            this.#watchChildren();
            this.#schedule();
        });

        this.#mutations.observe(track, { childList: true });

        // A frozen part moves as its style or its class changes, which resizes nothing the track holds, so a change of either is
        // measured again.
        if (frozenSelector) {
            this.#restyling = new MutationObserver(() => this.#schedule());
            this.#restyling.observe(track, { subtree: true, attributes: true, attributeFilter: ['style', 'class'] });
        }
    }

    /** Measures the container's state now. */
    read() {
        this.#lastState = this.#container.measure(this.#measureOptions);

        return this.#lastState;
    }

    /** Measures and publishes the container's state again. */
    refresh() {
        this.#lastState = null;
        this.#schedule();
    }

    dispose() {
        this.#isDisposed = true;
        this.#scheduler.cancel();
        this.#controller.abort();
        this.#mutations.disconnect();
        this.#restyling?.disconnect();
        this.#trackSize.dispose();

        for (const size of this.#childSizes) {
            size.dispose();
        }

        this.#childSizes = [];
    }

    #watchChildren() {
        for (const size of this.#childSizes) {
            size.dispose();
        }

        this.#childSizes = [...this.#track.children].map(child => ResizeObserverPool.observe(child, () => this.#schedule()));
    }

    #schedule() {
        if (!this.#isDisposed) {
            this.#scheduler.request(() => this.#report());
        }
    }

    #report() {
        // A track out of the document measures as nothing.
        if (this.#isDisposed || !this.#track.isConnected) {
            return;
        }

        const nextState = this.#container.measure(this.#measureOptions);

        if (this.#lastState
            && this.#lastState.canScrollBack === nextState.canScrollBack
            && this.#lastState.canScrollForward === nextState.canScrollForward
            && this.#lastState.frozenExtent === nextState.frozenExtent
            && this.#lastState.cornerExtent === nextState.cornerExtent
            && this.#lastState.viewportExtent === nextState.viewportExtent) {
            return;
        }

        this.#lastState = nextState;
        this.#publish(nextState);
    }
}

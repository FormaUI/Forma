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
    #measuring;
    #publish;
    #controller = new AbortController();
    #mutations;
    #restyling = null;
    #trackSize;
    #childSizes = [];
    #last = null;
    #scheduler = new FrameScheduler();
    #isDisposed = false;

    /**
     * @param {HTMLElement} track The scroll container.
     * @param {{ axis?: 'inline' | 'block', frozen?: string | null, viewportExtent?: boolean }} options
     *   The axis it scrolls along, the selector of its frozen parts, and whether its visible extent is measured.
     * @param {(state: ScrollState) => void} publish Publishes each changed state.
     */
    constructor(track, { axis = 'inline', frozen = null, viewportExtent = false } = {}, publish) {
        this.#track = track;
        this.#container = new ScrollContainer(track, axis);
        this.#measuring = { frozen, viewportExtent };
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
        if (frozen) {
            this.#restyling = new MutationObserver(() => this.#schedule());
            this.#restyling.observe(track, { subtree: true, attributes: true, attributeFilter: ['style', 'class'] });
        }
    }

    /** Measures the container's state now. */
    read() {
        this.#last = this.#container.measure(this.#measuring);

        return this.#last;
    }

    /** Measures and publishes the container's state again. */
    refresh() {
        this.#last = null;
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

        const next = this.#container.measure(this.#measuring);

        if (this.#last
            && this.#last.canScrollBack === next.canScrollBack
            && this.#last.canScrollForward === next.canScrollForward
            && this.#last.frozenExtent === next.frozenExtent
            && this.#last.cornerExtent === next.cornerExtent
            && this.#last.viewportExtent === next.viewportExtent) {
            return;
        }

        this.#last = next;
        this.#publish(next);
    }
}

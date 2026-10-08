/**
 * ScrollStateObserver — watches a scroll container's state and publishes each change
 *
 * Scrolling, a size change of the container or of a child, and a child added or removed each schedule
 * one measurement for the next frame. Each measurement is a snapshot the container takes, compared
 * whole with the last one published, and only a snapshot that differs is published.
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
    #trackSize;
    #childSizes = [];
    #last = null;
    #scheduler = new FrameScheduler();
    #isDisposed = false;

    /**
     * @param {HTMLElement} track The scroll container.
     * @param {{ axis?: 'inline' | 'block', frozen?: string | null, viewportExtent?: boolean }} options
     *   The axis it scrolls along, the selector of its frozen parts, and whether its visible extent is measured.
     * @param {(state: ScrollState) => void} publish Takes each state that differs from the last.
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
    }

    /** Measures the container now, taking the snapshot as the last state published. */
    read() {
        this.#last = this.#container.measure(this.#measuring);

        return this.#last;
    }

    /** Measures again on the next frame, publishing the snapshot even where it equals the last. */
    refresh() {
        this.#last = null;
        this.#schedule();
    }

    dispose() {
        this.#isDisposed = true;
        this.#scheduler.cancel();
        this.#controller.abort();
        this.#mutations.disconnect();
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

/**
 * DomElement — a native element an owner registers on, and the registrations held on it
 */

import { FrameScheduler } from '../../core/scripts/FrameScheduler.js';
import { ResizeObserverPool } from './ResizeObserverPool.mjs';

export class DomElement {
    #element;
    #forget;
    /** @type {Set<() => void>} */
    #releases = new Set();
    #size;
    #scheduler = new FrameScheduler();
    #isDisposed = false;

    /**
     * @param {Element} element The native element.
     * @param {() => void} forget The callback that forgets the element.
     */
    constructor(element, forget) {
        this.#element = element;
        this.#forget = forget;
        this.#size = ResizeObserverPool.observe(element, () => this.#scheduler.request(() => this.#check()));
    }

    /** The native element. */
    get element() {
        return this.#element;
    }

    /**
     * Holds a registration on the element.
     * @param {() => void} release The registration's release.
     */
    hold(release) {
        this.#releases.add(release);
        let isLetGo = false;

        return {
            dispose: () => {
                if (isLetGo) {
                    return;
                }

                isLetGo = true;
                this.#releases.delete(release);

                if (this.#releases.size === 0) {
                    this.dispose();
                }
            },
        };
    }

    /** Stops watching the element and forgets it. */
    dispose() {
        if (this.#isDisposed) {
            return;
        }

        this.#isDisposed = true;
        this.#scheduler.cancel();
        this.#size.dispose();
        this.#forget();
    }

    #check() {
        if (this.#isDisposed || this.#element.isConnected) {
            return;
        }

        // A release can dispose the element.
        for (const release of [...this.#releases]) {
            release();
        }

        this.dispose();
    }
}

/**
 * DomElement — a native element an owner registers on, and the registrations held on it
 *
 * The owner's session keeps one for each native element. The element's size is watched through the
 * document's shared resize observer, since a rendered element's size changes as it leaves the
 * document, and a frame after each change the element is checked: one moved within the document is
 * back in it by then, while one taken out of it has left, which releases every registration held on
 * it. A release frees the registration's browser resources and leaves its token free for a later
 * generation.
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
     * @param {() => void} forget Called once nothing is held on the element any longer.
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
     * Holds a registration on the element, calling its release should the element leave the document;
     * the returned hold lets the registration go without calling it.
     * @param {() => void} release
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

        // Each release lets its hold go, which can dispose the element before the loop ends.
        for (const release of [...this.#releases]) {
            release();
        }

        this.dispose();
    }
}

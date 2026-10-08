/**
 * Scroll — what a surface does with scrolling
 *
 * A handle tells its owner as a sentinel comes into view. Moving a container to its center is
 * exported beside it. A right-to-left container counts scrollLeft downward from zero, so its
 * inline middle is mirrored.
 */

import { Direction } from '../../../core/scripts/Direction.js';
import { tell } from '../../../core/scripts/Owner.js';

/** @typedef {import('../../../core/scripts/Owner.js').Owner} Owner */

/**
 * @param {HTMLElement} target The surface.
 * @param {{ sentinel?: { owner: Owner, root?: HTMLElement | null, rootMargin?: string } }} options
 *   What the handle does for the target: treat it as a sentinel and tell the owner through
 *   HandleReachedAsync as it comes into view of the root.
 */
export function attach(target, options = {}) {
    return new Scroll(target, options);
}

/** @param {HTMLElement} container */
export function toCenter(container) {
    const middle = (container.scrollWidth - container.clientWidth) / 2;

    container.scrollLeft = Direction.isRightToLeft(container) ? -middle : middle;
    container.scrollTop = (container.scrollHeight - container.clientHeight) / 2;
}

/** The parts a surface asked for, disposed together. */
export class Scroll {
    #parts = [];

    constructor(target, { sentinel } = {}) {
        if (sentinel) {
            this.#parts.push(new Sentinel(target, sentinel));
        }
    }

    dispose() {
        for (const part of this.#parts) {
            part.dispose();
        }

        this.#parts = [];
    }
}

/** Tells the owner as the target comes into view. */
class Sentinel {
    #observer;
    #isDisposed = false;

    constructor(target, { owner, root = null, rootMargin = '200px' }) {
        this.#observer = new IntersectionObserver(observations => {
            // Observations queued before disconnect still run.
            if (!this.#isDisposed && observations.some(observation => observation.isIntersecting)) {
                tell(owner, 'HandleReachedAsync');
            }
        }, { root, rootMargin });

        this.#observer.observe(target);
    }

    dispose() {
        this.#isDisposed = true;
        this.#observer.disconnect();
    }
}

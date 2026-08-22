/**
 * InfiniteScroll — asking for the next page as a sentinel comes into view
 *
 * The handle keeps the observer, so the element can be released before it leaves the DOM
 * rather than the observer having to outlive it.
 */

/**
 * @param {HTMLElement} target The sentinel that trails the loaded items.
 * @param {{
 *   owner: { invokeMethodAsync: (name: string, ...args: unknown[]) => Promise<unknown> },
 *   root?: HTMLElement,
 *   rootMargin?: string
 * }} options
 */
export function attach(target, options) {
    return new InfiniteScroll(target, options);
}

class InfiniteScroll {
    #observer;
    #isDisposed = false;

    constructor(target, { owner, root = null, rootMargin = '200px' }) {
        // A panel that scrolls rather than the page is the root, so intersection is
        // measured against it and not the viewport.
        this.#observer = new IntersectionObserver(observations => {
            // Observations already queued still run after disconnect, and the owner may be gone by then.
            if (!this.#isDisposed && observations.some(observation => observation.isIntersecting)) {
                owner.invokeMethodAsync('HandleReachedAsync');
            }
        }, { root, rootMargin });

        this.#observer.observe(target);
    }

    dispose() {
        this.#isDisposed = true;
        this.#observer.disconnect();
    }
}

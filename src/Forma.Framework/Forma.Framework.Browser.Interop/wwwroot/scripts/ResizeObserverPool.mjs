/**
 * ResizeObserverPool — the document's one resize observer, shared by every observation that watches a size
 */

/** @typedef {(entry: ResizeObserverEntry) => void} SizeListener */

/** @type {ResizeObserver | null} */
let observer = null;

/** @type {Map<Element, Set<SizeListener>>} */
const listenersByTarget = new Map();

export class ResizeObserverPool {
    /**
     * Calls the listener whenever the target's size changes, until the returned subscription is disposed.
     * @param {Element} target
     * @param {SizeListener} listener
     */
    static observe(target, listener) {
        observer ??= new ResizeObserver(entries => ResizeObserverPool.#deliver(entries));
        let listeners = listenersByTarget.get(target);

        if (!listeners) {
            listeners = new Set();
            listenersByTarget.set(target, listeners);
            observer.observe(target);
        }

        listeners.add(listener);
        let isDisposed = false;

        return {
            dispose() {
                if (isDisposed) {
                    return;
                }

                isDisposed = true;
                ResizeObserverPool.#forget(target, listener);
            },
        };
    }

    /** The number of elements the pool observes. */
    static get size() {
        return listenersByTarget.size;
    }

    static #deliver(entries) {
        for (const entry of entries) {
            // A listener can dispose another, so each target's listeners are copied before any runs.
            for (const listener of [...(listenersByTarget.get(entry.target) ?? [])]) {
                listener(entry);
            }
        }
    }

    static #forget(target, listener) {
        const listeners = listenersByTarget.get(target);

        if (!listeners?.delete(listener) || listeners.size > 0) {
            return;
        }

        listenersByTarget.delete(target);
        observer?.unobserve(target);

        if (listenersByTarget.size === 0) {
            observer?.disconnect();
            observer = null;
        }
    }
}

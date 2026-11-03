/**
 * ResizeObserverPool — the document's one resize observer
 */

/** @typedef {(entry: ResizeObserverEntry) => void} SizeListener */

/** @type {ResizeObserver | null} */
let observer = null;

/** @type {Map<Element, Set<SizeListener>>} */
const listenersByTarget = new Map();

export class ResizeObserverPool {
    /**
     * Subscribes the listener to the target's size changes.
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

    static #deliver(entries) {
        for (const entry of entries) {
            // A listener can dispose another.
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

/**
 * IntersectionObserverPool — the document's intersection observers, one for each root, margin and threshold
 *
 * Subscribers asking for the same root, margin and threshold share one native observer, which observes
 * each of their targets once; a subscriber with any of the three different gets an observer of its own.
 * An observer is disconnected once its last target is let go.
 */

/** @typedef {(entry: IntersectionObserverEntry) => void} IntersectionListener */
/** @typedef {{ observer: IntersectionObserver, listenersByTarget: Map<Element, Set<IntersectionListener>> }} Pool */

/** @type {Map<Element | null, Map<string, Pool>>} */
const poolsByRoot = new Map();

export class IntersectionObserverPool {
    /**
     * Calls the listener with each intersection change of the target against the root, until the returned subscription is disposed.
     * @param {Element} target
     * @param {{ root?: Element | null, rootMargin?: number, threshold?: number }} options The root, null for the viewport; the margin around it in pixels; and the threshold.
     * @param {IntersectionListener} listener
     */
    static observe(target, { root = null, rootMargin = 0, threshold = 0 }, listener) {
        const key = `${rootMargin}|${threshold}`;
        let pools = poolsByRoot.get(root);

        if (!pools) {
            pools = new Map();
            poolsByRoot.set(root, pools);
        }

        let pool = pools.get(key);

        if (!pool) {
            const listenersByTarget = new Map();
            const observer = new IntersectionObserver(
                entries => IntersectionObserverPool.#deliver(listenersByTarget, entries),
                { root, rootMargin: `${rootMargin}px`, threshold });
            pool = { observer, listenersByTarget };
            pools.set(key, pool);
        }

        let listeners = pool.listenersByTarget.get(target);

        if (!listeners) {
            listeners = new Set();
            pool.listenersByTarget.set(target, listeners);
            pool.observer.observe(target);
        }

        listeners.add(listener);
        let isDisposed = false;

        return {
            /** Has the observer report the target afresh, as it does when it starts observing it. */
            remeasure() {
                if (!isDisposed) {
                    pool.observer.unobserve(target);
                    pool.observer.observe(target);
                }
            },
            dispose() {
                if (isDisposed) {
                    return;
                }

                isDisposed = true;
                IntersectionObserverPool.#forget(root, key, pool, target, listener);
            },
        };
    }

    static #deliver(listenersByTarget, entries) {
        for (const entry of entries) {
            // A listener can dispose another, so each target's listeners are copied before any runs.
            for (const listener of [...(listenersByTarget.get(entry.target) ?? [])]) {
                listener(entry);
            }
        }
    }

    static #forget(root, key, pool, target, listener) {
        const listeners = pool.listenersByTarget.get(target);

        if (!listeners?.delete(listener) || listeners.size > 0) {
            return;
        }

        pool.listenersByTarget.delete(target);
        pool.observer.unobserve(target);

        if (pool.listenersByTarget.size > 0) {
            return;
        }

        pool.observer.disconnect();
        const pools = poolsByRoot.get(root);
        pools.delete(key);

        if (pools.size === 0) {
            poolsByRoot.delete(root);
        }
    }
}

/**
 * CapabilityCounts — the capabilities established on each element, counted across the owners that established them
 */

export class CapabilityCounts {
    /** @type {WeakMap<Element, Map<string, number>>} */
    #counts = new WeakMap();

    /**
     * The capabilities established on `target`.
     * @param {Element | null} target
     */
    capabilitiesOf(target) {
        return target instanceof Element ? [...(this.#counts.get(target)?.keys() ?? [])] : [];
    }

    /**
     * Counts one more establishment of `capability` on `target`.
     * @param {Element} target
     * @param {string} capability
     */
    add(target, capability) {
        this.#change(target, capability, 1);
    }

    /**
     * Counts one fewer establishment of `capability` on `target`.
     * @param {Element} target
     * @param {string} capability
     */
    remove(target, capability) {
        this.#change(target, capability, -1);
    }

    #change(target, capability, change) {
        const counts = this.#counts.get(target) ?? new Map();
        const count = (counts.get(capability) ?? 0) + change;

        if (count > 0) {
            counts.set(capability, count);
        } else {
            counts.delete(capability);
        }

        this.#counts.set(target, counts);
    }
}

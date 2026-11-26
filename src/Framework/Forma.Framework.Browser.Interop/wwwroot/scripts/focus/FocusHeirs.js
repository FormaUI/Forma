/**
 * FocusHeirs — the surfaces that hand on the focus of an element they took away, where a closing dialog or popover would
 * have handed it back to that element
 */

/** @typedef {{ dispose(): void }} FocusHeirRegistration */

export class FocusHeirs {
    /** Every surface registered, each answering for the elements it took away. */
    static #handOns = new Set();

    /**
     * Registers a surface that hands on the focus of the elements it takes away.
     * @param {(element: Element) => boolean} handOn Moves the focus to the heir of an element the surface took away, and
     * answers whether it did; an element the surface never held is left to the next.
     * @returns {FocusHeirRegistration}
     */
    static register(handOn) {
        FocusHeirs.#handOns.add(handOn);

        return { dispose: () => FocusHeirs.#handOns.delete(handOn) };
    }

    /**
     * Moves the focus to the heir of an element taken away, as the surface that took it names it, and answers whether one did.
     * @param {Element | null} element
     * @returns {boolean}
     */
    static handOn(element) {
        if (!element || element.isConnected) {
            return false;
        }

        for (const handOn of FocusHeirs.#handOns) {
            if (handOn(element)) {
                return true;
            }
        }

        return false;
    }
}

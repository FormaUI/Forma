/**
 * DomElements — an owner's DomElements, one for each native element it registers on
 */

import { DomElement } from './DomElement.mjs';

export class DomElements {
    /** @type {Map<Element, DomElement>} */
    #elements = new Map();

    /** The DomElement for the native element. */
    get(target) {
        let element = this.#elements.get(target);

        if (!element) {
            element = new DomElement(target, () => this.#elements.delete(target));
            this.#elements.set(target, element);
        }

        return element;
    }

    dispose() {
        // Disposing an element removes it from the map.
        for (const element of [...this.#elements.values()]) {
            element.dispose();
        }
    }
}

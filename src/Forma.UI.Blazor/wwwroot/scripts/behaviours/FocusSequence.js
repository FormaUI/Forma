/**
 * FocusSequence — Tab order inside one container decided by the container, not the document
 *
 * The regions named first are visited first, in the order given, and the rest of the
 * container follows in document order. Tab past the last control leaves for whatever
 * follows the container in the page, and Shift+Tab before the first leaves for whatever
 * precedes it, so the container is one stretch of the page's tab order with its own
 * arrangement inside.
 */

import { focusablesIn } from '../operations/Focus.js';

/**
 * @param {HTMLElement} target The container whose tab order is arranged.
 * @param {{ order: string[] }} options Selectors of the regions visited first, in that order.
 */
export function attach(target, options) {
    return new FocusSequence(target, options);
}

class FocusSequence {
    #target;
    #order;
    #controller = new AbortController();

    constructor(target, { order = [] } = {}) {
        this.#target = target;
        this.#order = order;

        target.addEventListener('keydown', event => this.#handle(event), { signal: this.#controller.signal });
    }

    dispose() {
        this.#controller.abort();
    }

    #handle(event) {
        // A surface inside the container that already answered Tab keeps its answer.
        if (event.key !== 'Tab' || event.defaultPrevented) {
            return;
        }

        const sequence = this.#sequence();
        const index = sequence.indexOf(document.activeElement);
        if (index < 0) {
            return;
        }

        const next = event.shiftKey ? index - 1 : index + 1;
        const destination = next < 0
            ? this.#neighbour(false)
            : next >= sequence.length
                ? this.#neighbour(true)
                : sequence[next];

        if (destination) {
            event.preventDefault();
            destination.focus();
        }
    }

    /** The container's controls in the arranged order: each named region's in turn, then the rest in document order. */
    #sequence() {
        const regions = this.#order.map(selector => [...this.#target.querySelectorAll(selector)]);
        const inRegion = control => regions.findIndex(elements => elements.some(region => region.contains(control)));
        const all = focusablesIn(this.#target);
        const grouped = regions.map(() => []);
        const rest = [];

        for (const control of all) {
            const region = inRegion(control);
            (region < 0 ? rest : grouped[region]).push(control);
        }

        return [...grouped.flat(), ...rest];
    }

    /** The first control after the container in the page, or the last before it. */
    #neighbour(forward) {
        const outside = focusablesIn(document).filter(control => !this.#target.contains(control));
        const following = control => this.#target.compareDocumentPosition(control) & Node.DOCUMENT_POSITION_FOLLOWING;

        return forward
            ? outside.find(following) ?? null
            : outside.filter(control => !following(control)).at(-1) ?? null;
    }
}

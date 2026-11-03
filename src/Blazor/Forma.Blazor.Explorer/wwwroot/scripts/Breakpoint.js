/**
 * Breakpoint — telling its owner which layout tier a stylesheet has put the target in
 *
 * A container query decides a tier in CSS and records it in a custom property. The target's
 * computed value of that property is read back each time the target is laid out again and
 * reported to the owner only when it changes.
 */

import { tell } from '../../forma/core/scripts/Owner.js';

/** @typedef {import('../../forma/core/scripts/Owner.js').Owner} Owner */

/**
 * @param {HTMLElement} target The element whose computed property names the tier.
 * @param {{ owner: Owner, property: string }} options
 *   The owner told the tier through HandleBreakpointAsync, and the custom property that names it.
 */
export function attach(target, options) {
    return new Breakpoint(target, options);
}

class Breakpoint {
    #target;
    #owner;
    #property;
    #observer;
    #last = null;

    constructor(target, { owner, property }) {
        this.#target = target;
        this.#owner = owner;
        this.#property = property;
        this.#observer = new ResizeObserver(() => this.#report());
        this.#observer.observe(target);
        this.#report();
    }

    dispose() {
        this.#observer.disconnect();
    }

    #report() {
        // A target taken out of the document has no tier.
        if (!this.#target.isConnected) {
            return;
        }

        const tier = getComputedStyle(this.#target).getPropertyValue(this.#property).trim();

        if (tier === this.#last) {
            return;
        }

        this.#last = tier;
        tell(this.#owner, 'HandleBreakpointAsync', tier);
    }
}

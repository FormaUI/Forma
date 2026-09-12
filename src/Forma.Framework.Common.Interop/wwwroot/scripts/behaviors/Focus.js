/**
 * Focus — where Tab goes inside one surface
 *
 * A surface decides its own Tab order. A wrapping surface, such as a menu or a selection
 * bar, keeps Tab and Shift+Tab among its own controls and wraps at either end, so the page
 * behind it is never tabbed into by accident. An arranged surface, such as a composer,
 * visits the regions named first in the order given and the rest of itself in document
 * order, then leaves for whatever follows or precedes it in the page.
 */

import { Controls } from '../../../core/scripts/Controls.js';

/**
 * @param {HTMLElement} target The surface whose Tab order is decided.
 * @param {{ wrap?: boolean, order?: string[] }} options Whether Tab wraps inside the surface, and the selectors of the regions visited first.
 */
export function attach(target, options = {}) {
    const controller = new AbortController();
    const focus = new Focus(target, options, controller.signal);

    return { focus, dispose: () => controller.abort() };
}

/**
 * The controls inside a root that Tab can reach, in document order, leaving out any that are not displayed.
 * @param {ParentNode} root
 * @returns {HTMLElement[]}
 */
export function focusablesIn(root) {
    return [...root.querySelectorAll(Controls.Focusable)].filter(isDisplayed);
}

/** Whether an element is drawn: neither it nor an ancestor is hidden through display or visibility. */
function isDisplayed(element) {
    if (typeof element.checkVisibility === 'function') {
        return element.checkVisibility({ visibilityProperty: true });
    }

    for (let node = element; node; node = node.parentElement) {
        const style = getComputedStyle(node);
        if (style.display === 'none' || style.visibility === 'hidden') {
            return false;
        }
    }

    return true;
}

export class Focus {
    #target;
    #wrap;
    #order;

    /**
     * @param {HTMLElement} target The surface whose Tab order is decided.
     * @param {{ wrap?: boolean, order?: string[] }} options
     * @param {AbortSignal} signal Ends the handling.
     */
    constructor(target, { wrap = false, order = [] } = {}, signal) {
        this.#target = target;
        this.#wrap = wrap;
        this.#order = order;

        target.addEventListener('keydown', event => this.#handle(event), { signal });
    }

    #handle(event) {
        // A surface inside this one that already answered Tab keeps its answer.
        if (event.key !== 'Tab' || event.defaultPrevented) {
            return;
        }

        const controls = this.#sequence();
        if (controls.length === 0) {
            return;
        }

        const index = controls.indexOf(document.activeElement);
        const step = event.shiftKey ? -1 : 1;
        const destination = this.#wrap
            ? controls[index < 0 ? (step > 0 ? 0 : controls.length - 1) : (index + step + controls.length) % controls.length]
            : index < 0 ? null : this.#within(controls, index + step);

        if (destination) {
            event.preventDefault();
            destination.focus();
        }
    }

    /** The control at a place in the sequence, or the page's neighbor past either end. */
    #within(controls, place) {
        if (place < 0) {
            return this.#neighbor(false);
        }

        return place >= controls.length ? this.#neighbor(true) : controls[place];
    }

    /** The surface's controls in its arranged order: each named region's in turn, then the rest in document order. */
    #sequence() {
        const all = focusablesIn(this.#target);
        if (this.#order.length === 0) {
            return all;
        }

        const regions = this.#order.map(selector => [...this.#target.querySelectorAll(selector)]);
        const grouped = regions.map(() => []);
        const rest = [];

        for (const control of all) {
            const region = regions.findIndex(elements => elements.some(element => element.contains(control)));
            (region < 0 ? rest : grouped[region]).push(control);
        }

        return [...grouped.flat(), ...rest];
    }

    /** The first control after the surface in the page, or the last before it. */
    #neighbor(forward) {
        const outside = focusablesIn(document).filter(control => !this.#target.contains(control));
        const follows = control => this.#target.compareDocumentPosition(control) & Node.DOCUMENT_POSITION_FOLLOWING;

        return forward
            ? outside.find(follows) ?? null
            : outside.filter(control => !follows(control)).at(-1) ?? null;
    }
}

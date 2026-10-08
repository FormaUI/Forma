/**
 * FocusSurface — an element whose controls the focus moves among
 */

import { Controls } from '../../../core/scripts/Controls.js';

export class FocusSurface {
    #element;
    #order;

    /**
     * @param {HTMLElement} element The surface.
     * @param {string[]} [order] The selectors of the regions whose controls come first, in turn.
     */
    constructor(element, order = []) {
        this.#element = element;
        this.#order = order;
    }

    /** The surface. */
    get element() {
        return this.#element;
    }

    /**
     * The displayed elements inside the surface that match the selector, in document order.
     * @param {string} [selector] The selector of the elements, Tab's reach by default.
     * @returns {HTMLElement[]}
     */
    controls(selector = Controls.Focusable) {
        return [...this.#element.querySelectorAll(selector)].filter(FocusSurface.#isDisplayed);
    }

    /**
     * The controls in the surface's arranged order: each named region's in turn, then the rest in document order.
     * @param {HTMLElement[]} controls
     */
    arrange(controls) {
        if (this.#order.length === 0) {
            return controls;
        }

        const regions = this.#order.map(selector => [...this.#element.querySelectorAll(selector)]);
        const grouped = regions.map(() => []);
        const rest = [];

        for (const control of controls) {
            const region = regions.findIndex(elements => elements.some(element => element.contains(control)));
            (region < 0 ? rest : grouped[region]).push(control);
        }

        return [...grouped.flat(), ...rest];
    }

    /** The first control after the surface in the page, or the last before it. */
    neighbor(forward) {
        const page = new FocusSurface(this.#element.ownerDocument.documentElement);
        const outside = page.controls().filter(control => !this.#element.contains(control));
        const follows = control => this.#element.compareDocumentPosition(control) & Node.DOCUMENT_POSITION_FOLLOWING;

        return forward
            ? outside.find(follows) ?? null
            : outside.filter(control => !follows(control)).at(-1) ?? null;
    }

    /** Whether an element is drawn: neither it nor an ancestor is hidden through display or visibility. */
    static #isDisplayed(element) {
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
}

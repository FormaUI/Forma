/**
 * ScrollInteraction — what moves a scroll container
 */

import { register } from '../../../input/scripts/InputRouter.js';
import { ResizeObserverPool } from '../ResizeObserverPool.mjs';
import { ScrollButtons } from './ScrollButtons.mjs';
import { ScrollContainer } from './ScrollContainer.mjs';

/** Pixels in one line of a wheel turn. */
const LineHeight = 16;

/**
 * @typedef {{
 *   axis?: 'inline' | 'block', stops?: string | null, frozenSelector?: string | null,
 *   wheel?: string | null,
 *   buttons?: { container: HTMLElement } & import('./ScrollButtons.mjs').ScrollButtonOptions | null,
 *   current?: string | null,
 * }} ScrollInteractionOptions
 *   The axis the container moves along, the selectors of its stops and frozen parts, the action a wheel turn
 *   is routed as, the step buttons, and the selector of the element it keeps in view.
 */

export class ScrollInteraction {
    #wheel = null;
    #buttons = null;
    #currentInView = null;

    /**
     * @param {HTMLElement} target The scroll container.
     * @param {ScrollInteractionOptions} options
     */
    constructor(target, { axis = 'inline', stops = null, frozenSelector = null, wheel = null, buttons = null, current = null } = {}) {
        const container = new ScrollContainer(target, axis);

        if (wheel) {
            this.#wheel = register(target, {
                routes: [{
                    action: wheel,
                    bindings: [{ type: 'wheel', axis: 'vertical', when: () => container.isScrollable }],
                    handle: event => container.moveBy(event.deltaMode === WheelEvent.DOM_DELTA_LINE ? event.deltaY * LineHeight : event.deltaY),
                }],
            });
        }

        if (buttons) {
            this.#buttons = new ScrollButtons(buttons.container, buttons, direction => container.step(direction, { stops, frozenSelector }));
        }

        if (current) {
            this.#currentInView = new CurrentInView(target, container, current, frozenSelector);
        }
    }

    dispose() {
        this.#wheel?.dispose();
        this.#buttons?.dispose();
        this.#currentInView?.dispose();
        this.#wheel = null;
        this.#buttons = null;
        this.#currentInView = null;
    }
}

/** Keeps the element a selector marks current in a container's view: as the container opens, as it resizes, and as another element becomes current. */
class CurrentInView {
    #target;
    #container;
    #selector;
    #frozenSelector;
    #currentElement = null;
    #targetSize;
    #mutations;

    /**
     * @param {HTMLElement} target The scroll container.
     * @param {ScrollContainer} container The container's movement.
     * @param {string} selector The selector of the current element.
     * @param {string | null} frozenSelector The selector of the container's frozen parts.
     */
    constructor(target, container, selector, frozenSelector) {
        this.#target = target;
        this.#container = container;
        this.#selector = selector;
        this.#frozenSelector = frozenSelector;

        // A container narrowed after it opens, as by an arrow appearing beside it, would otherwise cover the element again.
        this.#targetSize = ResizeObserverPool.observe(target, () => this.#reveal());
        this.#mutations = new MutationObserver(() => this.#revealChanged());
        this.#mutations.observe(target, { subtree: true, childList: true, attributes: true });
        this.#reveal();
    }

    dispose() {
        this.#targetSize.dispose();
        this.#mutations.disconnect();
        this.#currentElement = null;
    }

    // Only another element becoming current moves the content, so a reader who scrolled away keeps their place as the page redraws.
    #revealChanged() {
        if (this.#target.querySelector(this.#selector) !== this.#currentElement) {
            this.#reveal();
        }
    }

    #reveal() {
        this.#currentElement = this.#target.querySelector(this.#selector);

        if (this.#currentElement && this.#target.isConnected) {
            this.#container.reveal(this.#currentElement, { frozenSelector: this.#frozenSelector });
        }
    }
}

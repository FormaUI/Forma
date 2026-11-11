/**
 * ScrollInteraction — what moves a scroll container
 */

import { register } from '../../../input/scripts/InputRouter.js';
import { ScrollButtons } from './ScrollButtons.mjs';
import { ScrollContainer } from './ScrollContainer.mjs';

/** Pixels in one line of a wheel turn. */
const LineHeight = 16;

/**
 * @typedef {{
 *   axis?: 'inline' | 'block', stops?: string | null, frozenSelector?: string | null,
 *   wheel?: string | null,
 *   buttons?: { container: HTMLElement } & import('./ScrollButtons.mjs').ScrollButtonOptions | null,
 * }} ScrollInteractionOptions
 *   The axis the container moves along, the selectors of its stops and frozen parts, the action a wheel turn
 *   is routed as, and the step buttons.
 */

export class ScrollInteraction {
    #wheel = null;
    #buttons = null;

    /**
     * @param {HTMLElement} target The scroll container.
     * @param {ScrollInteractionOptions} options
     */
    constructor(target, { axis = 'inline', stops = null, frozenSelector = null, wheel = null, buttons = null } = {}) {
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
    }

    dispose() {
        this.#wheel?.dispose();
        this.#buttons?.dispose();
        this.#wheel = null;
        this.#buttons = null;
    }
}

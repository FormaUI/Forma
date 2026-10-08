/**
 * ScrollInteraction — what moves a scroll container while it is attached
 *
 * A vertical wheel turned over the container moves it along its axis at once, as a route on the input
 * router under the action the options name, answered while the container can scroll and consumed at
 * either end too, so the host can remap it. The step buttons step it. Both run in the browser.
 */

import { register } from '../../../input/scripts/InputRouter.js';
import { ScrollButtons } from './ScrollButtons.mjs';
import { ScrollContainer } from './ScrollContainer.mjs';

/** Pixels a line is taken to be, for a wheel that reports in lines. */
const LineHeight = 16;

/**
 * @typedef {{
 *   axis?: 'inline' | 'block', stops?: string | null, frozen?: string | null,
 *   wheel?: string | null,
 *   buttons?: { container: HTMLElement } & import('./ScrollButtons.mjs').ScrollButtonOptions | null,
 * }} ScrollInteractionOptions
 *   The axis the container moves along, the selectors of its stops and frozen parts a step draws up to, the
 *   action a wheel turn is routed as, and the step buttons.
 */

export class ScrollInteraction {
    #wheel = null;
    #buttons = null;

    /**
     * @param {HTMLElement} target The scroll container.
     * @param {ScrollInteractionOptions} options
     */
    constructor(target, { axis = 'inline', stops = null, frozen = null, wheel = null, buttons = null } = {}) {
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
            this.#buttons = new ScrollButtons(buttons.container, buttons, direction => container.step(direction, { stops, frozen }));
        }
    }

    dispose() {
        this.#wheel?.dispose();
        this.#buttons?.dispose();
        this.#wheel = null;
        this.#buttons = null;
    }
}

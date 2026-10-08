/**
 * FocusInteraction — where the focus goes inside a surface
 */

import { FocusRecovery } from './FocusRecovery.mjs';
import { FocusSurface } from './FocusSurface.mjs';
import { RovingFocus } from './RovingFocus.mjs';
import { TabOrder } from './TabOrder.mjs';

/**
 * @typedef {{ wrap?: boolean, order?: string[], roving?: 'horizontal' | 'vertical' | null, heir?: string | null }} FocusInteractionOptions
 *   Whether Tab wraps among the surface's controls, the selectors of the regions whose controls come first, the axis
 *   the arrow keys move one Tab stop along, and the attribute marking the element that takes the focus when the one
 *   holding it goes away.
 */

export class FocusInteraction {
    #parts = [];

    /**
     * @param {HTMLElement} target The surface.
     * @param {FocusInteractionOptions} options
     */
    constructor(target, { wrap = false, order = [], roving = null, heir = null } = {}) {
        const surface = new FocusSurface(target, order);

        if (roving) {
            this.#parts.push(new RovingFocus(surface, roving));
        } else if (wrap || order.length > 0) {
            this.#parts.push(new TabOrder(surface, { wrap }));
        }

        if (heir) {
            this.#parts.push(new FocusRecovery(target, heir));
        }
    }

    dispose() {
        for (const part of this.#parts) {
            part.dispose();
        }

        this.#parts = [];
    }
}

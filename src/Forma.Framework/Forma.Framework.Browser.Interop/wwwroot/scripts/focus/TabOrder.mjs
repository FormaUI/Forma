/**
 * TabOrder — where Tab and Shift+Tab send the focus inside a surface
 */

import { register } from '../../../input/scripts/InputRouter.js';

/** The actions Tab and Shift+Tab are routed as. */
const Actions = {
    next: 'focus.next',
    previous: 'focus.previous',
};

export class TabOrder {
    #surface;
    #wrap;
    #registration;

    /**
     * @param {import('./FocusSurface.mjs').FocusSurface} surface The surface.
     * @param {{ wrap?: boolean }} options Whether Tab wraps among the surface's controls.
     */
    constructor(surface, { wrap = false } = {}) {
        this.#surface = surface;
        this.#wrap = wrap;

        // Each handler checks its binding's predicate again, since a host's remap of the action replaces the binding.
        const route = (action, step) => ({
            action,
            bindings: [{ type: 'keydown', key: 'Tab', modifiers: { shift: step < 0 }, when: () => this.#destination(step) !== null }],
            handle: () => this.#destination(step)?.focus(),
        });

        this.#registration = register(surface.element, { routes: [route(Actions.next, 1), route(Actions.previous, -1)] });
    }

    dispose() {
        this.#registration.dispose();
    }

    /** The control Tab sends the focus to, a step either way from the focused one. */
    #destination(step) {
        const controls = this.#surface.arrange(this.#surface.controls());

        if (controls.length === 0) {
            return null;
        }

        const index = controls.indexOf(document.activeElement);

        if (this.#wrap) {
            return controls[index < 0 ? (step > 0 ? 0 : controls.length - 1) : (index + step + controls.length) % controls.length];
        }

        if (index < 0) {
            return null;
        }

        const place = index + step;

        if (place < 0 || place >= controls.length) {
            return this.#surface.neighbor(place >= 0);
        }

        return controls[place];
    }
}

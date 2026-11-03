/**
 * RovingFocus — one Tab stop for a surface's controls, moved among them by the arrow keys
 */

import { Controls } from '../../../core/scripts/Controls.js';
import { Direction } from '../../../core/scripts/Direction.js';
import { register } from '../../../input/scripts/InputRouter.js';

/** The actions the surface's keys are routed as. */
const Actions = {
    next: 'focus.next',
    previous: 'focus.previous',
    first: 'focus.first',
    last: 'focus.last',
};

/** A control parked outside the page's Tab order. */
const Parked = '[tabindex="-1"]';

export class RovingFocus {
    #surface;
    #registration;
    #mutations;
    #abort = new AbortController();
    /** Each control the surface has parked, with the tabindex it carried before. */
    #parked = new Map();

    /**
     * @param {import('./FocusSurface.mjs').FocusSurface} surface The surface.
     * @param {'horizontal' | 'vertical'} axis The axis the arrow keys move the stop along.
     */
    constructor(surface, axis) {
        this.#surface = surface;

        const target = surface.element;
        const along = axis === 'horizontal';
        const mirrored = along && Direction.isRightToLeft(target);
        const forward = along ? (mirrored ? 'ArrowLeft' : 'ArrowRight') : 'ArrowDown';
        const backward = along ? (mirrored ? 'ArrowRight' : 'ArrowLeft') : 'ArrowUp';

        // Each handler checks its binding's predicate again, since a host's remap of the action replaces the binding.
        const route = (action, key, place) => ({
            action,
            bindings: [{ type: 'keydown', key, when: () => this.#stop() !== null }],
            handle: () => this.#rove(place),
        });

        this.#registration = register(target, {
            routes: [
                route(Actions.next, forward, (index, count) => (index + 1) % count),
                route(Actions.previous, backward, (index, count) => (index + count - 1) % count),
                route(Actions.first, 'Home', () => 0),
                route(Actions.last, 'End', (index, count) => count - 1),
            ],
        });

        target.addEventListener('focusin', event => this.#park(event.target), { signal: this.#abort.signal });
        this.#mutations = new MutationObserver(() => this.#park(document.activeElement));
        this.#mutations.observe(target, { childList: true, subtree: true, attributeFilter: ['disabled', 'hidden', 'style', 'class'] });
        this.#park(document.activeElement);
    }

    /** Ends the roving and hands every parked control the tabindex it carried before. */
    dispose() {
        this.#registration.dispose();
        this.#abort.abort();
        this.#mutations.disconnect();

        for (const [control, tabindex] of this.#parked) {
            if (tabindex === null) {
                control.removeAttribute('tabindex');
            } else {
                control.setAttribute('tabindex', tabindex);
            }
        }

        this.#parked.clear();
    }

    /** The roved controls and the place of the one holding focus. */
    #stop() {
        const controls = this.#roved();
        const index = controls.indexOf(document.activeElement);

        return index < 0 ? null : { controls, index };
    }

    /** Moves the stop to the place the key names among the roved controls. */
    #rove(place) {
        const stop = this.#stop();

        if (stop) {
            stop.controls[place(stop.index, stop.controls.length)].focus();
        }
    }

    /** Leaves one control reachable by Tab and parks the rest: the focused one, else the one holding the stop, else the first. */
    #park(focused) {
        for (const control of this.#parked.keys()) {
            if (!control.isConnected) {
                this.#parked.delete(control);
            }
        }

        const controls = this.#roved();
        const current = controls.includes(focused)
            ? focused
            : controls.find(control => control.tabIndex === 0 && this.#parked.has(control)) ?? controls[0];

        for (const control of controls) {
            if (!this.#parked.has(control)) {
                this.#parked.set(control, control.getAttribute('tabindex'));
            }

            control.tabIndex = control === current ? 0 : -1;
        }
    }

    /** The controls the stop roves among, in the surface's arranged order: what Tab could reach and what this surface parked, enabled and outside any popover. */
    #roved() {
        return this.#surface.arrange(this.#surface.controls(`${Controls.Focusable}, ${Parked}`)
            .filter(control => (control.matches(Controls.Focusable) || this.#parked.has(control))
                && !control.matches(':disabled')
                && control.closest('[popover]') === null));
    }
}

/**
 * Focus — where Tab and the arrow keys go inside one surface
 *
 * A wrapping surface keeps Tab and Shift+Tab among its own controls and wraps at either
 * end. An arranged surface visits the regions named first in the order given and the rest
 * of itself in document order, then leaves for whatever follows or precedes it in the page.
 * A roving surface is one Tab stop: the arrow keys along its axis move among its controls
 * and wrap at either end, Home and End reach the ends, and the control focused last keeps
 * the stop while the rest are parked at tabindex -1.
 *
 * The keys are routes on the input router, declared on the surface; a key this surface has
 * nowhere to send matches no route and keeps its default.
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

/**
 * @param {HTMLElement} target The surface whose Tab order is decided.
 * @param {{ wrap?: boolean, order?: string[], roving?: 'horizontal' | 'vertical' | null }} options Whether Tab wraps inside the surface, the selectors of the regions visited first, and the axis the arrow keys rove along.
 */
export function attach(target, options = {}) {
    const controller = new AbortController();
    const focus = new Focus(target, options, controller.signal);

    return { focus, dispose: () => controller.abort() };
}

/**
 * The displayed controls inside a root that Tab can reach, in document order.
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

/** A control parked outside the page's Tab order. */
const Parked = '[tabindex="-1"]';

export class Focus {
    #target;
    #wrap;
    #order;
    #roving;
    /** Each control a roving surface has parked, with the tabindex it carried before. */
    #parked = new Map();

    /**
     * @param {HTMLElement} target The surface whose Tab order is decided.
     * @param {{ wrap?: boolean, order?: string[], roving?: 'horizontal' | 'vertical' | null }} options
     * @param {AbortSignal} signal Ends the handling.
     */
    constructor(target, { wrap = false, order = [], roving = null } = {}, signal) {
        this.#target = target;
        this.#wrap = wrap;
        this.#order = order;
        this.#roving = roving;

        const registration = register(target, { routes: roving ? this.#rovingRoutes() : this.#tabbingRoutes() });
        signal.addEventListener('abort', () => registration.dispose(), { once: true });

        if (roving) {
            target.addEventListener('focusin', event => this.#park(event.target), { signal });

            const observer = new MutationObserver(() => this.#park(document.activeElement));
            observer.observe(target, { childList: true, subtree: true, attributeFilter: ['disabled', 'hidden', 'style', 'class'] });
            signal.addEventListener('abort', () => this.#release(observer), { once: true });

            this.#park(document.activeElement);
        }
    }

    // Each handler checks its binding's predicate again, since a host's remap of the action replaces the binding.
    /** Tab and Shift+Tab as the surface's next and previous, matching only where the surface has a control to send focus to. */
    #tabbingRoutes() {
        const route = (action, shift) => ({
            action,
            bindings: [{ type: 'keydown', key: 'Tab', modifiers: { shift }, when: () => this.#destination(shift ? -1 : 1) !== null }],
            handle: () => this.#destination(shift ? -1 : 1)?.focus(),
        });

        return [route(Actions.next, false), route(Actions.previous, true)];
    }

    /** The keys along the axis as next and previous, and Home and End as the ends, matching only while one of the roved controls holds focus. */
    #rovingRoutes() {
        const along = this.#roving === 'horizontal';
        const mirrored = along && Direction.isRightToLeft(this.#target);
        const forward = along ? (mirrored ? 'ArrowLeft' : 'ArrowRight') : 'ArrowDown';
        const backward = along ? (mirrored ? 'ArrowRight' : 'ArrowLeft') : 'ArrowUp';
        const route = (action, key, place) => ({
            action,
            bindings: [{ type: 'keydown', key, when: () => this.#stop() !== null }],
            handle: () => this.#rove(place),
        });

        return [
            route(Actions.next, forward, (index, count) => (index + 1) % count),
            route(Actions.previous, backward, (index, count) => (index + count - 1) % count),
            route(Actions.first, 'Home', () => 0),
            route(Actions.last, 'End', (index, count) => count - 1),
        ];
    }

    /** The control Tab sends focus to, a step either way from the focused one: wrapping at the ends, or leaving for the page's neighbor. */
    #destination(step) {
        const controls = this.#sequence();

        if (controls.length === 0) {
            return null;
        }

        const index = controls.indexOf(document.activeElement);

        if (this.#wrap) {
            return controls[index < 0 ? (step > 0 ? 0 : controls.length - 1) : (index + step + controls.length) % controls.length];
        }

        return index < 0 ? null : this.#within(controls, index + step);
    }

    /** The roved controls and the place of the one holding focus. */
    #stop() {
        const controls = this.#sequence();
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

        const controls = this.#sequence();
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

    /** Ends the roving and hands every parked control the tabindex it carried before. */
    #release(observer) {
        observer.disconnect();

        for (const [control, tabindex] of this.#parked) {
            if (tabindex === null) {
                control.removeAttribute('tabindex');
            } else {
                control.setAttribute('tabindex', tabindex);
            }
        }

        this.#parked.clear();
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
        const all = this.#roving ? this.#roved() : focusablesIn(this.#target);
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

    /** The controls the stop roves among, in document order: what Tab could reach and what this surface parked, enabled, displayed, and outside any popover. */
    #roved() {
        return [...this.#target.querySelectorAll(`${Controls.Focusable}, ${Parked}`)]
            .filter(control => (control.matches(Controls.Focusable) || this.#parked.has(control))
                && !control.matches(':disabled')
                && control.closest('[popover]') === null
                && isDisplayed(control));
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

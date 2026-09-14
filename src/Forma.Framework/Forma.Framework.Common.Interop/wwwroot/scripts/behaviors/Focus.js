/**
 * Focus — where Tab and the arrow keys go inside one surface
 *
 * A surface decides its own Tab order. A wrapping surface, such as a menu or a selection
 * bar, keeps Tab and Shift+Tab among its own controls and wraps at either end, so the page
 * behind it is never tabbed into by accident. An arranged surface, such as a composer,
 * visits the regions named first in the order given and the rest of itself in document
 * order, then leaves for whatever follows or precedes it in the page. A roving surface,
 * such as a toolbar, is one Tab stop: the arrow keys along its axis move among its
 * controls and wrap at either end, Home and End reach the ends, and the control focused
 * last keeps the stop while the rest are parked at tabindex -1, so Tab leaves the surface
 * on its own and a surface around it counts the stop alone.
 */

import { Controls } from '../../../core/scripts/Controls.js';
import { Direction } from '../../../core/scripts/Direction.js';

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

        target.addEventListener('keydown', event => this.#handle(event), { signal });

        if (roving) {
            target.addEventListener('focusin', event => this.#park(event.target), { signal });

            const observer = new MutationObserver(() => this.#park(document.activeElement));
            observer.observe(target, { childList: true, subtree: true, attributeFilter: ['disabled', 'hidden', 'style', 'class'] });
            signal.addEventListener('abort', () => this.#release(observer), { once: true });

            this.#park(document.activeElement);
        }
    }

    #handle(event) {
        // A surface inside this one that already answered a key keeps its answer.
        if (event.defaultPrevented) {
            return;
        }

        if (this.#roving) {
            this.#rove(event);
            return;
        }

        if (event.key !== 'Tab') {
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

    /** Moves the stop for an axis key pressed on one of the roving controls; a key with a modifier, or one a text field owns, is left alone. */
    #rove(event) {
        const controls = this.#sequence();
        const index = controls.indexOf(document.activeElement);

        if (index < 0 || event.altKey || event.ctrlKey || event.metaKey || event.target.closest(Controls.TextEntry)) {
            return;
        }

        const place = this.#place(event.key, index, controls.length);
        if (place === null) {
            return;
        }

        event.preventDefault();
        controls[place].focus();
    }

    /** The place a key moves the stop to: one along the axis, wrapping at the ends, or either end; null for a key the surface does not own. */
    #place(key, index, count) {
        const beside = step => (index + step + count) % count;
        const along = this.#roving === 'horizontal';
        const mirrored = along && Direction.isRightToLeft(this.#target);

        switch (key) {
            case 'Home': return 0;
            case 'End': return count - 1;
            case 'ArrowRight': return along ? beside(mirrored ? -1 : 1) : null;
            case 'ArrowLeft': return along ? beside(mirrored ? 1 : -1) : null;
            case 'ArrowDown': return along ? null : beside(1);
            case 'ArrowUp': return along ? null : beside(-1);
            default: return null;
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

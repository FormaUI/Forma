/**
 * ScrollButtons — the buttons among a container's children that step a scroll target back and forward
 *
 * Each button present is a scope on the input router with a click route under the action its kind
 * names, so the button stays a native button: a click, Enter and Space each press it once, a disabled
 * one presses nothing, and the host can remap the action. A button that joins the container is routed
 * as it arrives, and one that leaves is released.
 */

import { register } from '../../../input/scripts/InputRouter.js';

/**
 * @typedef {{ back: string, forward: string, backAction: string, forwardAction: string }} ScrollButtonOptions
 *   The selectors of the back and forward buttons, and the actions each is routed as.
 */

export class ScrollButtons {
    #container;
    #kinds;
    #step;
    /** @type {Map<Element, { dispose(): void } | null>} */
    #registrations = new Map();
    #mutations;

    /**
     * @param {HTMLElement} container The element whose children the buttons are.
     * @param {ScrollButtonOptions} options
     * @param {(direction: -1 | 1) => void} step Takes one step toward the start, -1, or the end, 1.
     */
    constructor(container, { back, forward, backAction, forwardAction }, step) {
        this.#container = container;
        this.#step = step;
        this.#kinds = [
            { selector: back, action: backAction, direction: -1 },
            { selector: forward, action: forwardAction, direction: 1 },
        ];

        this.#mutations = new MutationObserver(() => this.#refresh());
        this.#mutations.observe(container, { childList: true });
        this.#refresh();
    }

    dispose() {
        this.#mutations.disconnect();

        for (const registration of this.#registrations.values()) {
            registration?.dispose();
        }

        this.#registrations.clear();
    }

    #refresh() {
        const presentButtons = new Map();

        for (const child of this.#container.children) {
            const kind = this.#kinds.find(candidate => child.matches(candidate.selector));

            if (kind) {
                presentButtons.set(child, kind);
            }
        }

        for (const [button, registration] of this.#registrations) {
            if (!presentButtons.has(button)) {
                registration?.dispose();
                this.#registrations.delete(button);
            }
        }

        for (const [button, kind] of presentButtons) {
            if (!this.#registrations.has(button)) {
                this.#registrations.set(button, register(button, {
                    routes: [{ action: kind.action, bindings: [{ type: 'click' }], handle: () => this.#press(button, kind.direction) }],
                }));
            }
        }
    }

    // The host's remap replaces the binding, so a disabled button is checked here rather than in a predicate.
    #press(button, direction) {
        if (!button.matches(':disabled')) {
            this.#step(direction);
        }
    }
}

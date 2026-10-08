/**
 * DomEngine — what an owner does to its elements
 */

import { RegistrationTable } from './RegistrationTable.mjs';

/** @typedef {import('./DomElements.mjs').DomElements} DomElements */
/** @typedef {{ token: string, generation: number, capability: string, target: Element, options: object }} Request */

/** The interaction each capability attaches. */
const Interactions = {
    scroll: () => import('./scrolling/ScrollInteraction.mjs').then(module => module.ScrollInteraction),
};

export class DomEngine {
    #elements;
    #registrations = new RegistrationTable();

    /**
     * @param {DomElements} elements The owner's DomElements.
     */
    constructor(elements) {
        this.#elements = elements;
    }

    /**
     * Attaches the interaction the request names, returning whether it attached.
     * @param {Request} request
     */
    async attach({ token, generation, capability, target, options }) {
        const load = Interactions[capability];

        if (!load) {
            throw new Error(`The capability '${capability}' has no interaction.`);
        }

        if (!this.#registrations.begin(token, generation)) {
            return false;
        }

        const Interaction = await load();

        if (!(target instanceof Element) || !this.#registrations.isCurrent(token, generation)) {
            return false;
        }

        const interaction = new Interaction(target, options);
        const hold = this.#elements.get(target).hold(() => this.#registrations.remove(token, generation));

        return this.#registrations.install(token, generation, {
            dispose() {
                hold.dispose();
                interaction.dispose();
            },
        });
    }

    /** Releases the interaction the token holds. */
    detach(token, generation) {
        this.#registrations.remove(token, generation);
    }

    dispose() {
        this.#registrations.dispose();
    }
}

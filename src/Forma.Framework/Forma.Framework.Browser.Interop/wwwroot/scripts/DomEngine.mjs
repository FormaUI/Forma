/**
 * DomEngine — what an owner does to its elements
 */

import { RegistrationTable } from './RegistrationTable.mjs';

/** @typedef {import('./DomElement.mjs').DomElement} DomElement */
/** @typedef {{ token: string, generation: number, capability: string, target: Element, options: object }} Request */

/** The interaction each capability attaches. */
const Interactions = {
    scroll: () => import('./scrolling/ScrollInteraction.mjs').then(module => module.ScrollInteraction),
};

export class DomEngine {
    #elementOf;
    #registrations = new RegistrationTable();

    /**
     * @param {(target: Element) => DomElement} elementOf The session's DomElement for a native element.
     */
    constructor(elementOf) {
        this.#elementOf = elementOf;
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
        const hold = this.#elementOf(target).hold(() => this.#registrations.remove(token, generation));

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

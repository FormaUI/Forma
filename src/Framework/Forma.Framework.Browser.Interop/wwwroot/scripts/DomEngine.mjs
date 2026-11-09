/**
 * DomEngine — what an owner does to its elements
 */

import { CapabilityCounts } from './CapabilityCounts.mjs';
import { RegistrationTable } from './RegistrationTable.mjs';

/** @typedef {import('./DomElements.mjs').DomElements} DomElements */
/** @typedef {import('./DomWatcher.mjs').Receiver} Receiver */
/** @typedef {{ token: string, generation: number, capability: string, target: Element, options: object }} Request */

// The capabilities whose interactions are installed on each element, across every owner.
const installedCapabilities = new CapabilityCounts();

/**
 * The capabilities whose interactions are installed on `target`, their listeners in place.
 * @param {Element | null} target
 */
export function describeInstalled(target) {
    return installedCapabilities.capabilitiesOf(target);
}

/** The interaction each capability attaches. */
const Interactions = {
    scroll: () => import('./scrolling/ScrollInteraction.mjs').then(module => module.ScrollInteraction),
    focus: () => import('./focus/FocusInteraction.mjs').then(module => module.FocusInteraction),
    edits: () => import('./text/TextEdits.mjs').then(module => module.TextEdits),
};

export class DomEngine {
    #runtimeKey;
    #elements;
    #registrations = new RegistrationTable();
    /** The interaction each token last held, which its next one goes on from however the last was released, until the owner's release. */
    #lastInteractions = new Map();

    /**
     * @param {DomElements} elements The owner's DomElements.
     * @param {string} runtimeKey The owner's runtime key.
     */
    constructor(elements, runtimeKey) {
        this.#elements = elements;
        this.#runtimeKey = runtimeKey;
    }

    /**
     * Attaches the interaction the request names, returning whether it attached.
     * @param {Request} request
     * @param {Receiver | null} receiver The owner's receiver, which an interaction reporting to its owner reports through.
     */
    async attach({ token, generation, capability, target, options }, receiver = null) {
        const load = Interactions[capability];

        if (!load) {
            throw new Error(`The capability '${capability}' has no interaction.`);
        }

        const predecessor = this.#lastInteractions.get(token) ?? null;

        if (!this.#registrations.begin(token, generation)) {
            return false;
        }

        const Interaction = await load();

        if (!(target instanceof Element) || !this.#registrations.isCurrent(token, generation)) {
            return false;
        }

        const interaction = new Interaction(target, options, {
            name: `owner ${this.#runtimeKey}, registration ${token}.${generation}`,
            predecessor,
            report: (method, ...args) => this.#report(receiver, method, token, args),
        });
        const hold = this.#elements.get(target).hold(() => this.#registrations.remove(token, generation));

        // Counted before the install, whose refusal releases the resource and with it the count.
        installedCapabilities.add(target, capability);
        const isInstalled = this.#registrations.install(token, generation, {
            interaction,
            dispose() {
                hold.dispose();
                interaction.dispose();
                installedCapabilities.remove(target, capability);
            },
        });

        if (isInstalled) {
            this.#lastInteractions.set(token, interaction);
        }

        return isInstalled;
    }

    /**
     * Runs one of the commands an interaction offers its owner, returning its result, or null where the generation is no longer the token's.
     * @param {string} token
     * @param {number} generation
     * @param {string} command
     * @param {unknown[]} args
     */
    invoke(token, generation, command, args) {
        const interaction = this.#registrations.isCurrent(token, generation) ? this.#registrations.get(token)?.interaction : null;

        if (!interaction) {
            return null;
        }

        if (!interaction.constructor.commands?.has(command)) {
            throw new Error(`The interaction of registration ${token} has no command '${command}'.`);
        }

        return interaction[command](...args);
    }

    /** Releases the interaction the token holds. */
    detach(token, generation) {
        this.#registrations.remove(token, generation);
    }

    dispose() {
        this.#registrations.dispose();
        this.#lastInteractions.clear();
    }

    // A receiver already released rejects rather than throwing into the interaction that reports.
    #report(receiver, method, token, args) {
        if (!receiver) {
            return Promise.reject(new Error(`Owner ${this.#runtimeKey} attached registration ${token} with no receiver to report to.`));
        }

        try {
            return receiver.invokeMethodAsync(method, token, ...args);
        } catch (error) {
            return Promise.reject(error);
        }
    }
}

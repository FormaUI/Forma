/**
 * DomEngine — what an owner does to its elements
 *
 * An engine acts through the owner's watcher: it holds the owner's interactions, each under the token
 * and generation its owner minted, on the watcher's DomElements, so an element that leaves the
 * document releases the interactions held on it too. It also runs the commands an owner issues. A
 * capability's modules load on its first interaction or command, before any handler it installs. The
 * functions exported below the class are the surface the owner's .NET side calls, one engine per owner.
 */

import { DomSessionRegistry } from './DomSessionRegistry.mjs';
import { DomWatcher } from './DomWatcher.js';
import { RegistrationTable } from './RegistrationTable.mjs';

/** @typedef {{ token: string, generation: number, capability: string, target: Element, options: object }} Request */

/** The interaction each capability attaches, loaded on its first. */
const Interactions = {
    scroll: () => import('./scrolling/ScrollInteraction.mjs').then(module => module.ScrollInteraction),
};

/** The commands each capability runs by name, each taking the target first, loaded on its first. */
const Commands = {
    scroll: () => import('./scrolling/ScrollContainer.mjs').then(({ ScrollContainer }) => ({
        step: (target, direction, { axis, ...options } = {}) => new ScrollContainer(target, axis).step(direction, options),
    })),
};

export class DomEngine {
    #watcher;
    #registrations = new RegistrationTable();

    /**
     * @param {DomWatcher} watcher The owner's watcher, whose elements the engine's interactions stand on.
     */
    constructor(watcher) {
        this.#watcher = watcher;
    }

    /** The number of interactions the engine holds. */
    get size() {
        return this.#registrations.size;
    }

    /**
     * Attaches the interaction the request names, replacing any the token holds; false when the target has
     * gone, or the request was overtaken while its capability loaded.
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
        const hold = this.#watcher.element(target).hold(() => this.#registrations.remove(token, generation));

        return this.#registrations.install(token, generation, {
            dispose() {
                hold.dispose();
                interaction.dispose();
            },
        });
    }

    /** Releases the interaction the token holds, and drops any earlier generation still loading. */
    detach(token, generation) {
        this.#registrations.remove(token, generation);
    }

    dispose() {
        this.#registrations.dispose();
    }
}

/**
 * Runs one of a capability's commands on the target and returns its result.
 * @param {string} capability
 * @param {string} command
 * @param {Element} target
 * @param {...unknown} args
 */
export async function run(capability, command, target, ...args) {
    const load = Commands[capability];

    if (!load) {
        throw new Error(`The capability '${capability}' has no commands.`);
    }

    const commands = await load();

    if (!Object.hasOwn(commands, command)) {
        throw new Error(`The capability '${capability}' has no command '${command}'.`);
    }

    return target instanceof Element ? commands[command](target, ...args) : false;
}

/** Attaches an interaction for the owner, making the owner's watcher and engine on its first. */
export function attach(runtimeKey, request) {
    const session = DomSessionRegistry.open(runtimeKey);
    session.watcher ??= new DomWatcher(runtimeKey);
    session.engine ??= new DomEngine(session.watcher);

    return session.engine.attach(request);
}

/** Releases one of the owner's interactions. */
export function detach(runtimeKey, token, generation) {
    DomSessionRegistry.find(runtimeKey)?.engine?.detach(token, generation);
}

/** Releases the owner's engine, leaving its watcher. */
export function releaseEngine(runtimeKey) {
    DomSessionRegistry.releaseEngine(runtimeKey);
}

/** Releases everything the owner holds, its engine before its watcher. */
export function release(runtimeKey) {
    DomSessionRegistry.release(runtimeKey);
}

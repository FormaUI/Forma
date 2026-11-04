/**
 * DomEngine — the surface an owner's .NET side calls to act on its elements
 */

import { DomEngine } from './DomEngine.mjs';
import { DomSessionRegistry } from './DomSessionRegistry.mjs';

/** @typedef {import('./DomEngine.mjs').Request} Request */
/** @typedef {import('./DomWatcher.mjs').Receiver} Receiver */

/** The commands each capability runs by name. */
const Commands = {
    scroll: () => import('./scrolling/ScrollContainer.mjs').then(({ ScrollContainer }) => ({
        step: (target, direction, { axis, ...options } = {}) => new ScrollContainer(target, axis).step(direction, options),
    })),
};

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

/**
 * Attaches an interaction for the owner, returning whether it attached.
 * @param {string} runtimeKey
 * @param {Receiver | null} receiver The owner's receiver, which an interaction reporting to its owner reports through.
 * @param {Request} request
 */
export function attach(runtimeKey, receiver, request) {
    return DomSessionRegistry.open(runtimeKey).openEngine(DomEngine).attach(request, receiver);
}

/**
 * Runs one of the commands the owner's interaction offers, returning its result, or null where the generation is no longer the token's.
 * @param {string} runtimeKey
 * @param {string} token
 * @param {number} generation
 * @param {string} command
 * @param {...unknown} args
 */
export function invoke(runtimeKey, token, generation, command, ...args) {
    return DomSessionRegistry.find(runtimeKey)?.engine?.invoke(token, generation, command, args) ?? null;
}

/** Releases one of the owner's interactions. */
export function detach(runtimeKey, token, generation) {
    DomSessionRegistry.find(runtimeKey)?.engine?.detach(token, generation);
}

/** Releases the owner's engine. */
export function releaseEngine(runtimeKey) {
    DomSessionRegistry.releaseEngine(runtimeKey);
}

/** Releases everything the owner holds. */
export function release(runtimeKey) {
    DomSessionRegistry.release(runtimeKey);
}

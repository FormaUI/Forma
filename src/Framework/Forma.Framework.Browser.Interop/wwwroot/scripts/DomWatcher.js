/**
 * DomWatcher — the surface an owner's .NET side calls to observe its elements
 */

import { DomSessionRegistry } from './DomSessionRegistry.mjs';
import { describeObserved } from './DomWatcher.mjs';

/** @typedef {import('./DomWatcher.mjs').Receiver} Receiver */
/** @typedef {import('./DomWatcher.mjs').Request} Request */

/**
 * Starts an observation for the owner and returns its first state.
 * @param {string} runtimeKey
 * @param {Receiver} receiver
 * @param {Request} request
 */
export function attach(runtimeKey, receiver, request) {
    return DomSessionRegistry.open(runtimeKey).openWatcher().watch(request, receiver);
}

/**
 * The observations made of `target`, by capability, once they deliver its changes.
 * @param {Element | null} target
 */
export function describe(target) {
    return { capabilities: describeObserved(target) };
}

/** Releases one of the owner's observations. */
export function detach(runtimeKey, token, generation) {
    DomSessionRegistry.find(runtimeKey)?.watcher?.unwatch(token, generation);
}

/** Resumes and measures afresh one of the owner's observations, returning whether the owner holds it. */
export function refresh(runtimeKey, token, generation) {
    return DomSessionRegistry.find(runtimeKey)?.watcher?.refresh(token, generation) ?? false;
}

/**
 * Resumes and measures afresh each observation named.
 * @param {Array<{ runtimeKey: string, registrations: Array<{ token: string, generation: number }> }>} owners
 */
export function refreshAll(owners) {
    for (const { runtimeKey, registrations } of owners) {
        const watcher = DomSessionRegistry.find(runtimeKey)?.watcher;

        for (const { token, generation } of registrations) {
            watcher?.refresh(token, generation);
        }
    }
}

/** Releases everything the owner holds. */
export function release(runtimeKey) {
    DomSessionRegistry.release(runtimeKey);
}

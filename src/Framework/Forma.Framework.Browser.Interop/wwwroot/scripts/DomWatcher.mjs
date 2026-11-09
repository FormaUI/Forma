/**
 * DomWatcher — what an owner observes of its elements
 */

import { Transfer } from '../../core/scripts/Transfer.js';
import { CapabilityCounts } from './CapabilityCounts.mjs';
import { RegistrationTable } from './RegistrationTable.mjs';
import { StateDelivery } from './StateDelivery.mjs';

/** @typedef {import('./DomElements.mjs').DomElements} DomElements */
/** @typedef {{ invokeMethodAsync: (name: string, ...args: unknown[]) => Promise<unknown> }} Receiver */
/** @typedef {{ token: string, generation: number, capability: string, target: Element, options: object }} Request */

// The capabilities observed on each element, across every owner.
const observedCapabilities = new CapabilityCounts();

/**
 * The capabilities observed on `target`, their observers in place and delivering.
 * @param {Element | null} target
 */
export function describeObserved(target) {
    return observedCapabilities.capabilitiesOf(target);
}

/** The observer each capability observes through. */
const Observers = {
    scroll: () => import('./scrolling/ScrollStateObserver.mjs').then(module => module.ScrollStateObserver),
    intersection: () => import('./scrolling/IntersectionStateObserver.mjs').then(module => module.IntersectionStateObserver),
    focus: () => import('./focus/FocusStateObserver.mjs').then(module => module.FocusStateObserver),
    selection: () => import('./text/TextSelectionObserver.mjs').then(module => module.TextSelectionObserver),
    autofill: () => import('./text/AutofillObserver.mjs').then(module => module.AutofillObserver),
};

export class DomWatcher {
    #runtimeKey;
    #elements;
    #registrations = new RegistrationTable();

    /**
     * @param {string} runtimeKey The owner's runtime key.
     * @param {DomElements} elements The owner's DomElements.
     */
    constructor(runtimeKey, elements) {
        this.#runtimeKey = runtimeKey;
        this.#elements = elements;
    }

    /**
     * Starts the observation the request names and returns its first state.
     * @param {Request} request
     * @param {Receiver} receiver The owner's receiver.
     */
    async watch({ token, generation, capability, target, options }, receiver) {
        const load = Observers[capability];

        if (!load) {
            throw new Error(`The capability '${capability}' cannot be observed.`);
        }

        if (!this.#registrations.begin(token, generation)) {
            return null;
        }

        const Observer = await load();

        if (!(target instanceof Element) || !this.#registrations.isCurrent(token, generation)) {
            return null;
        }

        const delivery = new StateDelivery(
            state => receiver.invokeMethodAsync('NotifyAsync', token, generation, Transfer.of(state)),
            error => console.error(`Forma: the ${capability} state of owner ${this.#runtimeKey}, registration ${token}.${generation}, could not be delivered; delivery waits for a refresh.`, error),
            { deliversEveryState: Observer.deliversEveryState === true });
        const observer = new Observer(target, options, state => delivery.offer(state));
        const hold = this.#elements.get(target).hold(() => this.#registrations.remove(token, generation));

        // Counted before the install, whose refusal releases the resource and with it the count, and in the task that opens the delivery.
        observedCapabilities.add(target, capability);
        this.#registrations.install(token, generation, {
            observer,
            delivery,
            dispose() {
                hold.dispose();
                observer.dispose();
                delivery.close();
                observedCapabilities.remove(target, capability);
            },
        });

        const baseline = observer.read();
        delivery.open();

        return baseline;
    }

    /** Releases the observation the token holds. */
    unwatch(token, generation) {
        this.#registrations.remove(token, generation);
    }

    /** Resumes and measures afresh the observation the token holds, returning whether it holds one. */
    refresh(token, generation) {
        const observation = this.#registrations.isCurrent(token, generation) ? this.#registrations.get(token) : null;

        if (!observation) {
            return false;
        }

        observation.observer.refresh();
        observation.delivery.resume();

        return true;
    }

    dispose() {
        this.#registrations.dispose();
    }
}

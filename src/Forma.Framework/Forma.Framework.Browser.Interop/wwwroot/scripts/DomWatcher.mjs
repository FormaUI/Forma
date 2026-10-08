/**
 * DomWatcher — what an owner observes of its elements
 */

import { RegistrationTable } from './RegistrationTable.mjs';
import { StateDelivery } from './StateDelivery.mjs';

/** @typedef {import('./DomElement.mjs').DomElement} DomElement */
/** @typedef {{ invokeMethodAsync: (name: string, ...args: unknown[]) => Promise<unknown> }} Receiver */
/** @typedef {{ token: string, generation: number, capability: string, target: Element, options: object }} Request */

/** The observer each capability observes through. */
const Observers = {
    scroll: () => import('./scrolling/ScrollStateObserver.mjs').then(module => module.ScrollStateObserver),
    intersection: () => import('./scrolling/IntersectionStateObserver.mjs').then(module => module.IntersectionStateObserver),
};

export class DomWatcher {
    #runtimeKey;
    #elementOf;
    #registrations = new RegistrationTable();

    /**
     * @param {string} runtimeKey The owner's runtime key.
     * @param {(target: Element) => DomElement} elementOf The session's DomElement for a native element.
     */
    constructor(runtimeKey, elementOf) {
        this.#runtimeKey = runtimeKey;
        this.#elementOf = elementOf;
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
            state => receiver.invokeMethodAsync('NotifyAsync', token, generation, state),
            error => console.error(`Forma: the ${capability} state of owner ${this.#runtimeKey}, registration ${token}.${generation}, could not be delivered; delivery waits for a refresh.`, error));
        const observer = new Observer(target, options, state => delivery.offer(state));
        const hold = this.#elementOf(target).hold(() => this.#registrations.remove(token, generation));

        this.#registrations.install(token, generation, {
            observer,
            delivery,
            dispose() {
                hold.dispose();
                observer.dispose();
                delivery.close();
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

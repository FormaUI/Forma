/**
 * DomWatcher — what an owner observes of its elements
 *
 * A watcher holds an owner's observations, each under the token and generation its owner minted, and
 * sends what each measures through the receiver its attach carried. A capability's observer loads on
 * its first observation. The watcher keeps one DomElement for each native element its owner registers
 * on, which the owner's engine shares, and an element that leaves the document releases what is held
 * on it. The functions exported below the class are the surface the owner's .NET side calls, one
 * watcher per owner.
 */

import { DomElement } from './DomElement.mjs';
import { DomSessionRegistry } from './DomSessionRegistry.mjs';
import { RegistrationTable } from './RegistrationTable.mjs';
import { StateDelivery } from './StateDelivery.mjs';

/** @typedef {{ invokeMethodAsync: (name: string, ...args: unknown[]) => Promise<unknown> }} Receiver */
/** @typedef {{ token: string, generation: number, capability: string, target: Element, options: object }} Request */

/** The observer each capability observes through, loaded on its first observation. */
const Observers = {
    scroll: () => import('./scrolling/ScrollStateObserver.mjs').then(module => module.ScrollStateObserver),
};

export class DomWatcher {
    #owner;
    #registrations = new RegistrationTable();
    /** @type {Map<Element, DomElement>} */
    #elements = new Map();

    /**
     * @param {string} owner The owner's runtime id, which failures are reported under.
     */
    constructor(owner) {
        this.#owner = owner;
    }

    /** The number of observations the watcher holds. */
    get size() {
        return this.#registrations.size;
    }

    /** The owner's DomElement for the native element, made on its first use. */
    element(target) {
        let element = this.#elements.get(target);

        if (!element) {
            element = new DomElement(target, () => this.#elements.delete(target));
            this.#elements.set(target, element);
        }

        return element;
    }

    /**
     * Starts the observation the request names, replacing any the token holds, and returns its first state;
     * null when the target has gone, or the request was overtaken while its capability loaded.
     * @param {Request} request
     * @param {Receiver} receiver The owner's receiver, which takes each state through NotifyAsync.
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
            error => console.error(`Forma: the ${capability} state of owner ${this.#owner}, registration ${token}.${generation}, could not be delivered; delivery waits for a refresh.`, error));
        const observer = new Observer(target, options, state => delivery.offer(state));
        const hold = this.element(target).hold(() => this.#registrations.remove(token, generation));

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

    /** Releases the observation the token holds, and drops any earlier generation still loading. */
    unwatch(token, generation) {
        this.#registrations.remove(token, generation);
    }

    /** Resumes the observation's delivery and measures it afresh; false when the token holds no observation. */
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

        for (const element of [...this.#elements.values()]) {
            element.dispose();
        }
    }
}

/**
 * Starts an observation for the owner, making the owner's watcher on its first.
 * @param {string} owner
 * @param {Receiver} receiver
 * @param {Request} request
 */
export function attach(owner, receiver, request) {
    const session = DomSessionRegistry.open(owner);
    session.watcher ??= new DomWatcher(owner);

    return session.watcher.watch(request, receiver);
}

/** Releases one of the owner's observations. */
export function detach(owner, token, generation) {
    DomSessionRegistry.find(owner)?.watcher?.unwatch(token, generation);
}

/** Resumes one of the owner's observations and measures it afresh. */
export function refresh(owner, token, generation) {
    return DomSessionRegistry.find(owner)?.watcher?.refresh(token, generation) ?? false;
}

/** Releases everything the owner holds. */
export function release(owner) {
    DomSessionRegistry.release(owner);
}

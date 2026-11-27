// The switch coordinator: the core every coordinator of a setting the whole page shares is built on, the culture's and the
// theme's among them. It holds the setting's accepted value with its number in the document's sequence, every runtime's
// registration, which receives each value accepted, and the order switches are accepted in, holding them through an
// enhanced navigation and setting the accepted value again after any other page update.

/** One setting's coordinator in the document. */
export class SwitchCoordinator {
    #write;
    #toArguments;
    #registrations = new Map();
    #queue = Promise.resolve();
    #hold = null;
    #isTracking = false;
    #isNavigationPending = false;

    /** The document's identity, which a version carries beside its number, once the coordinator has started. */
    documentKey = null;

    /** The value accepted last, with its number in the document's sequence: the page's own, number zero, until a switch. */
    accepted = null;

    /** Blazor's API, once the coordinator follows the page's updates. */
    blazor = null;

    /**
     * A coordinator that sets the document's attributes for an accepted value through `write`, and hands each registration's
     * receiver the arguments `toArguments` makes of a value, before the document's identity and the value's number.
     */
    constructor({ write, toArguments }) {
        this.#write = write;
        this.#toArguments = toArguments;
    }

    /** Whether a navigation holds the document. */
    get isHeld() {
        return this.#hold !== null;
    }

    /** Starts the document's sequence from the value the page started in. */
    start(value) {
        this.documentKey = crypto.randomUUID();
        this.accepted = { ...value, number: 0 };
    }

    /** The registration of the runtime state named `identity`, its live one where it has one, `create`d otherwise. */
    register(identity, create) {
        let registration = this.#registrations.get(identity);

        if (registration === undefined) {
            registration = create();
            this.#registrations.set(identity, registration);
        }

        return registration;
    }

    /** Whether `registration` is the live registration of the state named `identity`. */
    isLive(identity, registration) {
        return this.#registrations.get(identity) === registration;
    }

    /** Releases `registration`, where it is the live registration of the state named `identity`. */
    release(identity, registration) {
        if (this.isLive(identity, registration)) {
            this.#registrations.delete(identity);
        }
    }

    /** Releases every registration of `runtime`, leaving the document on the accepted value. */
    releaseRuntime(runtime) {
        for (const [identity, registration] of this.#registrations) {
            if (registration.runtime === runtime) {
                this.#registrations.delete(identity);
            }
        }
    }

    /**
     * Runs `work` once every request before it has settled and no navigation holds the document, completing as `work`
     * does; the next request waits for it to settle, whatever its outcome, which reaches its own requester.
     */
    enqueue(work) {
        const pendingWork = this.#queue.then(async () => {
            await this.#hold?.promise;

            return work();
        });

        this.#queue = pendingWork.then(() => {}, () => {});

        return pendingWork;
    }

    /**
     * Accepts `value` as the next switch: it takes the next number, the document takes it unless a navigation holds it, and
     * every registration but `excludedRegistrations`, which await their own delivery, receives it. A delivery that fails is
     * recovered as its runtime registers again, which adopts the accepted value.
     */
    accept(value, excludedRegistrations = []) {
        this.accepted = { ...value, number: this.accepted.number + 1 };

        if (this.#hold === null) {
            this.#write(this.accepted);
        }

        for (const registration of this.#registrations.values()) {
            if (!excludedRegistrations.includes(registration)) {
                this.deliver(registration, this.accepted).then(() => {}, () => {});
            }
        }

        return this.accepted;
    }

    /** Delivers an accepted `value` to `registration`'s state, completing once the state has applied it. */
    deliver(registration, value) {
        return registration.receiver.invokeMethodAsync('ApplyAsync', ...this.#toArguments(value), this.documentKey, value.number);
    }

    /**
     * Follows the document's enhanced page updates: each navigation holds the document until it lands or ends, `land` reads
     * a landing from the first update after a navigation's start, `end` follows a navigation that ends without landing, and
     * the accepted value is set again after every other update.
     */
    track(blazor, { land, end }) {
        if (this.#isTracking) {
            return;
        }

        this.#isTracking = true;
        this.blazor = blazor;

        blazor.addEventListener('enhancednavigationstart', () => {
            this.#isNavigationPending = true;
            this.#hold ??= new DocumentHold();
        });

        // A navigation's response patches the whole document, its head and `<html>` among it, before its first update is
        // announced; a streaming update patches a component's region alone, so only the first update after a start is read.
        blazor.addEventListener('enhancedload', () => {
            if (this.#isNavigationPending) {
                this.#isNavigationPending = false;
                land();
                this.endHold();
            }

            if (this.#hold === null && this.accepted !== null) {
                this.#write(this.accepted);
            }
        });

        blazor.addEventListener('enhancednavigationend', () => {
            if (this.#isNavigationPending) {
                this.#isNavigationPending = false;
                this.endHold();
                end();
            }
        });
    }

    /** Ends the document's hold, setting the accepted value again, and releases what waited for it. */
    endHold() {
        const endedHold = this.#hold;
        this.#hold = null;

        if (this.accepted !== null) {
            this.#write(this.accepted);
        }

        endedHold?.end();
    }
}

/** A runtime state's registration with a coordinator: its receiver, and the place of each request and landing it reports. */
export class SwitchRegistration {
    #coordinator;
    #identity;
    #runtime;
    #lastRequest = 0;
    #lastLanding = 0;
    receiver = null;

    constructor(coordinator, identity, runtime) {
        this.#coordinator = coordinator;
        this.#identity = identity;
        this.#runtime = runtime;
    }

    /** The runtime the state lives in: `server` for a circuit, `webassembly` for a WebAssembly runtime. */
    get runtime() {
        return this.#runtime;
    }

    /** Whether this is its state's live registration. */
    get isLive() {
        return this.#coordinator.isLive(this.#identity, this);
    }

    /** Takes the request at place `order` among this registration's, refusing one made before one already taken. */
    takeRequest(order) {
        if (order <= this.#lastRequest) {
            return false;
        }

        this.#lastRequest = order;

        return true;
    }

    /** Takes the landing at place `order` among this registration's reports, refusing one made before one already taken. */
    takeLanding(order) {
        if (order <= this.#lastLanding) {
            return false;
        }

        this.#lastLanding = order;

        return true;
    }

    /** Delivers an accepted value to this registration's state, completing once the state has applied it. */
    deliver(value) {
        return this.#coordinator.deliver(this, value);
    }

    /** Releases the registration, leaving the document on the accepted value. */
    dispose() {
        this.#coordinator.release(this.#identity, this);
    }
}

// The document held through one enhanced navigation, and what waits for the hold to end.
class DocumentHold {
    promise;
    #resolve;

    constructor() {
        this.promise = new Promise(resolve => this.#resolve = resolve);
    }

    end() {
        this.#resolve();
    }
}

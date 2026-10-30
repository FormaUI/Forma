// The document's culture coordinator: the pair the page accepted last with its version, every runtime's registration,
// which receives each pair accepted, and the document's language and direction, which follow the accepted pair.

const root = document.documentElement;

// What the coordinator read as the document loaded: the document's key and the switch's policy, or why it can't start.
let initialization = null;

// The pair accepted last, with its number in the document's sequence: the marker's pair, number zero, until a switch.
let accepted = null;

// What the server last served to the document's attributes.
let servedPair = readPair();

const registrations = new Map();

// Requests are accepted one at a time, in the order they reach the coordinator.
let queue = Promise.resolve();

// The hold an enhanced navigation places on the document, from its start until its landing or its end.
let hold = null;
let isTracking = false;
let isNavigationPending = false;

/** Reads the page's culture marker and the switch's policy from the document's head, once, as the document loads. */
export function initialize(head = document.head) {
    if (initialization !== null) {
        return;
    }

    const marker = readMarker(head);

    if (marker === null) {
        initialization = {
            error: new Error('The page carries no valid culture marker. A coordinated prerendered page places FormaPageHead in its head, which publishes the cultures the server rendered it in.'),
        };
        console.error(initialization.error);

        return;
    }

    initialization = { documentKey: crypto.randomUUID(), policy: readPolicy(head) };
    accepted = { ...marker, direction: servedPair.direction, number: 0 };
}

/** Registers the culture state named `identity` with the receiver every accepted pair is delivered to, recovering its live registration where it has one, and completes once the state has adopted the accepted pair. */
export async function register(identity, receiver, runtime = 'server') {
    const { error } = getInitialization();

    if (error) {
        throw error;
    }

    let registration = registrations.get(identity);

    if (registration === undefined) {
        registration = new CultureCoordinatorRegistration(identity, runtime);
        registrations.set(identity, registration);
    }

    registration.receiver = receiver;
    await registration.deliver(accepted);

    return registration;
}

/** Releases the registrations of the circuit the client closed, leaving the document on the accepted pair. */
export function releaseCircuit() {
    for (const [identity, registration] of registrations) {
        if (registration.runtime === 'server') {
            registrations.delete(identity);
        }
    }
}

/** Follows the document's enhanced page updates: each navigation holds the document until it lands, its landing is accepted from its response's marker, and the accepted pair is set again after each update. */
export function track(blazor) {
    if (isTracking) {
        return;
    }

    isTracking = true;

    blazor.addEventListener('enhancednavigationstart', () => {
        isNavigationPending = true;
        hold ??= new DocumentHold();
    });

    // A navigation's response patches the whole document, its head and `<html>` among it, before its first update is
    // announced; a streaming update patches a component's region alone, so only the first update after a start is read.
    blazor.addEventListener('enhancedload', () => {
        if (isNavigationPending) {
            isNavigationPending = false;
            servedPair = readPair();
            land(readMarker(document.head));
        }

        if (hold === null && accepted !== null) {
            writePair(accepted);
        }
    });

    // A navigation that ends without landing ends the hold, leaving the accepted pair.
    blazor.addEventListener('enhancednavigationend', () => {
        if (isNavigationPending) {
            isNavigationPending = false;
            endHold();
        }
    });
}

// A culture state's registration with the coordinator.
class CultureCoordinatorRegistration {
    #identity;
    #runtime;
    receiver = null;

    constructor(identity, runtime) {
        this.#identity = identity;
        this.#runtime = runtime;
    }

    get runtime() {
        return this.#runtime;
    }

    get #isLive() {
        return registrations.get(this.#identity) === this;
    }

    /** Requests a switch to `culture` and `uiCulture`, written `direction`, completing once the switch was accepted and this registration's state applied it. */
    request(culture, uiCulture, direction) {
        const pending = queue.then(async () => {
            await hold?.promise;

            if (!this.#isLive) {
                throw new Error('The culture state requesting the switch is no longer registered with the page.');
            }

            persist(culture, uiCulture);
            await this.deliver(accept({ culture, uiCulture, direction }, this));
        });

        // The next request waits for this one to settle, whatever its outcome, which reaches its own requester.
        queue = pending.then(() => {}, () => {});

        return pending;
    }

    /** Delivers an accepted pair to this registration's state, completing once the state has applied it. */
    deliver(pair) {
        return this.receiver.invokeMethodAsync('ApplyAsync', pair.culture, pair.uiCulture, initialization.documentKey, pair.number);
    }

    /** Releases the registration, leaving the document on the accepted pair. */
    dispose() {
        if (this.#isLive) {
            registrations.delete(this.#identity);
        }
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

function getInitialization() {
    if (initialization === null) {
        initialize();
    }

    return initialization;
}

// A landing is accepted as the next switch, unless it is the accepted pair already; a response without a valid marker
// leaves the accepted pair.
function land(marker) {
    if (marker !== null && accepted !== null && (marker.culture !== accepted.culture || marker.uiCulture !== accepted.uiCulture)) {
        accept({ ...marker, direction: servedPair.direction });
    }

    endHold();
}

// The accepted pair is delivered to every registration but the requester's, which awaits its own; a delivery that fails is
// recovered as its runtime registers again, which adopts the accepted pair.
function accept(pair, requester = null) {
    accepted = { ...pair, number: accepted.number + 1 };

    if (hold === null) {
        writePair(accepted);
    }

    for (const registration of registrations.values()) {
        if (registration !== requester) {
            registration.deliver(accepted).then(() => {}, () => {});
        }
    }

    return accepted;
}

function endHold() {
    const ended = hold;
    hold = null;

    if (accepted !== null) {
        writePair(accepted);
    }

    ended?.end();
}

// Under the cookie policy, the cookie is written in request localization's serialization and read back before the
// switch is accepted, so it names the last pair accepted.
function persist(culture, uiCulture) {
    const policy = initialization.policy;

    if (policy.kind !== 'cookie') {
        return;
    }

    const value = `c=${culture}|uic=${uiCulture}`;
    document.cookie = serializeCookie(policy, value);

    if (readCookie(policy.name) !== value) {
        throw new Error(`The culture cookie ${policy.name} couldn't be read back, so the switch wasn't accepted.`);
    }
}

function serializeCookie(policy, value) {
    const attributes = [`${encodeURIComponent(policy.name)}=${encodeURIComponent(value)}`, `path=${policy.path}`];

    if (policy.domain !== null) {
        attributes.push(`domain=${policy.domain}`);
    }

    if (policy.maxAge !== null) {
        attributes.push(`max-age=${policy.maxAge}`);
    }

    attributes.push(`samesite=${policy.sameSite}`);

    if (policy.isSecure) {
        attributes.push('secure');
    }

    return attributes.join('; ');
}

function readCookie(name) {
    const prefix = `${encodeURIComponent(name)}=`;
    const entry = document.cookie.split('; ').find(cookie => cookie.startsWith(prefix));

    return entry === undefined ? null : decodeURIComponent(entry.slice(prefix.length));
}

function readMarker(head) {
    const marker = head?.querySelector('meta[name="forma-culture"]') ?? null;

    if (marker === null) {
        return null;
    }

    // Each name is read on its own, and a missing attribute reads as null, which names no culture.
    const culture = marker.getAttribute('data-culture');
    const uiCulture = marker.getAttribute('data-ui-culture');

    return isCultureName(culture) && isCultureName(uiCulture) ? { culture, uiCulture } : null;
}

function readPolicy(head) {
    const policy = head?.querySelector('meta[name="forma-culture-policy"]');
    const kind = policy?.getAttribute('content') ?? 'state';

    if (kind !== 'cookie') {
        return { kind };
    }

    const maxAge = policy.getAttribute('data-max-age');

    return {
        kind,
        name: policy.getAttribute('data-name'),
        path: policy.getAttribute('data-path') ?? '/',
        domain: policy.getAttribute('data-domain'),
        maxAge: maxAge === null ? null : Number(maxAge),
        isSecure: policy.hasAttribute('data-secure'),
        sameSite: policy.getAttribute('data-same-site') ?? 'lax',
    };
}

// The marker names the invariant culture by the empty name; any other name has to read as a language tag, a check of its
// syntax alone, never of whether the browser supports the culture.
function isCultureName(name) {
    if (name === null) {
        return false;
    }

    if (name === '') {
        return true;
    }

    try {
        return Intl.getCanonicalLocales(name).length === 1;
    } catch {
        return false;
    }
}

function readPair() {
    return { uiCulture: root.getAttribute('lang'), direction: root.getAttribute('dir') };
}

function writePair(pair) {
    writeAttribute('lang', pair.uiCulture);
    writeAttribute('dir', pair.direction);
}

function writeAttribute(name, value) {
    if (value === null) {
        root.removeAttribute(name);
    } else if (root.getAttribute(name) !== value) {
        root.setAttribute(name, value);
    }
}

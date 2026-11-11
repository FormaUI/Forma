// The document's culture coordinator: the pair the page accepted last with its version, every runtime's registration,
// which receives each pair accepted, and the document's language and direction, which follow the accepted pair.

const root = document.documentElement;

// A year, in seconds: how long the browser keeps the cookie naming the culture route last chosen.
const routeCookieMaxAge = 31536000;

// What the coordinator read as the document loaded: the document's key and the switch's policy, or why it can't start.
let initialization = null;

// The pair accepted last, with its number in the document's sequence: the marker's pair, number zero, until a switch.
let acceptedPair = null;

// What the server last served to the document's attributes.
let servedPair = readPair();

const registrations = new Map();

// Requests are accepted one at a time, in the order they reach the coordinator.
let queue = Promise.resolve();

// The hold an enhanced navigation places on the document, from its start until its landing or its end.
let hold = null;
let isTracking = false;
let isNavigationPending = false;

// Blazor's API, which an in-place switch navigates through.
let blazorApi = null;

// The switch by address waiting for its navigation's landing, and the pairs earlier switches navigated to that haven't
// landed: one of their late landings is the page's culture, never the waiting switch's.
let pendingSwitch = null;
let earlierPairs = [];

// The history entry state of an address a following document replaces to follow its application's culture, which tells
// a navigation handler that the page and the user's work stay.
export const FollowMark = 'forma-culture';

// What a following document knows of its own landings: the addresses its replacements went to that haven't landed, in
// the order it made them, the address the last traversal arrived at, and whether it has landed at all.
let replacements = [];
let traversalAddress = null;
let hasLanded = false;

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
    acceptedPair = { ...marker, direction: servedPair.direction, number: 0 };

    // A traversal is told by the document's own history, since the location's change reports one as any other; a landing
    // is the traversal's only where it lands at the address the traversal arrived at, which a refused one never leaves.
    if (initialization.policy.follows) {
        addEventListener('popstate', () => traversalAddress = withoutHash(location.href));
    }
}

/**
 * Registers the culture state named `identity` with the receiver every accepted pair is delivered to, recovering its live
 * registration where it has one, and completes once the state has adopted the accepted pair. On a page the document's
 * load didn't initialize the coordinator on, a standalone app's, `start` starts it.
 */
export async function register(identity, receiver, runtime = 'server', start = null) {
    const { error } = getInitialization(start);

    if (error) {
        throw error;
    }

    let registration = registrations.get(identity);

    if (registration === undefined) {
        registration = new CultureCoordinatorRegistration(identity, runtime);
        registrations.set(identity, registration);
    }

    registration.receiver = receiver;
    await registration.deliver(acceptedPair);

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
    blazorApi = blazor;

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

        if (hold === null && acceptedPair !== null) {
            writePair(acceptedPair);
        }
    });

    // A navigation that ends without landing ends the hold, leaving the accepted pair, and leaves a switch waiting on it
    // canceled.
    blazor.addEventListener('enhancednavigationend', () => {
        if (isNavigationPending) {
            isNavigationPending = false;
            endHold();
            cancelPendingSwitch();
        }
    });
}

// A culture state's registration with the coordinator.
class CultureCoordinatorRegistration {
    #identity;
    #runtime;
    #lastLanding = 0;
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

    /**
     * Requests a switch to `culture` and `uiCulture`, written `direction`, completing with `accepted` once the switch was
     * accepted and this registration's state applied it, or with `canceled` where its navigation didn't land. Under the
     * address policy a pair no route carries is refused before anything changes.
     */
    request(culture, uiCulture, direction) {
        const policy = getInitialization().policy;
        const route = policy?.kind === 'address' ? findRouteByPair(policy, culture, uiCulture) : null;

        if (policy?.kind === 'address' && route === null) {
            return Promise.reject(new Error(`No configured culture route represents the culture ${culture} with the UI culture ${uiCulture}, so the switch can't be made by address.`));
        }

        const pendingWork = queue.then(async () => {
            await hold?.promise;

            if (!this.#isLive) {
                throw new Error('The culture state requesting the switch is no longer registered with the page.');
            }

            if (route !== null) {
                return { landing: navigate(policy, route, this) };
            }

            persist(culture, uiCulture);
            await this.deliver(accept({ culture, uiCulture, direction }, [this]));

            return { landing: Promise.resolve('accepted') };
        });

        // The next request waits for this one to settle, whatever its outcome, which reaches its own requester; a switch by
        // address settles once its navigation is under way, so a newer one supersedes it rather than waiting for its landing.
        queue = pendingWork.then(() => {}, () => {});

        return pendingWork.then(({ landing }) => landing);
    }

    /**
     * Accepts the interactive landing a culture route page reports at the route `token` names, or at a plain address where
     * it is null, completing once this registration's state applied the page's pair. A report this registration made
     * before one already received is obsolete and accepts nothing; interactive routing without culture routes records
     * nothing.
     */
    async reportLanding(token, order) {
        if (order <= this.#lastLanding) {
            return;
        }

        this.#lastLanding = order;
        const policy = getInitialization().policy;
        const route = policy?.kind === 'address' ? findRouteByToken(policy, token) : null;

        if (route === null) {
            return;
        }

        if (policy.follows) {
            await this.#landFollowing(policy, toPair(route));

            return;
        }

        // No request carries an interactive landing to the server, so the browser names the route last chosen; a web view's
        // document has no server to name it to.
        if (!policy.isWebView) {
            writeRouteCookie(policy, route.token);
        }

        await acceptLanding(toPair(route), this);
    }

    // A following document's landing, which leaves its history entry unmarked: one its own replacement made requests
    // nothing; a traversal and its first landing take the application's culture, replacing their address where it is
    // another culture's; a navigation the page made to another culture's address is accepted, and its delivery requests
    // the switch from the application's coordinator. A landing at one replacement's address passes over every replacement
    // made before it, and a landing elsewhere over them all, since a later navigation supersedes them.
    async #landFollowing(policy, pair) {
        const replacementIndex = replacements.findIndex(destination => isSameAddress(destination, location.href));
        const isAdopting = (traversalAddress !== null && isSameAddress(traversalAddress, location.href)) || !hasLanded;
        replacements = replacementIndex >= 0 ? replacements.slice(replacementIndex + 1) : [];
        traversalAddress = null;
        hasLanded = true;
        unmarkHistoryEntry();

        if (replacementIndex >= 0 || isSamePair(acceptedPair, pair)) {
            return;
        }

        if (isAdopting) {
            readdress(policy, acceptedPair);

            return;
        }

        await acceptLanding(pair, this);
    }

    /**
     * Follows the pair `culture` and `uiCulture`, written `direction`, the application's coordinator accepted, on a page
     * whose document follows an application's coordinator: the document takes its language and direction, and under the
     * address policy the page's address moves into the pair's route.
     */
    follow(culture, uiCulture, direction) {
        const policy = getInitialization().policy;
        const pair = { culture, uiCulture, direction };

        if (!isSamePair(acceptedPair, pair)) {
            accept(pair, [this]);
        }

        if (policy?.kind === 'address') {
            readdress(policy, pair);
        }
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

function getInitialization(start = null) {
    if (initialization === null) {
        if (start === null || document.head.querySelector('meta[name="forma-culture"]') !== null) {
            initialize();
        } else {
            startPage(start);
        }
    }

    return initialization;
}

// A page no server published a marker in publishes the marker and policy its runtime supplies, as a server publishes them,
// and takes the marker's pair; its coordinator then starts from them as any page's does. A web view's page carries culture
// addresses as a server's does, since its application gives each culture its base.
function startPage(start, head = document.head) {
    if (start.policy.content === 'address' && !('data-web-view' in start.policy)) {
        initialization = {
            error: new Error('A page without a server has no base for each culture its addresses carry, so a standalone app switches its culture state alone or through a cookie rather than through culture addresses.'),
        };
        console.error(initialization.error);

        return;
    }

    head.append(createMeta('forma-culture', start.marker), createMeta('forma-culture-policy', start.policy));
    writePair({ uiCulture: start.marker['data-ui-culture'] ?? null, direction: start.direction });
    servedPair = readPair();
    initialize(head);
}

function createMeta(name, attributes) {
    const meta = document.createElement('meta');
    meta.name = name;

    for (const [attribute, value] of Object.entries(attributes)) {
        meta.setAttribute(attribute, value);
    }

    return meta;
}

// A server-rendered landing takes its pair from its own response's marker; a response without a valid marker leaves the
// accepted pair.
function land(marker) {
    if (marker !== null && acceptedPair !== null) {
        acceptLanding({ ...marker, direction: servedPair.direction }).then(() => {}, () => {});
    } else {
        cancelPendingSwitch();
    }

    endHold();
}

// A landing is accepted as the next switch, without waiting for any registration, unless it is the accepted pair
// already. The registration that reported it, and the requester of a switch waiting on it, await their own delivery; a
// switch waiting on a landing at another pair was replaced by it, and is canceled, unless an earlier switch navigated
// there, whose late landing leaves the waiting switch waiting.
async function acceptLanding(pair, reporter = null) {
    const earlierIndex = earlierPairs.findIndex(earlier => isSamePair(pair, earlier));

    if (earlierIndex >= 0) {
        earlierPairs.splice(earlierIndex, 1);
    }

    const isEarlier = earlierIndex >= 0 && pendingSwitch !== null && !isSamePair(pair, pendingSwitch.route);
    const waitingSwitch = isEarlier ? null : takePendingSwitch();

    // A navigation the waiting switch made lands after every earlier one, so none of theirs can land after it.
    if (waitingSwitch !== null && isSamePair(pair, waitingSwitch.route)) {
        earlierPairs = [];
    }

    const awaitedRegistrations = [reporter, waitingSwitch?.requester].filter(registration => registration != null);
    const isNew = pair.culture !== acceptedPair.culture || pair.uiCulture !== acceptedPair.uiCulture;
    const currentPair = isNew ? accept(pair, awaitedRegistrations) : acceptedPair;
    const deliveries = isNew ? new Map([...new Set(awaitedRegistrations)].map(registration => [registration, registration.deliver(currentPair)])) : new Map();

    if (waitingSwitch !== null) {
        if (isSamePair(pair, waitingSwitch.route)) {
            deliveries.get(waitingSwitch.requester)?.then(() => waitingSwitch.resolve('accepted'), waitingSwitch.reject) ?? waitingSwitch.resolve('accepted');
        } else {
            waitingSwitch.resolve('canceled');
        }
    }

    if (reporter !== null) {
        await deliveries.get(reporter);
    }
}

// The accepted pair is delivered to every registration but those that await their own; a delivery that fails is
// recovered as its runtime registers again, which adopts the accepted pair.
function accept(pair, excludedRegistrations = []) {
    acceptedPair = { ...pair, number: acceptedPair.number + 1 };

    if (hold === null) {
        writePair(acceptedPair);
    }

    for (const registration of registrations.values()) {
        if (!excludedRegistrations.includes(registration)) {
            registration.deliver(acceptedPair).then(() => {}, () => {});
        }
    }

    return acceptedPair;
}

// A switch by address navigates to the current page's address in its route: a full load leaves the document, and an
// in-place navigation completes once its landing is accepted, or as timed out where none arrives within the policy's
// timeout, as one a navigation handler refused doesn't; a newer switch supersedes it, which completes it as canceled.
function navigate(policy, route, requester) {
    const destination = buildDestination(policy, route, location.href);

    if (isSamePair(acceptedPair, route) && isSameAddress(destination, location.href)) {
        return Promise.resolve('accepted');
    }

    if (!policy.switchesInPlace) {
        location.assign(destination);

        return new Promise(() => {});
    }

    return new Promise((resolve, reject) => {
        const superseded = takePendingSwitch();

        if (superseded !== null) {
            earlierPairs.push(superseded.route);
            superseded.resolve('canceled');
        }

        const attempt = { route, requester, resolve, reject, timeout: 0 };
        attempt.timeout = setTimeout(() => timeOut(attempt), policy.switchTimeout);
        pendingSwitch = attempt;
        (blazorApi ?? window.Blazor).navigateTo(destination);
    });
}

// A switch whose navigation didn't land in time completes as timed out; its landing, should it arrive, is still the page's.
function timeOut(attempt) {
    if (pendingSwitch !== attempt) {
        return;
    }

    takePendingSwitch();
    earlierPairs.push(attempt.route);
    attempt.resolve('timed-out');
}

// The switch waiting for its landing, no longer waiting.
function takePendingSwitch() {
    const waitingSwitch = pendingSwitch;
    pendingSwitch = null;

    if (waitingSwitch !== null) {
        clearTimeout(waitingSwitch.timeout);
    }

    return waitingSwitch;
}

// A following document moves its page into the route carrying `pair`, keeping its page, query and fragment, in place of the
// current history entry and under the mark a navigation handler reads.
function readdress(policy, pair) {
    const route = findRouteByPair(policy, pair.culture, pair.uiCulture);

    if (route === null) {
        return;
    }

    const destination = buildDestination(policy, route, location.href);

    if (isSameAddress(destination, location.href)) {
        return;
    }

    replacements.push(destination);
    (blazorApi ?? window.Blazor).navigateTo(`${destination}${location.hash}`, { replaceHistoryEntry: true, historyEntryState: FollowMark });
}

// The entry a replacement landed at keeps its page, so a traversal back to it is an ordinary one, which a navigation
// handler may hold for the user's work; Blazor's own index on the entry stays.
function unmarkHistoryEntry() {
    if (history.state?.userState === FollowMark) {
        history.replaceState({ ...history.state, userState: null }, '');
    }
}

function withoutHash(href) {
    const address = new URL(href);
    address.hash = '';

    return address.href;
}

function cancelPendingSwitch() {
    takePendingSwitch()?.resolve('canceled');
}

function endHold() {
    const endedHold = hold;
    hold = null;

    if (acceptedPair !== null) {
        writePair(acceptedPair);
    }

    endedHold?.end();
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

// The cookie the server reads for a request whose address names no culture, its name carrying the application's path
// base already escaped.
function writeRouteCookie(policy, token) {
    const attributes = [`${policy.cookieName}=${encodeURIComponent(token)}`, `path=${policy.cookiePath}`, `max-age=${routeCookieMaxAge}`, 'samesite=lax'];

    if (location.protocol === 'https:') {
        attributes.push('secure');
    }

    document.cookie = attributes.join('; ');
}

function readCookie(name) {
    const prefix = `${encodeURIComponent(name)}=`;
    const entry = document.cookie.split('; ').find(cookie => cookie.startsWith(prefix));

    return entry === undefined ? null : decodeURIComponent(entry.slice(prefix.length));
}

/**
 * The address `href` carries in `route`'s culture, as the server builds it: the address relative to the application's
 * root, its culture segment replaced by the route's token, or left without one for the default route where the default
 * carries none, its query kept and its fragment dropped. An address outside the root is the root's.
 */
export function buildDestination(policy, route, href) {
    const url = new URL(href);
    const rootPath = policy.root;
    const rootAddress = rootPath.slice(0, -1);
    let relativePath = '';

    if (url.pathname.toLowerCase().startsWith(rootPath.toLowerCase())) {
        relativePath = url.pathname.slice(rootPath.length);
    } else if (url.pathname.toLowerCase() !== rootAddress.toLowerCase()) {
        url.search = '';
    }

    // The culture segment the path opens with is replaced; a path without one is kept as written.
    let path = `/${relativePath}`;
    const segments = path.split('/').filter(segment => segment !== '');

    if (segments.length > 0 && findRouteByToken(policy, decodeSegment(segments[0]), false) !== null) {
        path = segments.length === 1 ? '/' : `/${segments.slice(1).join('/')}`;
    }

    const token = escapeData(route.token);
    const isUnprefixed = route.token === policy.defaultToken && !policy.prefixesDefault;
    const prefixedPath = isUnprefixed ? path : path === '/' ? `/${token}` : `/${token}${path}`;

    return `${url.origin}${rootPath}${prefixedPath.replace(/^\/+/, '')}${url.search}`;
}

// Escapes as .NET's Uri.EscapeDataString does, every character but the unreserved ones.
function escapeData(value) {
    return encodeURIComponent(value).replace(/[!'()*]/g, character => `%${character.charCodeAt(0).toString(16).toUpperCase()}`);
}

/** The route a token or alias names, in any case, or the default route for a plain address, where `token` is null. */
export function findRouteByToken(policy, token, defaultsToPlain = true) {
    if (token === null || token === undefined) {
        return defaultsToPlain ? policy.routes.find(route => route.token === policy.defaultToken) ?? null : null;
    }

    const name = token.toLowerCase();

    return policy.routes.find(route => route.token.toLowerCase() === name || route.aliases.some(alias => alias.toLowerCase() === name)) ?? null;
}

// A segment whose escapes don't decode is compared as written, which names no token.
function decodeSegment(segment) {
    try {
        return decodeURIComponent(segment);
    } catch {
        return segment;
    }
}

function findRouteByPair(policy, culture, uiCulture) {
    return policy.routes.find(route => isSamePair({ culture, uiCulture }, route)) ?? null;
}

function toPair(route) {
    return { culture: route.culture, uiCulture: route.uiCulture, direction: route.direction };
}

function isSamePair(pair, other) {
    return pair.culture.toLowerCase() === other.culture.toLowerCase() && pair.uiCulture.toLowerCase() === other.uiCulture.toLowerCase();
}

function isSameAddress(destination, href) {
    return destination === withoutHash(href);
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

// The milliseconds a switch in place waits for its landing, 30 seconds where the page published none a timer can hold.
function readSwitchTimeout(published) {
    const timeout = Number(published);

    return published !== null && Number.isInteger(timeout) && timeout > 0 && timeout <= 2147483647 ? timeout : 30000;
}

/** The switch's policy the page published in `head`, the state alone where it published none. */
export function readPolicy(head) {
    const policy = head?.querySelector('meta[name="forma-culture-policy"]');
    const kind = policy?.getAttribute('content') ?? 'state';
    const follows = policy?.hasAttribute('data-follows') ?? false;
    const isWebView = policy?.hasAttribute('data-web-view') ?? false;

    if (kind === 'address') {
        return {
            kind,
            follows,
            isWebView,
            routes: JSON.parse(policy.getAttribute('data-routes') ?? '[]'),
            defaultToken: policy.getAttribute('data-default'),
            prefixesDefault: policy.getAttribute('data-prefix') === 'always',
            switchesInPlace: policy.getAttribute('data-switch') === 'in-place',
            switchTimeout: readSwitchTimeout(policy.getAttribute('data-switch-timeout')),
            root: policy.getAttribute('data-root') ?? '/',
            cookieName: policy.getAttribute('data-cookie-name'),
            cookiePath: policy.getAttribute('data-cookie-path') ?? '/',
        };
    }

    if (kind !== 'cookie') {
        return { kind, follows, isWebView };
    }

    const maxAge = policy.getAttribute('data-max-age');

    return {
        kind,
        follows,
        isWebView,
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

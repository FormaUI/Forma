// The document's theme coordinator, on the switch coordinator: the choice the page accepted last with its version, every
// runtime's registration, which receives each choice accepted, the document's two theme attributes, which follow the
// accepted choice, the browser's record of the choice, and the host's store, written in the order of the choices accepted.

import { SwitchCoordinator, SwitchRegistration } from '../../coordination/scripts/SwitchCoordinator.js';

const root = document.documentElement;

// A year, in seconds: how long the browser keeps its record of the choice.
const recordMaxAge = 31536000;

// What the coordinator read as the document loaded: whether the page keeps the reader's choice, the store's revision the
// page knows of, and each registered theme's schemes and whether its stylesheet loaded; or why it can't start.
let initialization = null;

// The choices the page accepts, in the order of its sequence, the marker's choice number zero until a switch, and every
// runtime's registration.
const coordinator = new SwitchCoordinator({ write: writeChoice, toArguments: choice => [choice.theme, choice.scheme] });

// The store's writes: the one under way, and the one waiting, which a newer accepted choice takes the place of.
let storeWrite = null;
let waitingWrite = null;

/** Reads the page's theme marker and its themes' stylesheets from the document's head, once, as the document loads. */
export function initialize(head = document.head) {
    if (initialization !== null) {
        return;
    }

    const marker = readMarker(head);

    if (marker === null) {
        initialization = {
            error: new Error('The page carries no valid theme marker. A server-rendered page places FormaPageHead in its head, which publishes the theme choice the server resolved.'),
        };

        return;
    }

    initialization = { keepsNothing: marker.keepsNothing, revision: marker.revision, themes: readThemes(head) };
    coordinator.start({ theme: marker.theme, scheme: marker.scheme });
}

/**
 * Registers the theme state named `identity` with the receiver every accepted choice is delivered to, recovering its live
 * registration where it has one, and completes once the state has adopted the accepted choice. Where `hasStore`, the
 * runtime writes the host's store when the coordinator asks it to.
 */
export async function register(identity, receiver, runtime = 'server', hasStore = false) {
    const { error } = getInitialization();

    if (error) {
        throw error;
    }

    const registration = coordinator.register(identity, () => new ThemeCoordinatorRegistration(identity, runtime));
    registration.receiver = receiver;
    registration.hasStore = hasStore;
    await registration.deliver(coordinator.accepted);

    return registration;
}

/** Releases the registrations of the circuit the client closed, leaving the document on the accepted choice. */
export function releaseCircuit() {
    coordinator.releaseRuntime('server');
}

/** Follows the document's enhanced page updates: each navigation holds the document until it lands, its landing is accepted from its response's marker, and the accepted choice is set again after each update. */
export function track(blazor) {
    coordinator.track(blazor, { land: () => land(readMarker(document.head)), end: () => {} });
}

/** Whether the theme `name` names can be switched to: a registered theme whose stylesheet the page loaded. */
export function isSelectable(name) {
    return getInitialization().themes?.get(name)?.isLoaded === true;
}

// A theme state's registration with the coordinator.
class ThemeCoordinatorRegistration extends SwitchRegistration {
    hasStore = false;

    constructor(identity, runtime) {
        super(coordinator, identity, runtime);
    }

    /**
     * Requests a switch to `theme` in the scheme preference `scheme`, completing with `accepted` once the switch was accepted,
     * the browser's record holds it where the page keeps the reader's choice, this registration's state applied it, and the
     * store holds it or a newer choice; with `unselectable` where the theme isn't selectable, changing nothing; and with
     * `canceled` where this registration made it before one already received, by its place `order` among them.
     */
    request(theme, scheme, order) {
        if (!this.takeRequest(order)) {
            return Promise.resolve('canceled');
        }

        if (!isSelectable(theme)) {
            return Promise.resolve('unselectable');
        }

        return coordinator.enqueue(async () => {
            if (!this.isLive) {
                throw new Error('The theme state requesting the switch is no longer registered with the page.');
            }

            const choice = { theme, scheme };

            if (!initialization.keepsNothing) {
                persist(choice, initialization.revision);
            }

            const accepted = coordinator.accept(choice, [this]);
            await this.deliver(accepted);

            return { written: initialization.keepsNothing ? Promise.resolve() : writeStore(this, accepted) };
        }).then(async ({ written }) => {
            await written;

            return 'accepted';
        });
    }
}

function getInitialization() {
    if (initialization === null) {
        initialize();
    }

    return initialization;
}

// A server-rendered landing takes its choice from its own response's marker, and the store's revision it rests on; a
// response without a valid marker leaves the accepted choice.
function land(marker) {
    if (marker === null || coordinator.accepted === null) {
        return;
    }

    initialization.keepsNothing = marker.keepsNothing;
    initialization.revision = Math.max(initialization.revision, marker.revision);

    if (marker.theme !== coordinator.accepted.theme || marker.scheme !== coordinator.accepted.scheme) {
        coordinator.accept({ theme: marker.theme, scheme: marker.scheme });
    }
}

// The store is written in the order the choices were accepted, one write at a time: a write starts once the one before it
// has written, and a newer accepted choice takes the place of a write that hasn't started, whose switch completes once the
// newer write does. The runtime that requested the switch writes it, or another registered with a store where that one
// has gone; each completed write's revision goes into the browser's record beside the choice accepted last.
function writeStore(requester, accepted) {
    return new Promise((resolve, reject) => {
        const waiters = [...(waitingWrite?.waiters ?? []), { resolve, reject }];
        waitingWrite = { requester, accepted, waiters };
        startWaitingWrite();
    });
}

function startWaitingWrite() {
    if (storeWrite !== null || waitingWrite === null) {
        return;
    }

    const write = waitingWrite;
    waitingWrite = null;
    storeWrite = (async () => {
        try {
            const writer = findWriter(write.requester);

            if (writer !== null) {
                const revision = initialization.revision + 1;
                await writer.receiver.invokeMethodAsync('WriteStoreAsync', write.accepted.theme, write.accepted.scheme, revision);
                initialization.revision = revision;
                persist(coordinator.accepted, revision);
            }

            write.waiters.forEach(waiter => waiter.resolve());
        } catch (error) {
            write.waiters.forEach(waiter => waiter.reject(error));
        } finally {
            storeWrite = null;
            startWaitingWrite();
        }
    })();
}

// The registration that writes the store for a switch: its requester, where it is live and has a store, or another live
// registration with one; none where no runtime on the page has a store.
function findWriter(requester) {
    if (requester.isLive && requester.hasStore) {
        return requester;
    }

    return coordinator.findRegistration(registration => registration.isLive && registration.hasStore) ?? null;
}

// The browser's record is written and read back before the switch is accepted, so it holds the last choice accepted.
function persist(choice, revision) {
    const name = getRecordName();
    const value = `theme=${encodeURIComponent(choice.theme)}&scheme=${choice.scheme}&revision=${revision}`;
    const attributes = [`${name}=${encodeURIComponent(value)}`, 'path=/', `max-age=${recordMaxAge}`, 'samesite=lax'];

    if (location.protocol === 'https:') {
        attributes.push('secure');
    }

    document.cookie = attributes.join('; ');

    if (readRecord(name) !== value) {
        throw new Error(`The theme record ${name} couldn't be read back, so the switch wasn't accepted.`);
    }
}

// The record is a host-prefixed cookie over HTTPS, so only the page's own origin sets it, and a plain one over HTTP, where
// a browser refuses a secure cookie.
function getRecordName() {
    return location.protocol === 'https:' ? '__Host-forma-theme' : 'forma-theme';
}

function readRecord(name) {
    const prefix = `${name}=`;
    const entry = document.cookie.split('; ').find(cookie => cookie.startsWith(prefix));

    return entry === undefined ? null : decodeURIComponent(entry.slice(prefix.length));
}

function readMarker(head) {
    const marker = head?.querySelector('meta[name="forma-theme"]') ?? null;

    if (marker === null) {
        return null;
    }

    const theme = marker.getAttribute('data-theme');
    const scheme = marker.getAttribute('data-scheme');
    const revision = Number(marker.getAttribute('data-revision'));

    return theme && ['system', 'light', 'dark'].includes(scheme) && Number.isSafeInteger(revision) && revision >= 0
        ? { theme, scheme, revision, keepsNothing: marker.hasAttribute('data-keeps-nothing') }
        : null;
}

// Each registered theme's stylesheet names its theme and its schemes; a stylesheet the browser holds no sheet for didn't
// load, and its theme isn't selectable.
function readThemes(head) {
    const themes = new Map();

    for (const link of head.querySelectorAll('link[data-forma-theme-stylesheet]')) {
        themes.set(link.getAttribute('data-forma-theme-stylesheet'), {
            schemes: (link.getAttribute('data-forma-theme-schemes') ?? 'light dark').split(' '),
            isLoaded: link.sheet !== null,
        });
    }

    return themes;
}

// The document draws the accepted choice: its theme, and its scheme preference, or its light scheme where its theme draws
// no dark one.
function writeChoice(choice) {
    const schemes = initialization?.themes?.get(choice.theme)?.schemes ?? ['light', 'dark'];
    writeAttribute('data-forma-theme', choice.theme);
    writeAttribute('data-forma-scheme', schemes.includes('dark') ? choice.scheme : 'light');
}

function writeAttribute(name, value) {
    if (root.getAttribute(name) !== value) {
        root.setAttribute(name, value);
    }
}

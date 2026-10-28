// The document's language and direction: the pair the server last served, and the pair of the one culture state
// that owns the document while its registration is live.

const root = document.documentElement;

// What the document's attributes were as it loaded, before anything here wrote them.
let servedPair = readPair();
let owner = null;
let isTracking = false;
let isNavigationPending = false;

// The hold an enhanced navigation places on the document of an owner that follows addresses, from the navigation's start
// until the reply to its landing's report; a reply ends only the hold it was made for.
let hold = null;

/** Registers the owner named `identity` with its pair and the receiver it follows addresses through, recovering the live registration where it is that owner's. */
export function register(identity, language, direction, receiver = null) {
    if (owner !== null && owner.identity !== identity) {
        throw new Error('A document follows one culture state.');
    }

    owner ??= new DocumentCultureSynchronizerRegistration(identity);
    owner.follow(receiver);
    owner.update(language, direction);

    // A landing whose report failed, or whose reply the dropped connection lost, is reported again over the new one.
    if (hold?.registration === owner && hold.address !== null) {
        report(hold);
    }

    return owner;
}

/** Releases the document's owner, restoring the pair the server last served. */
export function release() {
    if (owner !== null) {
        owner = null;
        hold = null;
        writePair(servedPair);
    }
}

/** Follows the document's enhanced page updates, reading what each navigation serves, holding an owner's document that follows addresses through each navigation, and setting the owner's pair again after each update. */
export function track(blazor) {
    if (isTracking) {
        return;
    }

    isTracking = true;

    blazor.addEventListener('enhancednavigationstart', () => {
        isNavigationPending = true;
        hold = owner?.followsAddresses ? new DocumentCultureHold(owner) : null;
    });

    // A navigation's response patches the whole document, `<html>` among it, before its first update is announced; a
    // streaming update patches a component's region alone, so only the first update after a navigation starts is read.
    blazor.addEventListener('enhancedload', () => {
        if (isNavigationPending) {
            isNavigationPending = false;
            servedPair = readPair();

            if (hold !== null) {
                hold.address = location.href;
                report(hold);
            }
        }

        if (hold === null && owner !== null) {
            writePair(owner.pair);
        }
    });

    // A navigation that ends without landing leaves nothing to report, while one that landed waits for its reply.
    blazor.addEventListener('enhancednavigationend', () => {
        if (hold !== null && hold.address === null) {
            endHold();
        }
    });
}

// A culture state's registration as the document's owner.
class DocumentCultureSynchronizerRegistration {
    #identity;
    #pair = null;
    #receiver = null;

    constructor(identity) {
        this.#identity = identity;
    }

    get identity() {
        return this.#identity;
    }

    get pair() {
        return this.#pair;
    }

    get receiver() {
        return this.#receiver;
    }

    get followsAddresses() {
        return this.#receiver !== null;
    }

    /** Takes the receiver this owner follows addresses through, or none. */
    follow(receiver) {
        this.#receiver = receiver;
    }

    /** Sets the document to this owner's new pair, keeping it unwritten through a hold, and reports whether this registration still owns the document. */
    update(language, direction) {
        if (owner !== this) {
            return false;
        }

        this.#pair = { language, direction };

        if (hold === null) {
            writePair(this.#pair);
        }

        return true;
    }

    /** Releases the document where this registration still owns it. */
    dispose() {
        if (owner === this) {
            release();
        }
    }
}

// One enhanced navigation's hold on one registration's document, and the address the navigation landed at.
class DocumentCultureHold {
    registration;
    address = null;

    constructor(registration) {
        this.registration = registration;
    }
}

// The reply ends the hold once the browser holds the pair the circuit's state is left in; a refused report leaves the hold
// to the next registration's report.
function report(reported) {
    reported.registration.receiver.invokeMethodAsync('ReportLandingAsync', reported.address).then(
        holdsPair => {
            if (holdsPair && hold === reported) {
                endHold();
            }
        },
        () => {});
}

function endHold() {
    hold = null;

    if (owner !== null) {
        writePair(owner.pair);
    }
}

function readPair() {
    return { language: root.getAttribute('lang'), direction: root.getAttribute('dir') };
}

function writePair(pair) {
    writeAttribute('lang', pair.language);
    writeAttribute('dir', pair.direction);
}

function writeAttribute(name, value) {
    if (value === null) {
        root.removeAttribute(name);
    } else if (root.getAttribute(name) !== value) {
        root.setAttribute(name, value);
    }
}

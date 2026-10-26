// The document's language and direction: the pair the server last served, and the pair of the one culture state
// that owns the document while its registration is live.

const root = document.documentElement;

// What the document's attributes were as it loaded, before anything here wrote them.
let servedPair = readPair();
let owner = null;
let isTracking = false;
let isNavigationPending = false;

/** Registers the owner named `identity` with its pair, recovering the live registration where it is that owner's. */
export function register(identity, language, direction) {
    if (owner !== null && owner.identity !== identity) {
        throw new Error('A document follows one culture state.');
    }

    owner ??= new DocumentCultureSynchronizerRegistration(identity);
    owner.update(language, direction);

    return owner;
}

/** Releases the document's owner, restoring the pair the server last served. */
export function release() {
    if (owner !== null) {
        owner = null;
        writePair(servedPair);
    }
}

/** Follows the document's enhanced page updates, reading what each navigation serves and setting the owner's pair again after each update. */
export function track(blazor) {
    if (isTracking) {
        return;
    }

    isTracking = true;

    // A navigation's response patches the whole document, `<html>` among it, before its first update is announced; a
    // streaming update patches a component's region alone, so only the first update after a navigation starts is read.
    blazor.addEventListener('enhancednavigationstart', () => {
        isNavigationPending = true;
    });

    blazor.addEventListener('enhancedload', () => {
        if (isNavigationPending) {
            isNavigationPending = false;
            servedPair = readPair();
        }

        if (owner !== null) {
            writePair(owner.pair);
        }
    });
}

// A culture state's hold on the document.
class DocumentCultureSynchronizerRegistration {
    #identity;
    #pair = null;

    constructor(identity) {
        this.#identity = identity;
    }

    get identity() {
        return this.#identity;
    }

    get pair() {
        return this.#pair;
    }

    /** Sets the document to this owner's new pair, reporting whether this registration still owns the document. */
    update(language, direction) {
        if (owner !== this) {
            return false;
        }

        this.#pair = { language, direction };
        writePair(this.#pair);

        return true;
    }

    /** Releases the document where this registration still owns it. */
    dispose() {
        if (owner === this) {
            release();
        }
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

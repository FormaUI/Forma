/**
 * DomSessionRegistry — the browser counterpart of each .NET owner's DOM session, found by the owner's runtime id
 *
 * A session holds the watcher the owner observes through and, once it acts, the engine it acts through.
 * Releasing the session releases its engine before its watcher.
 */

/** @typedef {{ dispose(): void }} Part */
/** @typedef {{ watcher: Part | null, engine: Part | null }} Session */

/** @type {Map<string, Session>} */
const sessions = new Map();

export class DomSessionRegistry {
    /** The owner's session, made on its first use. */
    static open(owner) {
        let session = sessions.get(owner);

        if (!session) {
            session = { watcher: null, engine: null };
            sessions.set(owner, session);
        }

        return session;
    }

    /** The owner's session, or null where it has none. */
    static find(owner) {
        return sessions.get(owner) ?? null;
    }

    /** Releases the owner's session, its engine before its watcher. */
    static release(owner) {
        const session = sessions.get(owner);

        if (!session) {
            return;
        }

        sessions.delete(owner);
        session.engine?.dispose();
        session.watcher?.dispose();
    }

    /** Releases the owner's engine alone, leaving its watcher. */
    static releaseEngine(owner) {
        const session = sessions.get(owner);

        if (!session?.engine) {
            return;
        }

        session.engine.dispose();
        session.engine = null;

        if (!session.watcher) {
            sessions.delete(owner);
        }
    }

    /** The number of owners with a session. */
    static get size() {
        return sessions.size;
    }
}

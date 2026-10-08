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
    static open(runtimeKey) {
        let session = sessions.get(runtimeKey);

        if (!session) {
            session = { watcher: null, engine: null };
            sessions.set(runtimeKey, session);
        }

        return session;
    }

    /** The owner's session, or null where it has none. */
    static find(runtimeKey) {
        return sessions.get(runtimeKey) ?? null;
    }

    /** Releases the owner's session, its engine before its watcher. */
    static release(runtimeKey) {
        const session = sessions.get(runtimeKey);

        if (!session) {
            return;
        }

        sessions.delete(runtimeKey);
        session.engine?.dispose();
        session.watcher?.dispose();
    }

    /** Releases the owner's engine alone, leaving its watcher. */
    static releaseEngine(runtimeKey) {
        const session = sessions.get(runtimeKey);

        if (!session?.engine) {
            return;
        }

        session.engine.dispose();
        session.engine = null;

        if (!session.watcher) {
            sessions.delete(runtimeKey);
        }
    }

    /** The number of owners with a session. */
    static get size() {
        return sessions.size;
    }
}

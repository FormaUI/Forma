/**
 * DomSessionRegistry — the owners' sessions, by runtime key
 */

import { DomSession } from './DomSession.mjs';

/** @type {Map<string, DomSession>} */
const sessions = new Map();

export class DomSessionRegistry {
    /** Opens the owner's session. */
    static open(runtimeKey) {
        let session = sessions.get(runtimeKey);

        if (!session) {
            session = new DomSession(runtimeKey);
            sessions.set(runtimeKey, session);
        }

        return session;
    }

    /** The owner's session. */
    static find(runtimeKey) {
        return sessions.get(runtimeKey) ?? null;
    }

    /** Releases the owner's session. */
    static release(runtimeKey) {
        const session = sessions.get(runtimeKey);

        if (!session) {
            return;
        }

        sessions.delete(runtimeKey);
        session.dispose();
    }

    /** Releases the owner's engine. */
    static releaseEngine(runtimeKey) {
        const session = sessions.get(runtimeKey);

        if (!session) {
            return;
        }

        if (session.watcher) {
            session.releaseEngine();
        } else {
            DomSessionRegistry.release(runtimeKey);
        }
    }
}

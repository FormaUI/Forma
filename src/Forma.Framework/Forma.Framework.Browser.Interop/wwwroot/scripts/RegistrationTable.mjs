/**
 * RegistrationTable — an owner's registrations by token, each held under the latest generation its owner asked for
 *
 * The .NET side mints a registration's token and each generation of it. A generation is recorded when an
 * attach for it begins, so the registration it replaces is released at once, and an attach that a later
 * one, a detach or a release overtook while it loaded its capability is dropped when it lands.
 */

/** @typedef {{ dispose(): void }} Resource */

export class RegistrationTable {
    /** @type {Map<string, { generation: number, resource: Resource | null }>} */
    #entries = new Map();
    #isDisposed = false;

    /** Whether the registrations have been released. */
    get isDisposed() {
        return this.#isDisposed;
    }

    /** The number of registrations holding a resource. */
    get size() {
        let count = 0;

        for (const entry of this.#entries.values()) {
            count += entry.resource ? 1 : 0;
        }

        return count;
    }

    /**
     * Records the generation as the token's latest and releases what an earlier one holds; false when the
     * generation is already behind, or the registrations have been released.
     */
    begin(token, generation) {
        if (this.#isDisposed) {
            return false;
        }

        const entry = this.#entries.get(token);

        if (entry && entry.generation >= generation) {
            return false;
        }

        entry?.resource?.dispose();
        this.#entries.set(token, { generation, resource: null });

        return true;
    }

    /** Whether the generation is still the token's latest. */
    isCurrent(token, generation) {
        return !this.#isDisposed && this.#entries.get(token)?.generation === generation;
    }

    /** Holds the resource under the generation; false, with the resource released, when the generation is no longer the latest. */
    install(token, generation, resource) {
        if (!this.isCurrent(token, generation)) {
            resource.dispose();
            return false;
        }

        this.#entries.get(token).resource = resource;

        return true;
    }

    /** The resource the token holds. */
    get(token) {
        return this.#entries.get(token)?.resource ?? null;
    }

    /** Records the generation as the token's latest and releases what the token holds. */
    remove(token, generation) {
        const entry = this.#entries.get(token);

        if (entry && entry.generation > generation) {
            return;
        }

        entry?.resource?.dispose();

        // The token is kept, under the generation, so an attach for an earlier one still loading is dropped when it lands.
        this.#entries.set(token, { generation, resource: null });
    }

    /** Releases every resource and refuses any later registration. */
    dispose() {
        this.#isDisposed = true;

        for (const entry of this.#entries.values()) {
            entry.resource?.dispose();
        }

        this.#entries.clear();
    }
}

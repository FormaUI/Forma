/**
 * RegistrationTable — an owner's registrations by token and generation
 */

/** @typedef {{ dispose(): void }} Resource */

export class RegistrationTable {
    /** @type {Map<string, { generation: number, resource: Resource | null }>} */
    #entries = new Map();
    #isDisposed = false;

    /** Begins the token's generation, returning whether it began. */
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

    /** Holds the resource under the generation, returning whether it is held. */
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

    /** Releases what the token holds. */
    remove(token, generation) {
        const entry = this.#entries.get(token);

        if (entry && entry.generation > generation) {
            return;
        }

        entry?.resource?.dispose();

        // The token keeps the generation it was removed under.
        this.#entries.set(token, { generation, resource: null });
    }

    /** Releases every resource. */
    dispose() {
        this.#isDisposed = true;

        for (const entry of this.#entries.values()) {
            entry.resource?.dispose();
        }

        this.#entries.clear();
    }
}

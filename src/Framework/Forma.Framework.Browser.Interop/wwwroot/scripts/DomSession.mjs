/**
 * DomSession — an owner's watcher, its engine, and the elements they share
 */

import { DomElements } from './DomElements.mjs';
import { DomWatcher } from './DomWatcher.mjs';

/** @typedef {{ dispose(): void }} Part */
/** @typedef {new (elements: DomElements, runtimeKey: string) => Part} EngineClass */

export class DomSession {
    #runtimeKey;
    #elements = new DomElements();
    /** @type {DomWatcher | null} */
    #watcher = null;
    /** @type {Part | null} */
    #engine = null;

    /**
     * @param {string} runtimeKey The owner's runtime key.
     */
    constructor(runtimeKey) {
        this.#runtimeKey = runtimeKey;
    }

    /** The owner's watcher. */
    get watcher() {
        return this.#watcher;
    }

    /** The owner's engine. */
    get engine() {
        return this.#engine;
    }

    /** Opens the owner's watcher. */
    openWatcher() {
        return this.#watcher ??= new DomWatcher(this.#runtimeKey, this.#elements);
    }

    /**
     * Opens the owner's engine.
     * @param {EngineClass} Engine The engine's class.
     */
    openEngine(Engine) {
        return this.#engine ??= new Engine(this.#elements, this.#runtimeKey);
    }

    /** Releases the owner's engine. */
    releaseEngine() {
        this.#engine?.dispose();
        this.#engine = null;
    }

    dispose() {
        this.releaseEngine();
        this.#watcher?.dispose();
        this.#watcher = null;
        this.#elements.dispose();
    }
}

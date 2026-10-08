/**
 * DomSession — an owner's watcher, its engine, and the elements they share
 */

import { DomElement } from './DomElement.mjs';
import { DomWatcher } from './DomWatcher.mjs';

/** @typedef {{ dispose(): void }} Part */
/** @typedef {new (elementOf: (target: Element) => DomElement) => Part} EngineClass */

export class DomSession {
    #runtimeKey;
    /** @type {Map<Element, DomElement>} */
    #elements = new Map();
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
        return this.#watcher ??= new DomWatcher(this.#runtimeKey, target => this.#elementOf(target));
    }

    /**
     * Opens the owner's engine.
     * @param {EngineClass} Engine The engine's class.
     */
    openEngine(Engine) {
        return this.#engine ??= new Engine(target => this.#elementOf(target));
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

        // Disposing an element removes it from the map.
        for (const element of [...this.#elements.values()]) {
            element.dispose();
        }
    }

    #elementOf(target) {
        let element = this.#elements.get(target);

        if (!element) {
            element = new DomElement(target, () => this.#elements.delete(target));
            this.#elements.set(target, element);
        }

        return element;
    }
}

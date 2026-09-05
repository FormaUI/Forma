/**
 * InputInspector — the input router's decisions, reported as they are made
 *
 * Every key, press, and wheel turn on the page is resolved again, without dispatching it,
 * and the decision and the active ordering are described to the .NET owner in words a
 * panel can show. Wheel turns are reported at most once per interval.
 */

import { resolve, stack } from './InputRouter.js';

const ObservedTypes = ['keydown', 'pointerdown', 'wheel'];

/**
 * @param {Element} target The inspector's element.
 * @param {{ owner: { invokeMethodAsync: (name: string, ...args: unknown[]) => Promise<unknown> }, wheelInterval?: number }} options
 */
export function attach(target, options) {
    return new InputInspector(options);
}

class InputInspector {
    #owner;
    #wheelInterval;
    #controller = new AbortController();
    #lastWheel = 0;

    constructor({ owner, wheelInterval = 100 }) {
        this.#owner = owner;
        this.#wheelInterval = wheelInterval;
        const { signal } = this.#controller;

        for (const type of ObservedTypes) {
            document.addEventListener(type, event => this.#report(event), { signal, capture: true, passive: true });
        }
    }

    dispose() {
        this.#controller.abort();
    }

    #report(event) {
        if (event.type === 'wheel') {
            const now = performance.now();

            if (now - this.#lastWheel < this.#wheelInterval) {
                return;
            }

            this.#lastWheel = now;
        }

        // The owner may already have been disposed by the time the report lands.
        this.#owner.invokeMethodAsync('ReportAsync', describeDecision(resolve(event))).catch(() => {});
    }
}

function describeDecision(decision) {
    return {
        input: decision.input,
        owner: describeElement(decision.owner),
        action: decision.action,
        precedence: decision.precedence,
        source: decision.source ?? null,
        tie: decision.tie,
        reason: decision.reason,
        preventDefault: decision.preventDefault,
        propagation: decision.propagation,
        stack: stack().map(entry => ({
            element: describeElement(entry.element),
            layer: entry.layer,
            modal: entry.modal,
            parent: describeElement(entry.parent),
            activation: entry.activation,
            reason: entry.reason,
        })),
    };
}

/** An element as a short selector: its tag and its id, or its tag and up to two of its classes when it has no id. */
function describeElement(element) {
    if (!(element instanceof Element)) {
        return null;
    }

    const tag = element.tagName.toLowerCase();

    if (element.id) {
        return `${tag}#${element.id}`;
    }

    return `${tag}${[...element.classList].slice(0, 2).map(name => `.${name}`).join('')}`;
}

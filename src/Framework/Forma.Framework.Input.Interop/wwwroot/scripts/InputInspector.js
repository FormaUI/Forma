/**
 * InputInspector — the input router's decision for every key, press, and wheel turn on the
 * page, with the active ordering, reported to a .NET owner
 */

import { resolve, stack } from './InputRouter.js';

const ObservedTypes = ['keydown', 'pointerdown', 'wheel'];

/** The attribute an element carries while an inspector's listeners are attached to it. */
const InspectingAttribute = 'data-forma-inspecting';

// The inspectors attached to each element, so the mark stays while any of them listens.
const inspectorCounts = new WeakMap();

/**
 * @param {Element} target The inspector's element.
 * @param {{ owner: { invokeMethodAsync: (name: string, ...args: unknown[]) => Promise<unknown> }, wheelInterval?: number }} options
 */
export function attach(target, options) {
    return new InputInspector(target, options);
}

class InputInspector {
    #target;
    #owner;
    #wheelInterval;
    #controller = new AbortController();
    #lastWheel = 0;

    constructor(target, { owner, wheelInterval = 100 }) {
        this.#target = target;
        this.#owner = owner;
        this.#wheelInterval = wheelInterval;
        const { signal } = this.#controller;

        for (const type of ObservedTypes) {
            document.addEventListener(type, event => this.#report(event), { signal, capture: true, passive: true });
        }

        countInspectors(target, 1);
    }

    dispose() {
        if (this.#controller.signal.aborted) {
            return;
        }

        this.#controller.abort();
        countInspectors(this.#target, -1);
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

/** Counts an inspector attached to or detached from `target`, marking it while any is attached. */
function countInspectors(target, change) {
    if (!(target instanceof Element)) {
        return;
    }

    const count = (inspectorCounts.get(target) ?? 0) + change;

    if (count > 0) {
        inspectorCounts.set(target, count);
        target.setAttribute(InspectingAttribute, '');
    } else {
        inspectorCounts.delete(target);
        target.removeAttribute(InspectingAttribute);
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

/** An element as a short selector. */
function describeElement(element) {
    if (!(element instanceof Element)) {
        return null;
    }

    const tag = element.tagName.toLowerCase();

    const runtimeId = element.getAttribute('data-rid');

    if (runtimeId) {
        return `${tag}[data-rid="${runtimeId}"]`;
    }

    if (element.id) {
        return `${tag}#${element.id}`;
    }

    return `${tag}${[...element.classList].slice(0, 2).map(name => `.${name}`).join('')}`;
}

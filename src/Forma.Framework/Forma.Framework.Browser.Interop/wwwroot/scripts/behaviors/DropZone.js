/**
 * DropZone — a drag-and-drop target
 *
 * The drag passing over and the drop are one route on the input router, declared on the zone
 * under the action the caller names and matching the data kinds it accepts. A zone holding
 * several targets, each marked by an attribute and named by a key, gives the drop to the target
 * under the pointer. The active state is depth-counted across the dragenter and dragleave of
 * nested children.
 */

import { tell } from '../../../core/scripts/Owner.js';
import { register } from '../../../input/scripts/InputRouter.js';

/** @typedef {import('../../../core/scripts/Owner.js').Owner} Owner */

/**
 * @param {HTMLElement} target
 * @param {{
 *   action: string,
 *   accepts?: string[],
 *   dropEffect?: 'copy' | 'move' | 'link',
 *   targets?: string,
 *   activeClass?: string,
 *   owner?: Owner,
 *   onDrop?: (transfer: DataTransfer, target: HTMLElement) => void
 * }} options
 *   The action the drop is routed as; the data kinds the zone takes, every kind when unset; the effect the
 *   drag shows over it; the selector of the parts inside the zone that each take a drop, the zone itself
 *   when unset; the class marking the zone while a drag is over it; and who is told of a drop: the owner
 *   through HandleDropAsync with the target's data-drop-key, or the function given the transfer and target.
 */
export function attach(target, options) {
    return new DropZone(target, options);
}

class DropZone {
    #target;
    #dropEffect;
    #targets;
    #activeClass;
    #owner;
    #onDrop;
    #controller = new AbortController();
    #registration;
    #depth = 0;

    constructor(target, { action, accepts, dropEffect = 'copy', targets, activeClass, owner, onDrop }) {
        this.#target = target;
        this.#dropEffect = dropEffect;
        this.#targets = targets ?? null;
        this.#activeClass = activeClass;
        this.#owner = owner ?? null;
        this.#onDrop = onDrop ?? null;

        const { signal } = this.#controller;
        const binding = type => ({ type, ...(accepts ? { data: accepts } : {}), when: event => this.#findTarget(event) !== null });

        this.#registration = register(target, {
            routes: [{ action, bindings: [binding('dragover'), binding('drop')], handle: event => this.#handle(event) }],
        });

        target.addEventListener('dragenter', event => this.#handleEnter(event), { signal });
        target.addEventListener('dragleave', () => this.#handleLeave(), { signal });
    }

    dispose() {
        this.#controller.abort();
        this.#registration.dispose();
        this.#setActive(false);
    }

    /** The part of the zone the event is over that takes a drop: the zone itself unless it names targets. */
    #findTarget(event) {
        if (this.#targets === null) {
            return this.#target;
        }

        const target = event.target instanceof Element ? event.target.closest(this.#targets) : null;

        return target && this.#target.contains(target) ? target : null;
    }

    // The target is found again here, since a host's remap of the action replaces the binding and its predicate with it.
    #handle(event) {
        const target = this.#findTarget(event);

        if (!target) {
            return;
        }

        if (event.type === 'dragover') {
            event.dataTransfer.dropEffect = this.#dropEffect;
            return;
        }

        this.#depth = 0;
        this.#setActive(false);

        if (this.#onDrop) {
            this.#onDrop(event.dataTransfer, target);
        } else {
            tell(this.#owner, 'HandleDropAsync', target.dataset.dropKey ?? null);
        }
    }

    // An uncancelled dragenter sends the drag on to the body, and the drag passing over never reaches the route.
    #handleEnter(event) {
        event.preventDefault();
        this.#depth += 1;
        this.#setActive(true);
    }

    #handleLeave() {
        this.#depth = Math.max(this.#depth - 1, 0);

        if (this.#depth === 0) {
            this.#setActive(false);
        }
    }

    #setActive(isActive) {
        if (this.#activeClass) {
            this.#target.classList.toggle(this.#activeClass, isActive);
        }
    }
}

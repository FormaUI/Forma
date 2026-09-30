/**
 * PanGesture — dragging a scroll container's contents with the pointer
 *
 * A mouse drag, through DragGesture, moves the container's scroll offset with the pointer,
 * its press a route on the input router under the action the container names.
 */

import { attach as attachDragGesture } from './DragGesture.js';

/**
 * @param {HTMLElement} target
 * @param {{ action: string, whileClass?: string }} options The action the press is routed as, and the class the target must carry for a drag to start.
 */
export function attach(target, options) {
    return new PanGesture(target, options);
}

class PanGesture {
    #target;
    #whileClass;
    #gesture;

    /** Where the target was scrolled to when the pan began. */
    #origin = null;

    constructor(target, { action, whileClass }) {
        this.#target = target;
        this.#whileClass = whileClass;

        this.#gesture = attachDragGesture(target, {
            action,
            pointerType: 'mouse',
            threshold: 0,
            axis: 'any',
            autoScroll: false,
            findSubject: () => this.#isPannable() ? target : null,
            onStart: () => this.#begin(),
            onMove: context => this.#pan(context),
            onDrop: () => this.#end(),
            onCancel: () => this.#end(),
        });
    }

    dispose() {
        this.#gesture.dispose();
    }

    #isPannable() {
        return !this.#whileClass || this.#target.classList.contains(this.#whileClass);
    }

    #begin() {
        this.#origin = { left: this.#target.scrollLeft, top: this.#target.scrollTop };
    }

    #pan({ clientX, clientY, startX, startY }) {
        if (!this.#origin) {
            return;
        }

        this.#target.scrollLeft = this.#origin.left - (clientX - startX);
        this.#target.scrollTop = this.#origin.top - (clientY - startY);
    }

    #end() {
        this.#origin = null;
    }
}

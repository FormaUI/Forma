/**
 * PanGesture — dragging a scroll container's contents with the pointer
 *
 * Mouse users get click-drag panning; touch and pen already pan a scrollable frame
 * natively, so they are left alone.
 */

/**
 * @param {HTMLElement} target
 * @param {{ whileClass?: string }} options The class the target must carry for a drag to start.
 */
export function attach(target, options = {}) {
    return new PanGesture(target, options);
}

class PanGesture {
    #target;
    #whileClass;
    #controller = new AbortController();
    #drag = null;

    constructor(target, { whileClass }) {
        this.#target = target;
        this.#whileClass = whileClass;

        const { signal } = this.#controller;
        const end = event => this.#handleEnd(event);

        target.addEventListener('pointerdown', event => this.#handleDown(event), { signal });
        target.addEventListener('pointermove', event => this.#handleMove(event), { signal });
        target.addEventListener('pointerup', end, { signal });
        target.addEventListener('pointercancel', end, { signal });
    }

    dispose() {
        this.#controller.abort();

        if (this.#drag && this.#target.hasPointerCapture(this.#drag.pointerId)) {
            this.#target.releasePointerCapture(this.#drag.pointerId);
        }

        this.#drag = null;
    }

    #handleDown(event) {
        if (event.pointerType !== 'mouse') {
            return;
        }

        if (this.#whileClass && !this.#target.classList.contains(this.#whileClass)) {
            return;
        }

        this.#drag = {
            pointerId: event.pointerId,
            x: event.clientX,
            y: event.clientY,
            left: this.#target.scrollLeft,
            top: this.#target.scrollTop,
        };

        this.#target.setPointerCapture(event.pointerId);
    }

    #handleMove(event) {
        if (!this.#drag || event.pointerId !== this.#drag.pointerId) {
            return;
        }

        this.#target.scrollLeft = this.#drag.left - (event.clientX - this.#drag.x);
        this.#target.scrollTop = this.#drag.top - (event.clientY - this.#drag.y);
    }

    #handleEnd(event) {
        if (this.#drag?.pointerId === event.pointerId) {
            this.#drag = null;
        }
    }
}

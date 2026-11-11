/**
 * DragGesture — the press, move and release of a drag, through Pointer Events
 *
 * The handle owns one container: it decides when a press has traveled far enough to be a
 * drag, keeps the pointer captured for the rest of the gesture, scrolls the nearest
 * scrolling ancestor while the pointer is held at an edge, and reports each stage to its
 * caller with the subject, where the pointer is and where the press began.
 *
 * A drag under way is an active registration on the input router, standing above whatever
 * contains it: Escape and a platform close request cancel the drag wherever focus is, and a
 * press elsewhere dismisses nothing while it lasts.
 */

import { FrameScheduler } from '../../../core/scripts/FrameScheduler.js';
import { Scrolling } from '../../../core/scripts/Scrolling.js';
import { register } from '../../../input/scripts/InputRouter.js';
import { capture, release } from '../operations/Pointer.js';

/**
 * @param {HTMLElement} target
 * @param {{
 *   findSubject: (event: PointerEvent) => Element | null,
 *   onStart?: (context: object) => void,
 *   onMove?: (context: object) => void,
 *   onDrop?: (context: object) => void,
 *   onCancel?: (context: object) => void,
 *   threshold?: number,
 *   axis?: 'vertical' | 'any',
 *   autoScroll?: boolean,
 *   holdDelay?: number,
 *   pointerType?: 'mouse' | 'pen' | 'touch',
 *   action?: string
 * }} options
 *   How far a press travels before it is a drag; which axis that travel is measured along; whether a drag
 *   held at a scrolling edge scrolls; how long a touch rests before it lifts; the one pointer type the
 *   gesture answers, every type when unset; and the action the press is routed as on the gesture's
 *   registration.
 */
export function attach(target, options) {
    return new DragGesture(target, options);
}

class DragGesture {
    /** How far a pointer travels before a press counts as a drag rather than a click. */
    static #DefaultThreshold = 4;

    /** Distance from a scrolling edge at which a held drag starts scrolling. */
    static #ScrollEdge = 48;

    /** Pixels scrolled per frame while a drag is held at an edge. */
    static #ScrollStep = 8;

    #element;
    #findSubject;
    #onStart;
    #onMove;
    #onDrop;
    #onCancel;
    #threshold;
    #axis;
    #autoScroll;
    #holdDelay;
    #pointerType;

    #subject = null;
    #pressedElement = null;
    #pointerId = null;
    #startX = 0;
    #startY = 0;
    #lastX = 0;
    #lastY = 0;
    #isDragging = false;
    #holdTimer = null;
    #scroller = null;
    #scrollStep = 0;
    #scrollScheduler = new FrameScheduler();
    #listeners = new AbortController();
    #registration;

    constructor(container, options) {
        this.#element = container;
        this.#findSubject = options.findSubject;
        this.#onStart = options.onStart;
        this.#onMove = options.onMove;
        this.#onDrop = options.onDrop;
        this.#onCancel = options.onCancel;
        this.#threshold = options.threshold ?? DragGesture.#DefaultThreshold;
        this.#axis = options.axis ?? 'vertical';
        this.#autoScroll = options.autoScroll ?? true;
        this.#holdDelay = options.holdDelay ?? 0;
        this.#pointerType = options.pointerType ?? null;

        const { signal } = this.#listeners;

        this.#registration = register(container, {
            escape: 'cancel',
            pressAway: 'ignore',
            onDismiss: () => this.cancel(),
            routes: options.action ? [{
                action: options.action,
                bindings: [{ type: 'pointerdown', button: 0, ...(this.#pointerType ? { pointerType: this.#pointerType } : {}), when: event => this.#findPressed(event) !== null }],
                handle: event => this.#press(event),
            }] : [],
        });

        if (!options.action) {
            container.addEventListener('pointerdown', event => this.#handlePointerDown(event), { signal });
        }

        container.addEventListener('pointermove', event => this.#handlePointerMove(event), { signal });
        container.addEventListener('pointerup', event => this.#handlePointerUp(event), { signal });
        container.addEventListener('pointercancel', () => this.cancel(), { signal });

        // Refusing touchmove during a drag keeps the compositor from scrolling the touch and canceling the pointer.
        if (this.#pointerType === null || this.#pointerType === 'touch') {
            container.addEventListener('touchmove', event => {
                if (this.#isDragging) {
                    event.preventDefault();
                }
            }, { signal, passive: false });
        }
    }

    /** Stops listening and abandons any gesture in flight. */
    dispose() {
        this.cancel();
        this.#listeners.abort();
        this.#registration.dispose();
    }

    /** Abandons a gesture in flight, reporting a drag under way as canceled. */
    cancel() {
        if (!this.#subject) {
            return;
        }

        const context = this.#context();
        const wasDragging = this.#isDragging;
        this.#release();

        if (wasDragging) {
            this.#onCancel?.(context);
        }
    }

    #context() {
        return {
            subject: this.#subject,
            clientX: this.#lastX,
            clientY: this.#lastY,
            startX: this.#startX,
            startY: this.#startY,
        };
    }

    #release() {
        if (this.#holdTimer !== null) {
            clearTimeout(this.#holdTimer);
            this.#holdTimer = null;
        }

        this.#stopAutoScroll();
        this.#registration.release();

        if (this.#pointerId !== null) {
            release(this.#element, this.#pointerId);
        }

        this.#subject = null;
        this.#pressedElement = null;
        this.#pointerId = null;
        this.#isDragging = false;
    }

    /** Marks the press a drag, stands it above whatever contains it, and claims the click the drag ends in. */
    #begin() {
        this.#isDragging = true;
        this.#registration.activate();
        this.#registration.claimClick(this.#pointerId, this.#pressedElement);
        this.#onStart?.(this.#context());
    }

    #handlePointerDown(event) {
        if (event.button === 0) {
            this.#press(event);
        }
    }

    /** What a press would drag. */
    #findPressed(event) {
        if (this.#subject || (this.#pointerType && event.pointerType !== this.#pointerType)) {
            return null;
        }

        return this.#findSubject(event);
    }

    // The press is checked again here, since a host's remap of the action replaces the binding and its predicate with it.
    #press(event) {
        const subject = this.#findPressed(event);

        if (!subject) {
            return;
        }

        this.#subject = subject;
        this.#pressedElement = event.target;
        this.#pointerId = event.pointerId;
        this.#startX = event.clientX;
        this.#startY = event.clientY;
        this.#lastX = event.clientX;
        this.#lastY = event.clientY;
        this.#isDragging = false;
        this.#scroller = this.#autoScroll ? Scrolling.nearestScroller(this.#element) : null;

        // Capture waits for the hold, so a touch abandoned as a swipe still scrolls.
        if (this.#holdDelay > 0 && event.pointerType === 'touch') {
            this.#holdTimer = setTimeout(() => this.#beginHeldDrag(), this.#holdDelay);
            return;
        }

        // Captured on the container, so the gesture outlasts the pressed element going away.
        capture(this.#element, event.pointerId);
    }

    /** Lifts a held touch straight into a drag. */
    #beginHeldDrag() {
        this.#holdTimer = null;
        if (!this.#subject) {
            return;
        }

        capture(this.#element, this.#pointerId);
        this.#begin();
    }

    #handlePointerMove(event) {
        if (!this.#subject || event.pointerId !== this.#pointerId) {
            return;
        }

        // A press that travels before the hold elapses is a swipe, left to the browser to scroll.
        if (this.#holdTimer !== null) {
            if (this.#getTravelledDistance(event) >= this.#threshold) {
                this.#release();
            }

            return;
        }

        this.#lastX = event.clientX;
        this.#lastY = event.clientY;

        if (!this.#isDragging) {
            if (this.#getTravelledDistance(event) < this.#threshold) {
                return;
            }

            this.#begin();
        }

        event.preventDefault();
        this.#onMove?.(this.#context());
        this.#updateAutoScroll();
    }

    #handlePointerUp(event) {
        if (!this.#subject || event.pointerId !== this.#pointerId) {
            return;
        }

        this.#lastX = event.clientX;
        this.#lastY = event.clientY;

        const context = this.#context();
        const wasDragging = this.#isDragging;
        this.#release();

        if (wasDragging) {
            this.#onDrop?.(context);
        }
    }

    /** How far the pointer has traveled from where the press began, along the axis the gesture drags on. */
    #getTravelledDistance(event) {
        return this.#axis === 'any'
            ? Math.hypot(event.clientX - this.#startX, event.clientY - this.#startY)
            : Math.abs(event.clientY - this.#startY);
    }

    #updateAutoScroll() {
        if (!this.#autoScroll) {
            return;
        }

        const rect = this.#scroller
            ? this.#scroller.getBoundingClientRect()
            : { top: 0, bottom: window.innerHeight };

        if (this.#lastY < rect.top + DragGesture.#ScrollEdge) {
            this.#scrollStep = -DragGesture.#ScrollStep;
        } else if (this.#lastY > rect.bottom - DragGesture.#ScrollEdge) {
            this.#scrollStep = DragGesture.#ScrollStep;
        } else {
            this.#scrollStep = 0;
        }

        if (this.#scrollStep === 0) {
            this.#stopAutoScroll();
        } else if (!this.#scrollScheduler.isPending) {
            this.#scrollScheduler.request(() => this.#scrollTick());
        }
    }

    #scrollTick() {
        (this.#scroller ?? window).scrollBy(0, this.#scrollStep);
        // Content moves under a still pointer, so the caller is told each frame.
        this.#onMove?.(this.#context());
        this.#scrollScheduler.request(() => this.#scrollTick());
    }

    #stopAutoScroll() {
        this.#scrollScheduler.cancel();
        this.#scrollStep = 0;
    }
}

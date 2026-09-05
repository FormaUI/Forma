/**
 * DragGesture — the press-move-release half of a drag, with none of what a drag means
 *
 * Pointer Events rather than HTML5 drag-and-drop, which never fires on touch, so one path
 * covers mouse, touch and pen alike.
 *
 * The handle owns one container: it decides when a press has travelled far enough to be a
 * drag, keeps the pointer captured for the rest of the gesture, scrolls the nearest
 * scrolling ancestor while the pointer is held at an edge, and reports each stage to its
 * caller with the subject, where the pointer is and where the press began. What is being
 * dragged, where it may land, and what landing there means are all the caller's to answer.
 */

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
 *   holdDelay?: number
 * }} options
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

    #subject = null;
    #pointerId = null;
    #startX = 0;
    #startY = 0;
    #lastX = 0;
    #lastY = 0;
    #isDragging = false;
    #holdTimer = null;
    #scroller = null;
    #scrollStep = 0;
    #scrollFrame = null;
    #listeners = new AbortController();

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

        const { signal } = this.#listeners;

        container.addEventListener('pointerdown', event => this.#handlePointerDown(event), { signal });
        container.addEventListener('pointermove', event => this.#handlePointerMove(event), { signal });
        container.addEventListener('pointerup', event => this.#handlePointerUp(event), { signal });
        container.addEventListener('pointercancel', () => this.cancel(), { signal });
        container.addEventListener('keydown', event => this.#handleKeydown(event), { signal });

        // Pointer capture does not stop the compositor scrolling a touch that began on a
        // scrollable area; it cancels the pointer instead. Only refusing the touchmove
        // keeps a drag alive, and only once one is under way.
        container.addEventListener('touchmove', event => {
            if (this.#isDragging) {
                event.preventDefault();
            }
        }, { signal, passive: false });
    }

    /** Stops listening and abandons any gesture in flight. */
    dispose() {
        this.cancel();
        this.#listeners.abort();
    }

    /** Abandons a gesture in flight, reporting it as cancelled rather than dropped. */
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

        if (this.#pointerId !== null && this.#element.hasPointerCapture(this.#pointerId)) {
            this.#element.releasePointerCapture(this.#pointerId);
        }

        this.#subject = null;
        this.#pointerId = null;
        this.#isDragging = false;
    }

    #handlePointerDown(event) {
        if (event.button !== 0 || this.#subject) {
            return;
        }

        const subject = this.#findSubject(event);
        if (!subject) {
            return;
        }

        this.#subject = subject;
        this.#pointerId = event.pointerId;
        this.#startX = event.clientX;
        this.#startY = event.clientY;
        this.#lastX = event.clientX;
        this.#lastY = event.clientY;
        this.#isDragging = false;
        this.#scroller = this.#autoScroll ? DragGesture.#findScroller(this.#element) : null;

        // A touch that could equally have been a scroll has to rest before it lifts,
        // otherwise every swipe across the container would drag instead of scrolling.
        // Capture waits too, so an abandoned press still reaches the browser as a scroll.
        if (this.#holdDelay > 0 && event.pointerType === 'touch') {
            this.#holdTimer = setTimeout(() => this.#beginHeldDrag(), this.#holdDelay);
            return;
        }

        // Captured on the container rather than whatever was pressed, so the gesture
        // survives both the pointer leaving that element and the element itself going away.
        this.#element.setPointerCapture(event.pointerId);
    }

    // A held touch lifts straight into a drag: the rest has already made the intent clear,
    // so it does not also have to travel the threshold.
    #beginHeldDrag() {
        this.#holdTimer = null;
        if (!this.#subject) {
            return;
        }

        this.#element.setPointerCapture(this.#pointerId);
        this.#isDragging = true;
        this.#onStart?.(this.#context());
    }

    #handlePointerMove(event) {
        if (!this.#subject || event.pointerId !== this.#pointerId) {
            return;
        }

        // Moving before the hold elapses means the press was a swipe, so it is abandoned
        // and the browser is left to scroll with it.
        if (this.#holdTimer !== null) {
            if (this.#travelled(event) >= this.#threshold) {
                this.#release();
            }

            return;
        }

        this.#lastX = event.clientX;
        this.#lastY = event.clientY;

        if (!this.#isDragging) {
            if (this.#travelled(event) < this.#threshold) {
                return;
            }

            this.#isDragging = true;
            this.#onStart?.(this.#context());
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

    /** How far the pointer has travelled from where the press began, along the axis the gesture drags on. */
    #travelled(event) {
        return this.#axis === 'any'
            ? Math.hypot(event.clientX - this.#startX, event.clientY - this.#startY)
            : Math.abs(event.clientY - this.#startY);
    }

    #handleKeydown(event) {
        if (event.key === 'Escape' && this.#isDragging) {
            event.preventDefault();
            this.cancel();
        }
    }

    /** The container or nearest ancestor that scrolls vertically, or null when the page itself does. */
    static #findScroller(element) {
        for (let candidate = element; candidate; candidate = candidate.parentElement) {
            const overflow = getComputedStyle(candidate).overflowY;
            if ((overflow === 'auto' || overflow === 'scroll' || overflow === 'overlay')
                && candidate.scrollHeight > candidate.clientHeight) {
                return candidate;
            }
        }

        return null;
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
        } else if (this.#scrollFrame === null) {
            this.#scrollFrame = requestAnimationFrame(() => this.#scrollTick());
        }
    }

    #scrollTick() {
        (this.#scroller ?? window).scrollBy(0, this.#scrollStep);
        // The pointer can sit still while content moves under it, so the caller is asked to
        // re-evaluate each frame rather than only on the next pointermove.
        this.#onMove?.(this.#context());
        this.#scrollFrame = requestAnimationFrame(() => this.#scrollTick());
    }

    #stopAutoScroll() {
        if (this.#scrollFrame !== null) {
            cancelAnimationFrame(this.#scrollFrame);
            this.#scrollFrame = null;
        }

        this.#scrollStep = 0;
    }
}

/**
 * Pointer drag gesture
 *
 * The press-move-release half of a drag, with none of what a drag means. Pointer Events
 * rather than HTML5 drag-and-drop, which never fires on touch, so one path covers mouse,
 * touch and pen alike.
 *
 * An instance owns one container: it decides when a press has travelled far enough to be
 * a drag, keeps the pointer captured for the rest of the gesture, scrolls the nearest
 * scrolling ancestor while the pointer is held at an edge, and reports each stage to its
 * caller. What is being dragged, where it may land, and what landing there means are all
 * the caller's to answer.
 */
export class DragGesture {
    /** How far a pointer travels before a press counts as a drag rather than a click. */
    static #DEFAULT_THRESHOLD = 4;

    /** Distance from a scrolling edge at which a held drag starts scrolling. */
    static #SCROLL_EDGE = 48;

    /** Pixels scrolled per frame while a drag is held at an edge. */
    static #SCROLL_STEP = 8;

    #element;
    #findSubject;
    #onStart;
    #onMove;
    #onDrop;
    #onCancel;
    #threshold;
    #autoScroll;

    #subject = null;
    #pointerId = null;
    #startY = 0;
    #lastX = 0;
    #lastY = 0;
    #isDragging = false;
    #scroller = null;
    #scrollStep = 0;
    #scrollFrame = null;
    #listeners = null;

    /**
     * @param {HTMLElement} container
     * @param {{
     *   findSubject: (event: PointerEvent) => Element | null,
     *   onStart?: (context: object) => void,
     *   onMove?: (context: object) => void,
     *   onDrop?: (context: object) => void,
     *   onCancel?: (context: object) => void,
     *   threshold?: number,
     *   autoScroll?: boolean
     * }} options
     */
    constructor(container, options) {
        this.#element = container;
        this.#findSubject = options.findSubject;
        this.#onStart = options.onStart;
        this.#onMove = options.onMove;
        this.#onDrop = options.onDrop;
        this.#onCancel = options.onCancel;
        this.#threshold = options.threshold ?? DragGesture.#DEFAULT_THRESHOLD;
        this.#autoScroll = options.autoScroll ?? true;
    }

    /**
     * Builds a gesture bound to one container.
     * @param {HTMLElement} container
     * @param {object} options
     */
    static create(container, options) {
        return new DragGesture(container, options);
    }

    /** Begins listening for presses on the container. */
    attach() {
        this.detach();
        this.#listeners = new AbortController();
        const { signal } = this.#listeners;

        this.#element.addEventListener('pointerdown', event => this.#handlePointerDown(event), { signal });
        this.#element.addEventListener('pointermove', event => this.#handlePointerMove(event), { signal });
        this.#element.addEventListener('pointerup', event => this.#handlePointerUp(event), { signal });
        this.#element.addEventListener('pointercancel', () => this.cancel(), { signal });
        this.#element.addEventListener('keydown', event => this.#handleKeydown(event), { signal });
    }

    /** Stops listening and abandons any gesture in flight. */
    detach() {
        this.cancel();
        this.#listeners?.abort();
        this.#listeners = null;
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
        return { subject: this.#subject, clientX: this.#lastX, clientY: this.#lastY };
    }

    #release() {
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
        this.#startY = event.clientY;
        this.#lastX = event.clientX;
        this.#lastY = event.clientY;
        this.#isDragging = false;
        this.#scroller = this.#autoScroll ? DragGesture.#findScroller(this.#element) : null;

        // Captured on the container rather than whatever was pressed, so the gesture
        // survives both the pointer leaving that element and the element itself going away.
        this.#element.setPointerCapture(event.pointerId);
    }

    #handlePointerMove(event) {
        if (!this.#subject || event.pointerId !== this.#pointerId) {
            return;
        }

        this.#lastX = event.clientX;
        this.#lastY = event.clientY;

        if (!this.#isDragging) {
            if (Math.abs(event.clientY - this.#startY) < this.#threshold) {
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

        if (this.#lastY < rect.top + DragGesture.#SCROLL_EDGE) {
            this.#scrollStep = -DragGesture.#SCROLL_STEP;
        } else if (this.#lastY > rect.bottom - DragGesture.#SCROLL_EDGE) {
            this.#scrollStep = DragGesture.#SCROLL_STEP;
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

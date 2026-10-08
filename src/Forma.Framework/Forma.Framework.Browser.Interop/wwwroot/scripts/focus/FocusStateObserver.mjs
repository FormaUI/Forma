/**
 * FocusStateObserver — whether the reader is within a surface
 */

/** @typedef {{ isWithin: boolean }} FocusState */

export class FocusStateObserver {
    #target;
    #publish;
    #abort = new AbortController();
    #isWithin;
    #last = null;
    /** Whether the last press on the page landed inside the surface. */
    #pressedInside = false;
    /** Whether a press inside the surface dropped the focus to the page, with the reader still within it. */
    #fellInside = false;

    /**
     * @param {HTMLElement} target The surface.
     * @param {object} options The observation's options.
     * @param {(state: FocusState) => void} publish Publishes each changed state.
     */
    constructor(target, options, publish) {
        this.#target = target;
        this.#publish = publish;

        const page = target.ownerDocument;
        const signal = this.#abort.signal;
        this.#isWithin = target.contains(page.activeElement);

        // A press inside the surface on something that takes no focus, a label or a gap, drops the focus to the page with the reader
        // still within it, until the next press or focus lands outside.
        page.addEventListener('pointerdown', event => {
            this.#pressedInside = target.contains(event.target);

            if (this.#fellInside && !this.#pressedInside) {
                this.#move(false);
            }
        }, { signal, capture: true });

        page.addEventListener('focusin', event => {
            if (target.contains(event.target)) {
                this.#move(true);
            } else if (this.#fellInside) {
                this.#move(false);
            }
        }, { signal, capture: true });

        target.addEventListener('focusout', event => {
            if (event.relatedTarget !== null) {
                if (!target.contains(event.relatedTarget)) {
                    this.#move(false);
                }

                return;
            }

            // A focus going nowhere fell to the page, went with its element, or left the window, and only the first leaves the surface;
            // which it was shows once the move has settled.
            const element = event.target;

            setTimeout(() => {
                if (signal.aborted || !element.isConnected || !page.hasFocus() || target.contains(page.activeElement)) {
                    return;
                }

                if (this.#pressedInside) {
                    this.#fellInside = true;
                } else {
                    this.#move(false);
                }
            });
        }, { signal });
    }

    /** Reads whether the reader is within the surface now. */
    read() {
        this.#last = { isWithin: this.#isWithin };

        return this.#last;
    }

    /** Publishes whether the reader is within the surface again. */
    refresh() {
        this.#last = null;
        this.#take(this.#isWithin);
    }

    dispose() {
        this.#abort.abort();
    }

    /** Takes the reader into or out of the surface. */
    #move(isWithin) {
        this.#fellInside = false;
        this.#take(isWithin);
    }

    #take(isWithin) {
        this.#isWithin = isWithin;

        if (this.#abort.signal.aborted || this.#last?.isWithin === isWithin) {
            return;
        }

        this.#last = { isWithin };
        this.#publish(this.#last);
    }
}

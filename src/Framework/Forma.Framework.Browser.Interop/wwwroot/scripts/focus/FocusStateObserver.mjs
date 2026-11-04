/**
 * FocusStateObserver — whether the reader is within a surface, and which of its items holds the focus
 */

/** @typedef {{ isWithin: boolean, item?: string | null }} FocusState */

export class FocusStateObserver {
    /** Every state is delivered as it is measured, since what the reader does next acts on where the focus went. */
    static deliversEveryState = true;

    #target;
    #publish;
    #itemAttribute;
    #abort = new AbortController();
    #isWithin;
    /** The item attribute's value on the item holding the focus. */
    #item = null;
    #last = null;
    /** Whether the last press on the page landed inside the surface. */
    #pressedInside = false;
    /** Whether a press inside the surface dropped the focus to the page, with the reader still within it. */
    #fellInside = false;

    /**
     * @param {HTMLElement} target The surface.
     * @param {{ itemAttribute?: string | null }} options The attribute the surface's items carry, whose value names the item holding the focus.
     * @param {(state: FocusState) => void} publish Publishes each changed state.
     */
    constructor(target, { itemAttribute = null } = {}, publish) {
        this.#target = target;
        this.#publish = publish;
        this.#itemAttribute = itemAttribute;

        const page = target.ownerDocument;
        const signal = this.#abort.signal;
        this.#isWithin = target.contains(page.activeElement);
        this.#item = this.#isWithin ? this.#findItem(page.activeElement) : null;

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
                this.#move(true, this.#findItem(event.target));
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
                    // The reader is still within the surface, with no item of it holding the focus.
                    this.#fellInside = true;
                    this.#take(true, null);
                } else {
                    this.#move(false);
                }
            });
        }, { signal });
    }

    /** Reads whether the reader is within the surface now, and the item holding the focus. */
    read() {
        this.#last = this.#describe(this.#isWithin, this.#item);

        return this.#last;
    }

    /** Publishes whether the reader is within the surface again. */
    refresh() {
        this.#last = null;
        this.#take(this.#isWithin, this.#item);
    }

    dispose() {
        this.#abort.abort();
    }

    /** Takes the reader into or out of the surface, with the item holding the focus. */
    #move(isWithin, item = null) {
        this.#fellInside = false;
        this.#take(isWithin, item);
    }

    #take(isWithin, item) {
        this.#isWithin = isWithin;
        this.#item = item;

        if (this.#abort.signal.aborted || (this.#last?.isWithin === isWithin && (this.#last.item ?? null) === item)) {
            return;
        }

        this.#last = this.#describe(isWithin, item);
        this.#publish(this.#last);
    }

    // The state names the item only where the observation asks for items.
    #describe(isWithin, item) {
        return this.#itemAttribute === null ? { isWithin } : { isWithin, item };
    }

    // The nearest item of the surface around the element, by the attribute its items carry.
    #findItem(element) {
        if (this.#itemAttribute === null || !(element instanceof Element)) {
            return null;
        }

        const item = element.closest(`[${this.#itemAttribute}]`);

        return item !== null && this.#target.contains(item) ? item.getAttribute(this.#itemAttribute) : null;
    }
}

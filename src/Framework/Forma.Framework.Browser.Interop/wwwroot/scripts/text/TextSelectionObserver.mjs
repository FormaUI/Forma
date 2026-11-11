/**
 * TextSelectionObserver — the range a text field's selection spans
 */

/** @typedef {{ start: number, end: number }} TextSelection */

export class TextSelectionObserver {
    #field;
    #publish;
    #abort = new AbortController();
    /** @type {TextSelection | null} */
    #lastSelection = null;

    /**
     * @param {HTMLInputElement | HTMLTextAreaElement} field The field observed.
     * @param {object} options The observation's options.
     * @param {(state: TextSelection) => void} publish Publishes each changed state.
     */
    constructor(field, options, publish) {
        this.#field = field;
        this.#publish = publish;

        const signal = this.#abort.signal;
        const page = field.ownerDocument;

        // A browser that reports a field's selection on the field fires it there, and one that doesn't reports it on the document while
        // the field holds the focus; a selection made by a drag or select-all also fires select.
        field.addEventListener('selectionchange', () => this.#take(), { signal });
        field.addEventListener('select', () => this.#take(), { signal });
        page.addEventListener('selectionchange', () => {
            if (page.activeElement === field) {
                this.#take();
            }
        }, { signal });
    }

    /** Reads the range the selection spans now. */
    read() {
        this.#lastSelection = this.#measure();

        return this.#lastSelection;
    }

    /** Publishes the range again. */
    refresh() {
        this.#lastSelection = null;
        this.#take();
    }

    dispose() {
        this.#abort.abort();
    }

    // UTF-16 code units, as the field's text counts them.
    #measure() {
        return { start: this.#field.selectionStart ?? 0, end: this.#field.selectionEnd ?? 0 };
    }

    #take() {
        const state = this.#measure();

        if (this.#abort.signal.aborted || (this.#lastSelection?.start === state.start && this.#lastSelection?.end === state.end)) {
            return;
        }

        this.#lastSelection = state;
        this.#publish(state);
    }
}

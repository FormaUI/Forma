/**
 * TextSources — the text an element's edits read and write
 */

/**
 * @typedef {object} TextSource
 * @property {() => string} read The text the element holds.
 * @property {(text: string) => void} write Writes a text of the owner's own into the element.
 * @property {() => boolean} holdsFocus Whether the reader is in the element.
 * @property {(onInput: () => void, onCommit: () => void, signal: AbortSignal) => void} listen Hears each input and each commit the
 *   element makes, until the signal aborts.
 * @property {boolean} commitsUnchanged Whether a commit counts though the text stands as it was sent, as a field's change does.
 */

/** The sources the scripts owning an element's text provide for it. */
const providedSources = new WeakMap();

export class TextSources {
    /**
     * Provides the source of an element whose script owns its text, as an editor's canvas is, until the handle is disposed.
     * @param {Element} element
     * @param {TextSource} source
     */
    static provide(element, source) {
        providedSources.set(element, source);

        return {
            dispose() {
                if (providedSources.get(element) === source) {
                    providedSources.delete(element);
                }
            },
        };
    }

    /**
     * The source of an element's text: the one its script provides, or a field's own value.
     * @param {Element} element
     * @returns {TextSource}
     */
    static of(element) {
        return providedSources.get(element) ?? new FieldSource(element);
    }
}

/** A text field's value, which it commits as it reports a change. */
class FieldSource {
    #field;

    /** @param {HTMLInputElement | HTMLTextAreaElement} field */
    constructor(field) {
        this.#field = field;
    }

    get commitsUnchanged() {
        return true;
    }

    read() {
        return this.#field.value;
    }

    write(text) {
        this.#field.value = text;
    }

    holdsFocus() {
        return this.#field.ownerDocument.activeElement === this.#field;
    }

    listen(onInput, onCommit, signal) {
        this.#field.addEventListener('input', onInput, { signal });
        this.#field.addEventListener('change', onCommit, { signal });
    }
}

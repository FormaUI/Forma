/**
 * CharacterCount — a text field's running count of its characters, kept in the browser as the reader types
 */

export class CharacterCount {
    #field;
    #output;
    #maxLength;
    #controller = new AbortController();

    /**
     * @param {HTMLInputElement | HTMLTextAreaElement} field The field counted.
     * @param {{ output: string, maxLength?: number | null }} options The id of the element whose text shows the count, and the most the field accepts.
     */
    constructor(field, { output, maxLength = null }) {
        this.#field = field;
        this.#output = output;
        this.#maxLength = maxLength;
        field.addEventListener('input', () => this.show(), { signal: this.#controller.signal });

        // What was typed before the count attached is counted at once.
        this.show();
    }

    /**
     * Shows the count of the text the field holds: UTF-16 code units, which the browser's own maxlength counts, written into the output's
     * own text so the renderer that drew it goes on updating the same node.
     */
    show() {
        const text = document.getElementById(this.#output)?.firstChild;

        if (!(text instanceof Text)) {
            return;
        }

        const count = String(this.#field.value.length);
        text.nodeValue = this.#maxLength === null ? count : `${count} / ${this.#maxLength}`;
    }

    dispose() {
        this.#controller.abort();
    }
}

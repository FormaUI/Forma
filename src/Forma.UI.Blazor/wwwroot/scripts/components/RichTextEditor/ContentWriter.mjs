/**
 * ContentWriter — putting new content into the canvas at the selection the author last made.
 */

export class ContentWriter {
    #canvas;
    #memory;

    constructor(canvas, memory) {
        this.#canvas = canvas;
        this.#memory = memory;
    }

    /** Inserts markup over the remembered selection, appending at the end when none survives. */
    insertHtml(html) {
        if (!html) {
            return;
        }

        this.#memory.restore();
        const range = this.#usableRange();
        const template = document.createElement('template');
        template.innerHTML = html;

        if (!range) {
            this.#canvas.element.appendChild(template.content);
            this.#memory.save();
            return;
        }

        range.deleteContents();
        const fragment = template.content;
        const lastNode = fragment.lastChild;
        range.insertNode(fragment);
        if (lastNode) {
            range.setStartAfter(lastNode);
            range.collapse(true);
            const selection = window.getSelection();
            selection.removeAllRanges();
            selection.addRange(range);
        }
        this.#memory.save();
    }

    /** Inserts plain text over the remembered selection, appending at the end when none survives. */
    insertText(text) {
        if (!text) {
            return;
        }

        this.#memory.restore();
        const range = this.#usableRange();
        const node = document.createTextNode(text);

        if (!range) {
            this.#canvas.element.appendChild(node);
            this.#memory.save();
            return;
        }

        range.deleteContents();
        range.insertNode(node);
        range.setStartAfter(node);
        range.collapse(true);
        const selection = window.getSelection();
        selection.removeAllRanges();
        selection.addRange(range);
        this.#memory.save();
    }

    /** The current range, but only when an insert into it would land inside the canvas. */
    #usableRange() {
        const range = this.#canvas.currentRange();

        return range
            && (range.commonAncestorContainer === this.#canvas.element
                || this.#canvas.contains(range.commonAncestorContainer))
            ? range
            : null;
    }
}

/**
 * SelectionMemory — the selection the author last made in the canvas, kept alive across
 * the focus a toolbar press or a dialog takes away.
 */

export class SelectionMemory {
    #canvas;
    #saved = null;
    #frozen = null;

    constructor(canvas) {
        this.#canvas = canvas;
    }

    /** Remembers the current selection, when it lies inside the canvas. */
    save() {
        const range = this.#canvas.currentRange();
        if (!range) {
            return;
        }

        const anchor = window.getSelection()?.anchorNode;
        const element = anchor?.nodeType === Node.TEXT_NODE ? anchor.parentElement : anchor;

        if (element && this.#canvas.contains(element)) {
            this.#saved = range.cloneRange();
        }
    }

    /**
     * Freezes the remembered selection for a dialog about to open. Closing a dialog returns a
     * collapsed caret to the canvas, which would overwrite the memory, so the frozen copy is
     * what the insert that follows consumes.
     */
    freeze() {
        this.#frozen = this.#saved ? this.#saved.cloneRange() : null;

        return this.#frozen;
    }

    /** The range the next edit will land on, without consuming it. */
    peek() {
        return this.#frozen ?? this.#saved;
    }

    /** Makes the remembered selection current again, consuming any frozen copy. */
    restore() {
        const range = this.#frozen ?? this.#saved;
        this.#frozen = null;

        if (!range) {
            return;
        }

        const selection = window.getSelection();
        if (selection) {
            selection.removeAllRanges();
            selection.addRange(range);
        }
    }

    /** Forgets everything, for when the canvas is rewritten and the ranges point at detached nodes. */
    forget() {
        this.#saved = null;
        this.#frozen = null;
    }
}

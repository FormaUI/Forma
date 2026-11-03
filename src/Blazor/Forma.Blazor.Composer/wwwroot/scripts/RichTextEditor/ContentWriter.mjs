/**
 * ContentWriter — putting new content into the canvas at the selection the author last made.
 */

import { Canvas } from './Canvas.mjs';

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
        const insertedCount = fragment.childNodes.length;
        const block = ContentWriter.#holdsBlocks(fragment)
            ? this.#canvas.closest(range.startContainer, ContentWriter.#InlineOnlyBlockSelector)
            : null;

        if (block) {
            const following = this.#canvas.splitBlock(block, range);
            block.after(fragment);
            ContentWriter.#removeIfEmpty(block);

            if (ContentWriter.#removeIfEmpty(following)) {
                range.setStartAfter(lastNode);
            } else {
                // A trailing spacer paragraph is dropped where the following half already stands.
                if (insertedCount > 1 && lastNode.matches?.(ContentWriter.#InlineOnlyBlockSelector)) {
                    ContentWriter.#removeIfEmpty(lastNode);
                }

                range.setStart(following, 0);
            }
        } else {
            range.insertNode(fragment);
            if (lastNode) {
                range.setStartAfter(lastNode);
            }
        }

        if (lastNode) {
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

    /** Blocks that hold only inline content, which a block inserted into them splits. */
    static #InlineOnlyBlockSelector = 'p, h1, h2, h3, h4, h5, h6, pre';

    /** Elements that make a block worth keeping even when it holds no text. */
    static #ContentSelector = 'img, video, audio, iframe, object, embed, hr';

    static #holdsBlocks(fragment) {
        return [...fragment.children].some(child => child.matches(Canvas.BlockLikeSelector));
    }

    /** Removes a block that holds neither text nor media, and answers whether it did. */
    static #removeIfEmpty(block) {
        if (block.textContent.trim() !== '' || block.querySelector(ContentWriter.#ContentSelector)) {
            return false;
        }

        block.remove();
        return true;
    }

    /** The current range, when it lies inside the canvas. */
    #usableRange() {
        const range = this.#canvas.currentRange();

        return range
            && (range.commonAncestorContainer === this.#canvas.element
                || this.#canvas.contains(range.commonAncestorContainer))
            ? range
            : null;
    }
}

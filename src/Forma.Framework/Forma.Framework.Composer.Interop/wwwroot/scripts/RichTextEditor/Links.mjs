/**
 * Links — the anchors a selection touches: finding them, removing them, and building new ones.
 */

import { Escape } from './Markup.mjs';

export class Links {
    #canvas;

    constructor(canvas) {
        this.#canvas = canvas;
    }

    /**
     * Whether the selection touches a matching anchor, by sitting inside one or by overlapping any of them.
     * A selection reaching across two links starts in plain text, so asking only where it begins would
     * report no link and offer to insert one rather than remove what is already there.
     */
    touches(range, selector) {
        if (this.#canvas.closest(range.startContainer, selector)) {
            return true;
        }

        if (range.collapsed) {
            return false;
        }

        for (const candidate of this.#canvas.element.querySelectorAll(selector)) {
            if (range.intersectsNode(candidate)) {
                return true;
            }
        }

        return false;
    }

    /** The matching anchors a range sits inside or overlaps, in document order. */
    touching(range, selector) {
        if (range.collapsed) {
            const found = this.#canvas.closest(range.startContainer, selector);

            return found ? [found] : [];
        }

        return [...this.#canvas.element.querySelectorAll(selector)].filter(candidate => range.intersectsNode(candidate));
    }

    /**
     * Unwraps the matching anchors a range touches and reports how many went.
     * The text nodes move rather than die, so the range still covers the same text afterwards and a
     * caller may go on to wrap it; normalizing here would merge them and lose the range's offsets.
     */
    unwrapTouching(range, selector) {
        const found = this.touching(range, selector);

        for (const anchor of found) {
            anchor.replaceWith(...anchor.childNodes);
        }

        return found.length;
    }

    /**
     * The markup a link or named anchor request builds, or null when the request names nothing to build.
     * The caller supplies whatever text the selection already covered and inserts the result over it.
     */
    static markup(data, selectedText) {
        if (!data) {
            return null;
        }

        const { name = '', href = '', openInNewTab = false, isAnchor = false } = data;

        if (isAnchor) {
            if (!name) {
                return null;
            }

            const anchorAttrs = `id="${Escape.attribute(name)}" name="${Escape.attribute(name)}"`;

            return selectedText
                ? `<a ${anchorAttrs}>${Escape.html(selectedText)}</a>`
                : `<a ${anchorAttrs}></a>`;
        }

        if (!href) {
            return null;
        }

        const text = name || selectedText || href;
        const targetAttr = openInNewTab ? ' target="_blank" rel="noopener noreferrer"' : '';

        return `<a href="${Escape.attribute(href)}"${targetAttr}>${Escape.html(text)}</a>`;
    }
}

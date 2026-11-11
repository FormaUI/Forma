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
    getTouchingLinks(range, selector) {
        if (range.collapsed) {
            const foundLink = this.#canvas.closest(range.startContainer, selector);

            return foundLink ? [foundLink] : [];
        }

        return [...this.#canvas.element.querySelectorAll(selector)].filter(candidate => range.intersectsNode(candidate));
    }

    /**
     * Unwraps the matching anchors a range touches and returns how many it unwrapped.
     * The text nodes move into the anchors' places unnormalized, so the range still covers the same text afterwards.
     */
    unwrapTouching(range, selector) {
        const touchingLinks = this.getTouchingLinks(range, selector);

        for (const anchor of touchingLinks) {
            anchor.replaceWith(...anchor.childNodes);
        }

        return touchingLinks.length;
    }

    /** The markup a link or named anchor request builds around the selected text, where null means there is nothing to insert. */
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

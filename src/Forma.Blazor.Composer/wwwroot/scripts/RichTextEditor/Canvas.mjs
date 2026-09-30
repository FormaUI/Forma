/**
 * Canvas — the editable region itself: what it contains, how its blocks are found,
 * and how a selection inside it is read and placed.
 */

export class Canvas {
    /** The elements a caret treats as its enclosing block. */
    static BlockSelector = 'p, h1, h2, h3, h4, h5, h6, blockquote, pre, li, div';

    /** Everything inline content cannot flow through, so a wrapping pass recurses into these instead. */
    static BlockLikeSelector = `${Canvas.BlockSelector}, table, thead, tbody, tfoot, tr, td, th, caption, ul, ol, figure, figcaption, hr`;

    /** The element a node belongs to, stepping out of a text node onto its parent. */
    static elementOf(node) {
        return node?.nodeType === Node.TEXT_NODE ? node.parentElement : node;
    }

    #element;

    constructor(element) {
        this.#element = element;
    }

    get element() {
        return this.#element;
    }

    get html() {
        return this.#element.innerHTML;
    }

    set html(value) {
        this.#element.innerHTML = value;
    }

    contains(node) {
        return this.#element.contains(node);
    }

    focus() {
        this.#element.focus();
    }

    /** Merges the split text nodes an unwrapping pass leaves behind. */
    normalize() {
        this.#element.normalize();
    }

    /** The nearest matching ancestor, but only while it stays inside the canvas. */
    closest(node, selector) {
        const found = Canvas.elementOf(node)?.closest(selector);

        return found && this.contains(found) ? found : null;
    }

    /** The nearest matching ancestor of the selection's anchor, but only while it stays inside the canvas. */
    closestToSelection(selector) {
        const selection = window.getSelection();

        return selection && selection.rangeCount > 0 ? this.closest(selection.anchorNode, selector) : null;
    }

    /** The current selection's first range, or null when there is none. */
    currentRange() {
        const selection = window.getSelection();

        return selection && selection.rangeCount > 0 ? selection.getRangeAt(0) : null;
    }

    /** The innermost block holding the selection's anchor, falling back to the canvas's first block. */
    closestBlock() {
        const selection = window.getSelection();
        const node = Canvas.elementOf((selection && selection.rangeCount > 0) ? selection.anchorNode : null);

        if (node && this.contains(node) && node !== this.#element) {
            const found = node.closest(Canvas.BlockSelector);
            // The canvas root can itself match the selector; it is the canvas, never a block.
            return found && found !== this.#element ? found : node;
        }

        return this.#element.querySelector(Canvas.BlockSelector) ?? this.#element;
    }

    /** The innermost blocks the selection touches, so a command lands where typing would. */
    selectedBlocks() {
        const range = this.currentRange();
        if (!range) {
            return [];
        }

        if (range.collapsed) {
            const block = this.closestBlock();
            return block && block !== this.#element && block.matches?.(Canvas.BlockSelector) ? [block] : [];
        }

        const touched = [...this.#element.querySelectorAll(Canvas.BlockSelector)]
            .filter(block => range.intersectsNode(block));

        return touched.filter(block => !touched.some(other => other !== block && block.contains(other)));
    }

    /** Moves what follows the range out of its block into a copy standing after it, and returns that copy. */
    splitBlock(block, range) {
        const tail = document.createRange();
        tail.setStart(range.startContainer, range.startOffset);
        tail.setEnd(block, block.childNodes.length);

        const following = block.cloneNode(false);
        following.removeAttribute('id');
        following.append(tail.extractContents());
        block.after(following);

        return following;
    }

    /** Every text node the range covers at least partially, in document order. */
    textNodesIn(range) {
        const root = range.commonAncestorContainer;

        if (root.nodeType === Node.TEXT_NODE) {
            return [root];
        }

        const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
        const nodes = [];

        while (walker.nextNode()) {
            if (range.intersectsNode(walker.currentNode)) {
                nodes.push(walker.currentNode);
            }
        }

        return nodes;
    }

    /** Places the selection so it covers everything between two sibling nodes, inclusive. */
    selectBetween(first, last) {
        if (!first || !last) {
            return;
        }

        const selection = window.getSelection();
        if (!selection) {
            return;
        }

        const range = document.createRange();
        range.setStartBefore(first);
        range.setEndAfter(last);
        selection.removeAllRanges();
        selection.addRange(range);
    }

    /** Places a collapsed caret inside a node, right after the given offset. */
    placeCaret(node, offset) {
        const selection = window.getSelection();
        if (!selection) {
            return;
        }

        const range = document.createRange();
        range.setStart(node, offset);
        range.collapse(true);
        selection.removeAllRanges();
        selection.addRange(range);
    }

    /** The top-level block at a zero-based index, or null past the last one. */
    blockAt(index) {
        return this.#element.children[index] ?? null;
    }

    /** Collapses the caret to the start of an element's content, so what is typed next opens it. */
    placeCaretAtStart(element) {
        const selection = window.getSelection();
        if (!selection) {
            return;
        }

        const caret = document.createRange();
        caret.selectNodeContents(element);
        caret.collapse(true);
        selection.removeAllRanges();
        selection.addRange(caret);
    }

    /** Collapses the caret to the end of an element's content, so what is typed next follows it. */
    placeCaretAtEnd(element) {
        const selection = window.getSelection();
        if (!selection) {
            return;
        }

        const caret = document.createRange();
        caret.selectNodeContents(element);
        caret.collapse(false);
        selection.removeAllRanges();
        selection.addRange(caret);
    }

    /** Selects a whole node, so what replaces the selection replaces that node entirely. */
    selectNode(node) {
        const selection = window.getSelection();
        if (!selection || !this.contains(node)) {
            return;
        }

        const range = document.createRange();
        range.selectNode(node);
        selection.removeAllRanges();
        selection.addRange(range);
    }

    /** Selects the whole canvas. */
    selectAll() {
        const selection = window.getSelection();
        if (!selection) {
            return;
        }

        const everything = document.createRange();
        everything.selectNodeContents(this.#element);
        selection.removeAllRanges();
        selection.addRange(everything);
    }
}

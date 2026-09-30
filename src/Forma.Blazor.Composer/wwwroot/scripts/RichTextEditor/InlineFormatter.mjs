/**
 * InlineFormatter — the inline formatting engine: tag marks, styled spans, and their removal.
 *
 * Each operation extracts the selected range, rewrites the fragment, and reinserts it.
 * Extraction splits any element the selection only partly covers.
 */

import { Canvas } from './Canvas.mjs';
import { Fragments } from './Fragments.mjs';

export class InlineFormatter {
    /** The zero-width space carrying a pending format at a collapsed caret until something is typed into it. */
    static PendingMarker = '\u200B';

    /** An inline wrapper with nothing inside it, as markup. */
    static #EmptyWrapper = /<(b|strong|i|em|u|s|strike|del|sub|sup|code|span)((?:\s[^>]*)?)><\/\1>/g;

    /** Strips pending-format markers and the wrappers armed around them from outgoing HTML. */
    static stripMarkers(html) {
        let out = (html ?? '').replaceAll(InlineFormatter.PendingMarker, '');

        let before;
        do {
            before = out;
            out = out.replace(InlineFormatter.#EmptyWrapper, '');
        } while (out !== before);

        return out;
    }

    /** The tag each mark writes. */
    static #MarkTag = {
        bold: 'b',
        italic: 'i',
        underline: 'u',
        strike: 's',
        subscript: 'sub',
        superscript: 'sup',
        code: 'code',
    };

    /** Every spelling of each mark the canvas may already hold. */
    static #MarkSelector = {
        bold: 'b, strong',
        italic: 'i, em',
        underline: 'u',
        strike: 's, strike, del',
        subscript: 'sub',
        superscript: 'sup',
        code: 'code',
    };

    static #AllMarksSelector = 'b, strong, i, em, u, s, strike, del, sub, sup, code';

    /** The style properties formatting writes, and what a format sweep therefore clears. */
    static #FormattingProperties = [
        'color', 'background-color', 'font-family', 'font-size',
        'font-weight', 'font-style', 'text-decoration', 'line-height',
    ];

    #canvas;

    constructor(canvas) {
        this.#canvas = canvas;
    }

    /** Toggles an inline mark over the selection, or arms it at a collapsed caret for the next typed text. */
    toggle(mark) {
        const tag = InlineFormatter.#MarkTag[mark];
        const selector = InlineFormatter.#MarkSelector[mark];
        const range = this.#canvas.currentRange();

        if (!tag || !range) {
            return;
        }

        if (range.collapsed) {
            this.#toggleAtCaret(range, tag, selector);
            return;
        }

        const active = this.#isFullyMarked(range, selector);

        if (active) {
            this.#isolate(range, holder => holder.matches(selector));
        }

        const fragment = range.extractContents();

        this.#unwrapIn(fragment, selector);

        if (!active) {
            this.#wrapRuns(fragment, () => document.createElement(tag));
        }

        this.#reinsert(range, fragment);
    }

    /** Applies one style property over the selection, or arms it at a collapsed caret; an empty value only clears. */
    applyStyle(property, value) {
        const range = this.#canvas.currentRange();
        if (!range) {
            return;
        }

        if (range.collapsed) {
            if (value) {
                const pending = document.createElement('span');
                pending.style.setProperty(property, value);
                this.#armPending(range, pending);
            }
            return;
        }

        this.#isolate(range, holder => Boolean(holder.style?.getPropertyValue(property)));

        const fragment = range.extractContents();

        for (const styled of fragment.querySelectorAll('[style]')) {
            styled.style.removeProperty(property);
            this.#dropIfBareSpan(styled);
        }

        if (value) {
            this.#wrapRuns(fragment, () => {
                const span = document.createElement('span');
                span.style.setProperty(property, value);
                return span;
            });
        }

        this.#reinsert(range, fragment);
    }

    /** Strips every inline mark and formatting style from the selection. */
    clearFormatting() {
        const range = this.#canvas.currentRange();
        if (!range || range.collapsed) {
            return;
        }

        this.#isolate(range, holder => holder.matches(InlineFormatter.#AllMarksSelector)
            || InlineFormatter.#FormattingProperties.some(property => Boolean(holder.style?.getPropertyValue(property))));

        const fragment = range.extractContents();

        this.#unwrapIn(fragment, InlineFormatter.#AllMarksSelector);

        for (const styled of fragment.querySelectorAll('[style]')) {
            for (const property of InlineFormatter.#FormattingProperties) {
                styled.style.removeProperty(property);
            }
            this.#dropIfBareSpan(styled);
        }

        this.#reinsert(range, fragment);
    }

    /** Removes every pending-format marker, dissolving wrappers nothing was ever typed into. */
    clearPendingMarkers() {
        const walker = document.createTreeWalker(this.#canvas.element, NodeFilter.SHOW_TEXT);
        const carriers = [];

        while (walker.nextNode()) {
            if (walker.currentNode.data.includes(InlineFormatter.PendingMarker)) {
                carriers.push(walker.currentNode);
            }
        }

        for (const node of carriers) {
            node.data = node.data.replaceAll(InlineFormatter.PendingMarker, '');

            let shell = node.parentElement;
            if (node.data === '') {
                node.remove();
            }

            while (shell && shell !== this.#canvas.element
                && shell.childNodes.length === 0
                && shell.matches(`${InlineFormatter.#AllMarksSelector}, span`)) {
                const above = shell.parentElement;
                shell.remove();
                shell = above;
            }
        }
    }

    /**
     * Splits every matching element that wholly contains the range into halves around it,
     * then widens the range to the piece left holding exactly the covered content. Extraction
     * only splits elements between its boundaries, so a container has to be split by hand.
     */
    #isolate(range, matches) {
        let holder = this.#containing(range.commonAncestorContainer, matches);

        while (holder) {
            const tail = document.createRange();
            tail.setStart(range.endContainer, range.endOffset);
            tail.setEnd(holder, holder.childNodes.length);
            const after = tail.extractContents();
            if (Fragments.hasContent(after)) {
                const tailClone = holder.cloneNode(false);
                tailClone.appendChild(after);
                holder.after(tailClone);
            }

            const head = document.createRange();
            head.setStart(holder, 0);
            head.setEnd(range.startContainer, range.startOffset);
            const before = head.extractContents();
            if (Fragments.hasContent(before)) {
                const headClone = holder.cloneNode(false);
                headClone.appendChild(before);
                holder.before(headClone);
            }

            range.selectNode(holder);
            holder = this.#containing(holder.parentNode, matches);
        }
    }

    /** The nearest matching ancestor inside the canvas. */
    #containing(node, matches) {
        for (let element = Canvas.elementOf(node); element && element !== this.#canvas.element; element = element.parentElement) {
            if (matches(element)) {
                return element;
            }
        }

        return null;
    }

    /** Whether every character the range covers already sits inside the mark. */
    #isFullyMarked(range, selector) {
        const meaningful = this.#canvas.textNodesIn(range)
            .filter(node => node.data.replaceAll(InlineFormatter.PendingMarker, '').trim() !== '');

        if (meaningful.length === 0) {
            return this.#canvas.closest(range.startContainer, selector) !== null;
        }

        return meaningful.every(node => this.#canvas.closest(node, selector) !== null);
    }

    /** At a collapsed caret: leave the mark for what is typed next, or step typing out of the one the caret is in. */
    #toggleAtCaret(range, tag, selector) {
        const inside = this.#canvas.closest(range.startContainer, selector);

        if (!inside) {
            this.#armPending(range, document.createElement(tag));
            return;
        }

        // The rest of the mark past the caret moves into a clone after it, and typing resumes between the halves.
        const tail = document.createRange();
        tail.setStart(range.startContainer, range.startOffset);
        tail.setEnd(inside, inside.childNodes.length);
        const rest = tail.extractContents();

        // The caret may have sat inside further marks nested in this one; those survive for the next typed text.
        const nested = [];
        for (let node = Canvas.elementOf(range.startContainer); node && node !== inside; node = node.parentElement) {
            if (node.matches(InlineFormatter.#AllMarksSelector)) {
                nested.push(node.cloneNode(false));
            }
        }

        const marker = document.createTextNode(InlineFormatter.PendingMarker);
        let outermost = marker;
        for (const wrapper of nested) {
            wrapper.appendChild(outermost);
            outermost = wrapper;
        }

        inside.after(outermost);

        if (Fragments.hasContent(rest)) {
            const tailClone = inside.cloneNode(false);
            tailClone.appendChild(rest);
            outermost.after(tailClone);
        }

        if (inside.childNodes.length === 0) {
            inside.remove();
        }

        this.#canvas.placeCaret(marker, 1);
    }

    /** Arms a wrapper at the caret so the next typed text lands inside it. */
    #armPending(range, wrapper) {
        const marker = document.createTextNode(InlineFormatter.PendingMarker);
        wrapper.appendChild(marker);
        range.insertNode(wrapper);
        this.#canvas.placeCaret(marker, 1);
    }

    /** Unwraps every element the selector matches inside a fragment, keeping its content. */
    #unwrapIn(root, selector) {
        for (const found of root.querySelectorAll(selector)) {
            found.replaceWith(...found.childNodes);
        }
    }

    /** Drops a span that lost its last style and carries nothing else, keeping its content. */
    #dropIfBareSpan(element) {
        if (element.tagName === 'SPAN' && !element.getAttribute('style')?.trim() && element.attributes.length <= 1) {
            element.replaceWith(...element.childNodes);
        }
    }

    /**
     * Wraps each run of consecutive inline content in a fresh wrapper, recursing into anything
     * block-like, so a selection spanning paragraphs marks the text inside each block.
     */
    #wrapRuns(container, makeWrapper) {
        let run = [];

        const flush = () => {
            const meaningful = run.some(node => node.nodeType !== Node.TEXT_NODE || node.data.length > 0);
            if (run.length > 0 && meaningful) {
                const wrapper = makeWrapper();
                container.insertBefore(wrapper, run[0]);
                for (const node of run) {
                    wrapper.appendChild(node);
                }
            }
            run = [];
        };

        for (const child of [...container.childNodes]) {
            if (child.nodeType === Node.ELEMENT_NODE && child.matches(Canvas.BlockLikeSelector)) {
                flush();
                this.#wrapRuns(child, makeWrapper);
            } else {
                run.push(child);
            }
        }

        flush();
    }

    /** Puts a rewritten fragment back where it came from, selects it, and sweeps the shells extraction left empty. */
    #reinsert(range, fragment) {
        const first = fragment.firstChild;
        const last = fragment.lastChild;

        range.insertNode(fragment);

        const sweep = `${InlineFormatter.#AllMarksSelector}, span, ${Canvas.BlockSelector}`;
        for (const shell of this.#canvas.element.querySelectorAll(sweep)) {
            if (shell.textContent === '' && !shell.querySelector('br, img, hr')) {
                shell.remove();
            }
        }

        this.#canvas.selectBetween(first, last);
    }
}

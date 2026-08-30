/**
 * SelectionInspector — what the toolbar needs to know about the current selection,
 * read from the markup itself.
 */

import { Canvas } from './Canvas.mjs';
import { CssColor } from './Markup.mjs';

export class SelectionInspector {
    /** Which marks light which toolbar states. */
    static #MarkStates = [
        ['b, strong', 'bold'],
        ['i, em', 'italic'],
        ['u', 'underline'],
        ['s, strike, del', 'strike'],
        ['sub', 'subscript'],
        ['sup', 'superscript'],
    ];

    /** Which explicit inline styles report which values, and how their spelling normalizes. */
    static #ValueStates = [
        ['font-family', 'font-family', value => value.replaceAll('"', '')],
        ['color', 'text-color', value => CssColor.toHex(value)],
        ['background-color', 'highlight-color', value => CssColor.toHex(value)],
        ['font-size', 'font-size', value => value],
    ];

    #canvas;
    #links;

    constructor(canvas, links) {
        this.#canvas = canvas;
        this.#links = links;
    }

    /** Describes the selection for the toolbar, or null when it sits outside the canvas. */
    snapshot() {
        const range = this.#canvas.currentRange();
        // A range across marked text anchors on the block around it; the first text it covers is what carries the marks.
        const focus = range && !range.collapsed && range.commonAncestorContainer ? (this.#canvas.textNodesIn(range)[0] ?? range.startContainer) : window.getSelection()?.anchorNode;
        const anchor = Canvas.elementOf(focus);

        if (!range || !anchor || !this.#canvas.contains(anchor)) {
            return null;
        }

        const block = anchor.closest(Canvas.BlockSelector);
        const blockType = block ? block.tagName.toLowerCase() : 'p';
        const activeStyles = [];

        for (const [selector, name] of SelectionInspector.#MarkStates) {
            if (this.#canvas.closest(anchor, selector)) {
                activeStyles.push(name);
            }
        }

        const list = this.#canvas.closest(anchor, 'ul, ol');
        if (list) {
            activeStyles.push(list.tagName === 'OL' ? 'ordered-list' : 'bullet-list');
        }

        if (block) {
            activeStyles.push(...this.#alignmentOf(block));
        }

        if (anchor.closest('code')) {
            activeStyles.push('code');
        }

        if (this.#links.touches(range, 'a[href]')) {
            activeStyles.push('link');
        }

        if (this.#links.touches(range, 'a[name]')) {
            activeStyles.push('anchor');
        }

        if (block?.classList.contains('forma-composer-lead')) {
            activeStyles.push('lead');
        }

        const dir = block?.getAttribute('dir');
        if (dir) {
            activeStyles.push(`dir-${dir}`);
        }

        activeStyles.push(blockType);

        for (const [property, name, normalize] of SelectionInspector.#ValueStates) {
            const value = this.#inlineValue(anchor, property);
            if (value) {
                activeStyles.push(`${name}:${normalize(value)}`);
            }
        }

        const lineHeight = block?.style.lineHeight;
        if (lineHeight) {
            activeStyles.push(`line-height:${lineHeight}`);
        }

        const preRange = document.createRange();
        preRange.selectNodeContents(this.#canvas.element);
        preRange.setEnd(range.startContainer, range.startOffset);
        const start = preRange.toString().length;
        const end = start + range.toString().length;

        return {
            start,
            end,
            isCollapsed: range.collapsed,
            blockType,
            activeStyles,
        };
    }

    /** Which alignment state a block reports, resolving start and end through its direction. */
    #alignmentOf(block) {
        const style = window.getComputedStyle(block);
        const alignment = style.textAlign === 'start' ? (style.direction === 'rtl' ? 'right' : 'left')
            : style.textAlign === 'end' ? (style.direction === 'rtl' ? 'left' : 'right')
            : style.textAlign;
        const alignmentStyles = { left: 'align-left', center: 'align-center', right: 'align-right', justify: 'align-justify' };

        return alignmentStyles[alignment] ? [alignmentStyles[alignment]] : [];
    }

    /** The nearest explicitly set inline value for a property, walking outward to the canvas. */
    #inlineValue(anchor, property) {
        for (let node = anchor; node && node !== this.#canvas.element; node = node.parentElement) {
            const value = node.style?.getPropertyValue(property);
            if (value) {
                return value;
            }
        }

        return '';
    }
}

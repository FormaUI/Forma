/**
 * BlockFormatter — commands acting on whole blocks: conversion, alignment, presentation,
 * and the rule that splits one in two.
 */

import { Canvas } from './Canvas.mjs';

export class BlockFormatter {
    #canvas;

    constructor(canvas) {
        this.#canvas = canvas;
    }

    /** Converts the touched blocks to the named tag, leaving list items to keep their lists whole. */
    convert(value) {
        const name = String(value ?? 'p').replace(/[<>]/g, '').toLowerCase();
        const blocks = this.#canvas.selectedBlocks().filter(block => block.tagName !== 'LI');
        let last = null;

        for (const block of blocks) {
            if (block.tagName.toLowerCase() === name) {
                continue;
            }

            const replacement = document.createElement(name);
            if (block.dir) {
                replacement.dir = block.dir;
            }
            while (block.firstChild) {
                replacement.appendChild(block.firstChild);
            }
            block.replaceWith(replacement);
            last = replacement;
        }

        // Converting an empty block detaches the node the caret sat on, so the caret moves into the replacement.
        const selection = window.getSelection();
        if (last && selection && (!selection.anchorNode || !selection.anchorNode.isConnected)) {
            const caret = document.createRange();
            caret.selectNodeContents(last);
            caret.collapse(false);
            selection.removeAllRanges();
            selection.addRange(caret);
        }
    }

    /** Aligns every touched block explicitly, the way typing over them would find them. */
    align(value) {
        for (const block of this.#canvas.selectedBlocks()) {
            block.style.textAlign = value;
        }
    }

    toggleLead() {
        const block = this.#canvas.closestBlock();
        if (block && this.#canvas.contains(block)) {
            block.classList.toggle('forma-composer-lead');
        }
    }

    setLineHeight(value) {
        const block = this.#canvas.closestBlock();
        if (block && this.#canvas.contains(block)) {
            if (value) {
                block.style.lineHeight = value;
            } else {
                block.style.removeProperty('line-height');
            }
        }
    }

    setDirection(value) {
        const block = this.#canvas.closestBlock();
        if (block && this.#canvas.contains(block)) {
            if (value === 'ltr' || value === 'rtl') {
                block.setAttribute('dir', value);
            } else {
                block.removeAttribute('dir');
            }
        }
    }

    /** Splits the caret's block and stands a rule between the halves rather than nesting it inside one. */
    insertRule() {
        const rule = document.createElement('hr');
        const range = this.#canvas.currentRange();

        if (!range) {
            this.#canvas.element.appendChild(rule);
            return;
        }

        range.deleteContents();

        const node = Canvas.elementOf(range.startContainer);
        const block = node && node !== this.#canvas.element && this.#canvas.contains(node)
            ? node.closest(Canvas.BlockSelector)
            : null;

        if (!block || !this.#canvas.contains(block)) {
            range.insertNode(rule);
            return;
        }

        const tail = document.createRange();
        tail.setStart(range.startContainer, range.startOffset);
        tail.setEnd(block, block.childNodes.length);

        const following = block.cloneNode(false);
        following.appendChild(tail.extractContents());
        if (!following.hasChildNodes()) {
            following.appendChild(document.createElement('br'));
        }
        if (!block.hasChildNodes()) {
            block.appendChild(document.createElement('br'));
        }
        block.after(rule, following);

        this.#canvas.placeCaret(following, 0);
    }
}

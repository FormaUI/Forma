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
        const range = this.#canvas.currentRange();
        const kept = range ? { start: range.startContainer, startOffset: range.startOffset, end: range.endContainer, endOffset: range.endOffset } : null;
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

        const selection = window.getSelection();
        if (!last || !selection) {
            return;
        }

        // The text the selection covered moved whole into the replacement, so the selection is put back over it.
        if (kept && kept.start.isConnected && kept.end.isConnected) {
            const restored = document.createRange();
            restored.setStart(kept.start, kept.startOffset);
            restored.setEnd(kept.end, kept.endOffset);
            selection.removeAllRanges();
            selection.addRange(restored);
            return;
        }

        // Converting an empty block detaches the node the caret sat on, so the caret moves into the replacement.
        if (!selection.anchorNode || !selection.anchorNode.isConnected) {
            this.#canvas.placeCaretAtEnd(last);
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

        const block = this.#canvas.closest(range.startContainer, Canvas.BlockSelector);

        if (!block || block === this.#canvas.element) {
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

/**
 * RichTextEditor — the visual contenteditable component: lifecycle, events, and command dispatch
 * over the editing modules under RichTextEditor/.
 */

import { sanitize } from '../../forma/core/scripts/Sanitizer.js';
import { Canvas } from './RichTextEditor/Canvas.mjs';
import { SelectionMemory } from './RichTextEditor/SelectionMemory.mjs';
import { InlineFormatter } from './RichTextEditor/InlineFormatter.mjs';
import { BlockFormatter } from './RichTextEditor/BlockFormatter.mjs';
import { ListFormatter } from './RichTextEditor/ListFormatter.mjs';
import { Links } from './RichTextEditor/Links.mjs';
import { TableEditor } from './RichTextEditor/TableEditor.mjs';
import { MediaMarkup } from './RichTextEditor/MediaMarkup.mjs';
import { ContentWriter } from './RichTextEditor/ContentWriter.mjs';
import { SelectionInspector } from './RichTextEditor/SelectionInspector.mjs';

export function attach(target, options = {}) {
    return new RichTextEditor(target, options);
}

class RichTextEditor {
    /** Which callback each reopened kind reports to. */
    static #Activations = {
        image: 'HandleImageActivatedAsync',
        video: 'HandleVideoActivatedAsync',
        embed: 'HandleEmbedActivatedAsync',
    };

    #canvas;
    #memory;
    #inline;
    #blocks;
    #lists;
    #links;
    #tables;
    #writer;
    #inspector;

    #owner;
    #debounceDelay;
    #debounceTimer = null;
    #controller = new AbortController();
    #isDisposed = false;
    #lastHtml = null;

    /** What the owner was last told of the selection: the block, the styles and whether it is a caret. */
    #lastSelectionKey = null;

    constructor(target, { owner, debounceDelay = 400 }) {
        this.#canvas = new Canvas(target);
        this.#memory = new SelectionMemory(this.#canvas);
        this.#inline = new InlineFormatter(this.#canvas);
        this.#blocks = new BlockFormatter(this.#canvas);
        this.#lists = new ListFormatter(this.#canvas);
        this.#links = new Links(this.#canvas);
        this.#tables = new TableEditor(this.#canvas);
        this.#writer = new ContentWriter(this.#canvas, this.#memory);
        this.#inspector = new SelectionInspector(this.#canvas, this.#links);

        this.#owner = owner;
        this.#debounceDelay = debounceDelay;
        this.#lastHtml = InlineFormatter.stripMarkers(target.innerHTML);

        const { signal } = this.#controller;

        target.addEventListener('input', () => this.#handleInput(), { signal });
        target.addEventListener('blur', () => this.#handleBlur(), { signal });
        target.addEventListener('click', () => this.#reportSelection(), { signal });
        target.addEventListener('keyup', () => this.#reportSelection(), { signal });
        target.addEventListener('dblclick', event => this.#handleDoubleClick(event), { signal });

        document.addEventListener('selectionchange', () => this.#handleSelectionChange(), { signal });
    }

    /**
     * Opens whatever the author double-clicked in the dialog that built it, selecting the element
     * whole first so that confirming the dialog replaces it.
     */
    #handleDoubleClick(event) {
        const from = Canvas.elementOf(event.target);

        if (!from || !this.#canvas.contains(from)) {
            return;
        }

        const found = MediaMarkup.Kinds
            .map(({ kind, selector }) => ({ kind, element: from.closest(selector) }))
            .find(({ element }) => element && this.#canvas.contains(element));

        if (!found) {
            return;
        }

        // Replaces the word a double-click selects with the whole element.
        event.preventDefault();
        this.#canvas.selectNode(found.element);
        this.#memory.save();

        // A closing dialog returns a collapsed caret to the canvas; freezing keeps the element-wide
        // selection the replacement reads.
        this.#memory.freeze();

        if (found.kind === 'link' || found.kind === 'anchor') {
            this.#tell('HandleLinkActivatedAsync', found.kind === 'anchor');

            return;
        }

        this.#tell(RichTextEditor.#Activations[found.kind], MediaMarkup.read(found.kind, found.element));
    }

    /**
     * The key distinguishing one selection snapshot from the last: its block, its styles and whether it is a caret.
     * Offsets stay out of the key, so a caret moving within one formatting reports nothing; a report carries the
     * offsets of the change that raised it.
     */
    static #keyOf(snapshot) {
        return snapshot ? JSON.stringify([snapshot.blockType, snapshot.activeStyles, snapshot.isCollapsed]) : null;
    }

    /** Reports to the owner, letting go of the rejection a disposed owner answers with. */
    #tell(method, ...args) {
        this.#owner?.invokeMethodAsync(method, ...args).catch(() => {});
    }

    dispose() {
        this.#isDisposed = true;

        if (this.#debounceTimer !== null) {
            clearTimeout(this.#debounceTimer);
            this.#debounceTimer = null;
        }

        this.#controller.abort();
    }

    setHtml(html) {
        // The comparison ignores pending-format markers, so a round trip leaves a pending mark armed.
        if (InlineFormatter.stripMarkers(this.#canvas.html) !== html) {
            this.#canvas.html = html;
            this.#lastHtml = html;
            this.#lastSelectionKey = null;
            // The rewrite detached every node the remembered selection pointed into.
            this.#memory.forget();
        }
    }

    /**
     * What the link dialog is about to act on: the text the selection covers, the address of the single
     * link it touches, and how many links it touches.
     */
    beginLinkDialog(selector = 'a[href]') {
        const frozen = this.#memory.freeze();

        if (!frozen) {
            return { text: '', href: null, name: null, opensInNewTab: false, count: 0 };
        }

        const selected = frozen.collapsed ? '' : frozen.toString();
        const touched = this.#links.touching(frozen, selector);
        const single = touched.length === 1 ? touched[0] : null;

        return {
            text: selected || (single ? single.textContent : ''),
            href: single?.getAttribute('href') ?? null,
            name: single?.getAttribute('name') ?? null,
            opensInNewTab: single?.getAttribute('target') === '_blank',
            count: touched.length,
        };
    }

    exec(command, value = null) {
        if (this.#isDisposed) {
            return null;
        }

        this.#canvas.focus();

        switch (command) {
            case 'bold':
            case 'italic':
            case 'underline':
            case 'subscript':
            case 'superscript':
            case 'code':
                this.#withSelection(() => this.#inline.toggle(command));
                break;

            case 'strikeThrough':
                this.#withSelection(() => this.#inline.toggle('strike'));
                break;

            case 'removeFormat':
                this.#withSelection(() => this.#inline.clearFormatting());
                break;

            case 'fontName':
                this.#withSelection(() => this.#inline.applyStyle('font-family', value));
                break;

            case 'fontSize':
                this.#withSelection(() => this.#inline.applyStyle('font-size', value));
                break;

            case 'foreColor':
                this.#withSelection(() => this.#inline.applyStyle('color', value));
                break;

            case 'hiliteColor':
                this.#withSelection(() => this.#inline.applyStyle('background-color', value));
                break;

            case 'insertUnorderedList':
                this.#withSelection(() => this.#lists.toggle(false));
                break;

            case 'insertOrderedList':
                this.#withSelection(() => this.#lists.toggle(true));
                break;

            case 'indent':
                this.#withSelection(() => this.#lists.indent());
                break;

            case 'outdent':
                this.#withSelection(() => this.#lists.outdent());
                break;

            case 'justifyLeft':
                this.#withSelection(() => this.#blocks.align('left'));
                break;

            case 'justifyCenter':
                this.#withSelection(() => this.#blocks.align('center'));
                break;

            case 'justifyRight':
                this.#withSelection(() => this.#blocks.align('right'));
                break;

            case 'justifyFull':
                this.#withSelection(() => this.#blocks.align('justify'));
                break;

            case 'formatBlock':
                this.#withSelection(() => this.#blocks.convert(value));
                break;

            case 'lead':
                this.#withSelection(() => this.#blocks.toggleLead());
                break;

            case 'lineHeight':
                this.#withSelection(() => this.#blocks.setLineHeight(value));
                break;

            case 'dir':
                this.#withSelection(() => this.#blocks.setDirection(value));
                break;

            case 'revealBlock':
                this.#blocks.reveal(value);
                break;

            case 'insertHorizontalRule':
                this.#withSelection(() => this.#blocks.insertRule());
                break;

            case 'createLink':
                this.#createLink(value);
                break;

            case 'unlink':
                this.#unwrapAnchors('a[href]');
                break;

            case 'removeAnchor':
                this.#unwrapAnchors('a[name]');
                break;

            case 'selectAll':
                this.#canvas.selectAll();
                this.#memory.save();
                break;

            case 'insertText':
                this.#writer.insertText(value);
                break;

            case 'insertHtml':
                this.#writer.insertHtml(value);
                break;

            case 'insertTable':
                this.#writer.insertHtml(TableEditor.markup(value));
                break;

            case 'tableAddRowBefore':
                this.#withSelection(() => this.#tables.addRow(true));
                break;

            case 'tableAddRowAfter':
                this.#withSelection(() => this.#tables.addRow(false));
                break;

            case 'tableDeleteRow':
                this.#withSelection(() => this.#tables.deleteRow());
                break;

            case 'tableAddColBefore':
                this.#withSelection(() => this.#tables.addColumn(true));
                break;

            case 'tableAddColAfter':
                this.#withSelection(() => this.#tables.addColumn(false));
                break;

            case 'tableDeleteCol':
                this.#withSelection(() => this.#tables.deleteColumn());
                break;

            case 'tableToggleHeader':
                this.#withSelection(() => this.#tables.toggleHeader());
                break;

            case 'tableDelete':
                this.#withSelection(() => this.#tables.deleteTable());
                break;

            case 'insertImage':
                this.#writer.insertHtml(MediaMarkup.image(value));
                break;

            case 'insertResource':
                this.#writer.insertHtml(MediaMarkup.resource(value));
                break;

            case 'insertVideo':
                this.#writer.insertHtml(MediaMarkup.video(value));
                break;

            case 'insertEmbed':
                this.#writer.insertHtml(MediaMarkup.embed(value));
                break;

            case 'replaceContent':
                this.#canvas.html = value ?? '';
                break;

            case 'pasteText':
                this.#pasteText();
                break;

            case 'pasteWord':
                this.#pasteWord();
                break;
        }

        const html = InlineFormatter.stripMarkers(this.#canvas.html);
        this.#lastHtml = html;
        const selection = this.#inspector.snapshot();
        this.#lastSelectionKey = RichTextEditor.#keyOf(selection);

        return { html, selection };
    }

    /** Runs an operation over the selection the author last made in the canvas, and keeps its outcome. */
    #withSelection(operation) {
        this.#memory.restore();
        operation();
        this.#memory.save();
    }

    #handleInput() {
        if (this.#debounceTimer !== null) {
            clearTimeout(this.#debounceTimer);
        }

        this.#debounceTimer = setTimeout(() => {
            this.#debounceTimer = null;
            this.#flushContent();
        }, this.#debounceDelay);
    }

    #handleBlur() {
        if (this.#debounceTimer !== null) {
            clearTimeout(this.#debounceTimer);
            this.#debounceTimer = null;
        }

        // A format armed at the caret but never typed into is dropped as focus leaves.
        this.#inline.clearPendingMarkers();

        this.#flushContent();
        this.#reportSelection();
    }

    #flushContent() {
        if (this.#isDisposed) {
            return;
        }

        const currentHtml = InlineFormatter.stripMarkers(this.#canvas.html);
        if (currentHtml !== this.#lastHtml) {
            this.#lastHtml = currentHtml;
            this.#tell('HandleContentChangedAsync', currentHtml);
        }
    }

    #handleSelectionChange() {
        const selection = window.getSelection();
        if (!selection || selection.rangeCount === 0) {
            return;
        }

        const element = Canvas.elementOf(selection.anchorNode);

        if (element && this.#canvas.contains(element)) {
            this.#reportSelection();
        }
    }

    /** Remembers the selection and tells the owner of it, once per distinct block, styles and caret-or-range. */
    #reportSelection() {
        if (this.#isDisposed) {
            return;
        }

        this.#memory.save();
        const snapshot = this.#inspector.snapshot();
        if (!snapshot) {
            return;
        }

        const key = RichTextEditor.#keyOf(snapshot);
        if (key === this.#lastSelectionKey) {
            return;
        }

        this.#lastSelectionKey = key;
        this.#tell('HandleSelectionChangedAsync', snapshot);
    }

    /**
     * Unwraps every matching anchor the selection touches, keeping whatever text each held.
     * A caret inside one is enough, and a selection overlapping several unwraps all of them.
     */
    #unwrapAnchors(selector) {
        this.#memory.restore();

        const range = this.#canvas.currentRange();
        if (!range) {
            return;
        }

        if (this.#links.unwrapTouching(range, selector) === 0) {
            return;
        }

        // Unwrapping leaves the text split where the anchor used to start and end.
        this.#canvas.normalize();
        this.#memory.save();
    }

    #createLink(data) {
        const range = this.#memory.peek();
        const selectedText = range && !range.collapsed ? range.toString() : '';
        const html = Links.markup(data, selectedText);

        if (!html) {
            return;
        }

        // Whatever the selection already covered becomes one link, so the links it replaces go first.
        // Their text nodes survive the unwrapping, which leaves the range covering the same words.
        if (!data.isAnchor && range && !range.collapsed) {
            this.#links.unwrapTouching(range, 'a[href]');
        }

        this.#writer.insertHtml(html);
    }

    #pasteText() {
        if (!navigator.clipboard?.readText) {
            return;
        }

        navigator.clipboard.readText().then(text => {
            if (text) {
                this.#writer.insertText(text);
                this.#flushContent();
            }
        }).catch(() => { });
    }

    #pasteWord() {
        if (!navigator.clipboard?.read) {
            this.#pasteText();
            return;
        }

        navigator.clipboard.read().then(async items => {
            for (const item of items) {
                if (item.types.includes('text/html')) {
                    const html = await (await item.getType('text/html')).text();
                    this.#writer.insertHtml(sanitize(html));
                    this.#flushContent();
                    return;
                }
            }

            this.#pasteText();
        }).catch(() => { });
    }
}

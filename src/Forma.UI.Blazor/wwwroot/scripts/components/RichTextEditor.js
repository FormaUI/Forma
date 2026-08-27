/**
 * RichTextEditor — the visual contenteditable component: lifecycle, events, and command dispatch.
 * Each editing concern is a concept of its own under RichTextEditor/, composed here.
 */

import { sanitize } from '../operations/Html.js';
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
    /** Which callback each reopened kind reports to, since each carries fields of its own. */
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
     * Opens whatever the author double-clicked in the dialog that built it. The element is selected
     * whole first, so confirming the dialog replaces it rather than inserting a second one beside it.
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

        // Suppresses the word-selection a double-click would otherwise leave behind.
        event.preventDefault();
        this.#canvas.selectNode(found.element);
        this.#memory.save();

        // Frozen because the dialog about to open returns a collapsed caret when it closes, which
        // would otherwise overwrite the element-wide selection the replacement depends on.
        this.#memory.freeze();

        if (found.kind === 'link' || found.kind === 'anchor') {
            this.#owner?.invokeMethodAsync('HandleLinkActivatedAsync', found.kind === 'anchor');

            return;
        }

        this.#owner?.invokeMethodAsync(RichTextEditor.#Activations[found.kind], MediaMarkup.read(found.kind, found.element));
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
        // Pending-format markers never leave the canvas, so the comparison ignores them
        // rather than letting a round trip rewrite the content and disarm a pending mark.
        if (InlineFormatter.stripMarkers(this.#canvas.html) !== html) {
            this.#canvas.html = html;
            this.#lastHtml = html;
            // The rewrite detached every node the remembered selection pointed into, so restoring it would land the next edit somewhere unrelated.
            this.#memory.forget();
        }
    }

    getHtml() {
        return InlineFormatter.stripMarkers(this.#canvas.html);
    }

    /** The text of the last selection made inside the canvas, which outlives the focus a dialog takes away. */
    getSelectionText() {
        const frozen = this.#memory.freeze();

        return frozen && !frozen.collapsed ? frozen.toString() : '';
    }

    /**
     * What the link dialog is about to act on: the text it covers, and the address of the single link
     * it touches. Several links share no one address, so that field is left for the author to fill.
     */
    beginLinkDialog(selector = 'a[href]') {
        const frozen = this.#memory.freeze();

        if (!frozen) {
            return { text: '', href: null, count: 0 };
        }

        const selected = frozen.collapsed ? '' : frozen.toString();
        const touched = this.#links.touching(frozen, selector);
        const single = touched.length === 1 ? touched[0] : null;

        return {
            text: selected || (single ? single.textContent : ''),
            href: single ? single.getAttribute(selector === 'a[name]' ? 'name' : 'href') : null,
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

        // Focus is leaving, so a format armed at the caret but never typed into has no future.
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
            this.#owner?.invokeMethodAsync('HandleContentChangedAsync', currentHtml);
        }
    }

    #handleSelectionChange() {
        const selection = window.getSelection();
        if (!selection || selection.rangeCount === 0) {
            return;
        }

        const element = Canvas.elementOf(selection.anchorNode);

        if (element && this.#canvas.contains(element)) {
            this.#memory.save();
            this.#reportSelection();
        }
    }

    #reportSelection() {
        if (this.#isDisposed) {
            return;
        }

        this.#memory.save();
        const snapshot = this.#inspector.snapshot();
        if (snapshot) {
            this.#owner?.invokeMethodAsync('HandleSelectionChangedAsync', snapshot);
        }
    }

    /**
     * Unwraps every matching anchor the selection touches, keeping whatever text each held.
     * A caret inside one is enough, and a selection merely overlapping several clears all of them,
     * so an author never has to select a link exactly to remove it.
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
        const isAnchor = (data.isAnchor ?? data.IsAnchor ?? false);
        if (!isAnchor && range && !range.collapsed) {
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

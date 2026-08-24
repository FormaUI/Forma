/**
 * RichTextEditor — visual contenteditable canvas and formatting operations
 */

import { sanitize } from '../operations/Html.js';

export function attach(target, options = {}) {
    return new RichTextEditor(target, options);
}

/** Converts an rgb() or rgba() colour to #rrggbb, leaving any other notation alone. */
function toCssHex(value) {
    const parts = /^rgba?\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)/.exec(value);

    return parts
        ? `#${parts.slice(1, 4).map(part => Number(part).toString(16).padStart(2, '0')).join('')}`
        : value;
}

function escapeAttr(str) {
    return (str ?? '')
        .replaceAll('&', '&amp;')
        .replaceAll('"', '&quot;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;');
}

function escapeHtml(str) {
    return (str ?? '')
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;');
}

/** The largest execCommand size step, and the keyword it writes, used to find what a sizing pass just wrapped. */
const FontSizeMarkStep = '7';
const FontSizeMark = 'xxx-large';

class RichTextEditor {
    #target;
    #owner;
    #debounceDelay;
    #debounceTimer = null;
    #controller = new AbortController();
    #isDisposed = false;
    #lastHtml = null;
    #savedRange = null;
    #dialogRange = null;

    constructor(target, { owner, debounceDelay = 400 }) {
        this.#target = target;
        this.#owner = owner;
        this.#debounceDelay = debounceDelay;
        this.#lastHtml = target.innerHTML;

        const { signal } = this.#controller;

        target.addEventListener('input', () => this.#handleInput(), { signal });
        target.addEventListener('blur', () => this.#handleBlur(), { signal });
        target.addEventListener('click', () => this.#reportSelection(), { signal });
        target.addEventListener('keyup', () => this.#reportSelection(), { signal });

        document.addEventListener('selectionchange', () => this.#handleSelectionChange(), { signal });
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
        if (this.#target.innerHTML !== html) {
            this.#target.innerHTML = html;
            this.#lastHtml = html;
        }
    }

    getHtml() {
        return this.#target.innerHTML;
    }

    /** The text of the last selection made inside the canvas, which outlives the focus a dialog takes away. */
    getSelectionText() {
        // Closing a dialog returns a collapsed caret to the canvas, which would overwrite the saved range, so the range is frozen here and consumed by the insert that follows.
        this.#dialogRange = this.#savedRange ? this.#savedRange.cloneRange() : null;

        return this.#dialogRange && !this.#dialogRange.collapsed ? this.#dialogRange.toString() : '';
    }

    exec(command, value = null) {
        if (this.#isDisposed) {
            return null;
        }

        this.#target.focus();

        switch (command) {
            case 'bold':
            case 'italic':
            case 'underline':
            case 'strikeThrough':
            case 'subscript':
            case 'superscript':
            case 'removeFormat':
            case 'insertUnorderedList':
            case 'insertOrderedList':
            case 'indent':
            case 'outdent':
            case 'justifyLeft':
            case 'justifyCenter':
            case 'justifyRight':
            case 'justifyFull':
                document.execCommand(command, false, null);
                break;

            case 'formatBlock':
                document.execCommand('formatBlock', false, value?.startsWith('<') ? value : `<${value}>`);
                break;

            case 'fontName':
                this.#execStyled('fontName', value);
                break;

            case 'fontSize':
                this.#setFontSize(value);
                break;

            case 'foreColor':
                this.#execStyled('foreColor', value);
                break;

            case 'hiliteColor':
                this.#execStyled('hiliteColor', value);
                break;

            case 'code':
                this.#toggleInlineCode();
                break;

            case 'lead':
                this.#toggleLeadParagraph();
                break;

            case 'lineHeight':
                this.#setLineHeight(value);
                break;

            case 'dir':
                this.#setDirection(value);
                break;

            case 'insertHorizontalRule':
                document.execCommand('insertHorizontalRule', false, null);
                break;

            case 'createLink':
                this.#createLink(value);
                break;

            case 'unlink':
                this.#restoreSelection();
                document.execCommand('unlink', false, null);
                break;

            case 'removeAnchor':
                this.#removeAnchor();
                break;

            case 'selectAll':
                document.execCommand('selectAll', false, null);
                break;

            case 'insertText':
                this.#insertText(value);
                break;

            case 'insertHtml':
                this.#insertHtml(value);
                break;

            case 'insertTable':
                this.#insertTable(value);
                break;

            case 'tableAddRowBefore':
                this.#tableAddRow(true);
                break;

            case 'tableAddRowAfter':
                this.#tableAddRow(false);
                break;

            case 'tableDeleteRow':
                this.#tableDeleteRow();
                break;

            case 'tableAddColBefore':
                this.#tableAddCol(true);
                break;

            case 'tableAddColAfter':
                this.#tableAddCol(false);
                break;

            case 'tableDeleteCol':
                this.#tableDeleteCol();
                break;

            case 'tableToggleHeader':
                this.#tableToggleHeader();
                break;

            case 'tableDelete':
                this.#tableDelete();
                break;

            case 'insertMedia':
                this.#insertMedia(value);
                break;

            case 'insertVideo':
                this.#insertVideo(value);
                break;

            case 'insertEmbed':
                this.#insertEmbed(value);
                break;

            case 'replaceContent':
                this.#replaceContent(value);
                break;

            case 'pasteText':
                this.#pasteText();
                break;

            case 'pasteWord':
                this.#pasteWord();
                break;
        }

        const html = this.#target.innerHTML;
        this.#lastHtml = html;
        const selection = this.#getSelectionSnapshot();

        return { html, selection };
    }

    // Chrome emits <font> for these unless CSS styling is on, and the sanitizer keeps only inline styles.
    #execStyled(command, value) {
        document.execCommand('styleWithCSS', false, true);
        document.execCommand(command, false, value);
        document.execCommand('styleWithCSS', false, false);
    }

    // execCommand only sizes on a seven-step scale, so it marks the selection and the real size replaces the mark.
    #setFontSize(size) {
        const marked = `span[style*="${FontSizeMark}"]`;
        const existing = new Set(this.#target.querySelectorAll(marked));

        this.#execStyled('fontSize', FontSizeMarkStep);

        for (const span of this.#target.querySelectorAll(marked)) {
            if (existing.has(span)) {
                continue;
            }

            span.style.fontSize = size ?? '';

            if (!span.getAttribute('style')) {
                span.replaceWith(...span.childNodes);
            }
        }
    }

    #toggleInlineCode() {
        const selection = window.getSelection();
        if (!selection || selection.rangeCount === 0) {
            return;
        }

        const range = selection.getRangeAt(0);
        let parent = range.commonAncestorContainer;
        if (parent.nodeType === Node.TEXT_NODE) {
            parent = parent.parentElement;
        }

        const codeEl = parent?.closest('code');

        if (codeEl && this.#target.contains(codeEl)) {
            const fragment = document.createDocumentFragment();
            while (codeEl.firstChild) {
                fragment.appendChild(codeEl.firstChild);
            }
            codeEl.parentNode.replaceChild(fragment, codeEl);
        } else if (!range.collapsed) {
            const code = document.createElement('code');
            code.appendChild(range.extractContents());
            range.insertNode(code);
            selection.selectAllChildren(code);
        }
    }

    #toggleLeadParagraph() {
        const block = this.#getClosestBlock();
        if (block && this.#target.contains(block)) {
            block.classList.toggle('forma-composer-lead');
        }
    }

    #setLineHeight(value) {
        const block = this.#getClosestBlock();
        if (block && this.#target.contains(block)) {
            if (value) {
                block.style.lineHeight = value;
            } else {
                block.style.removeProperty('line-height');
            }
        }
    }

    #setDirection(value) {
        const block = this.#getClosestBlock();
        if (block && this.#target.contains(block)) {
            if (value === 'ltr' || value === 'rtl') {
                block.setAttribute('dir', value);
            } else {
                block.removeAttribute('dir');
            }
        }
    }

    #getClosestBlock() {
        const selection = window.getSelection();
        let node = (selection && selection.rangeCount > 0) ? selection.anchorNode : null;

        if (node && node.nodeType === Node.TEXT_NODE) {
            node = node.parentElement;
        }

        if (node && this.#target.contains(node) && node !== this.#target) {
            return node.closest('p, h1, h2, h3, h4, h5, h6, blockquote, pre, li, div') ?? node;
        }

        return this.#target.querySelector('p, h1, h2, h3, h4, h5, h6, blockquote, pre, li, div') ?? this.#target;
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

        this.#flushContent();
        this.#reportSelection();
    }

    #flushContent() {
        if (this.#isDisposed) {
            return;
        }

        const currentHtml = this.#target.innerHTML;
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

        const anchor = selection.anchorNode;
        const element = anchor?.nodeType === Node.TEXT_NODE ? anchor.parentElement : anchor;

        if (element && this.#target.contains(element)) {
            this.#saveSelection();
            this.#reportSelection();
        }
    }

    #reportSelection() {
        if (this.#isDisposed) {
            return;
        }

        this.#saveSelection();
        const snapshot = this.#getSelectionSnapshot();
        if (snapshot) {
            this.#owner?.invokeMethodAsync('HandleSelectionChangedAsync', snapshot);
        }
    }

    #saveSelection() {
        const selection = window.getSelection();
        if (selection && selection.rangeCount > 0) {
            const range = selection.getRangeAt(0);
            let anchor = selection.anchorNode;
            if (anchor && anchor.nodeType === Node.TEXT_NODE) {
                anchor = anchor.parentElement;
            }
            if (anchor && this.#target.contains(anchor)) {
                this.#savedRange = range.cloneRange();
            }
        }
    }

    /** The range an insert will land on, without consuming it, so a caller can read the text it is about to replace. */
    #peekRange() {
        return this.#dialogRange ?? this.#savedRange;
    }

    /** The nearest matching ancestor, but only while it stays inside the canvas. */
    #closestWithin(node, selector) {
        const found = node?.closest(selector);

        return found && this.#target.contains(found) ? found : null;
    }

    /** Unwraps the anchor around the caret, keeping whatever text it held. */
    #removeAnchor() {
        this.#restoreSelection();
        const selection = window.getSelection();
        const node = selection?.anchorNode;
        const anchor = this.#closestWithin(node?.nodeType === Node.TEXT_NODE ? node.parentElement : node, 'a[name]');

        if (!anchor) {
            return;
        }

        anchor.replaceWith(...anchor.childNodes);
        this.#saveSelection();
    }

    #restoreSelection() {
        const range = this.#dialogRange ?? this.#savedRange;
        this.#dialogRange = null;

        if (!range) {
            return;
        }
        const selection = window.getSelection();
        if (selection) {
            selection.removeAllRanges();
            selection.addRange(range);
        }
    }

    #insertHtml(html) {
        this.#restoreSelection();
        const selection = window.getSelection();
        const hasValidRange = selection
            && selection.rangeCount > 0
            && (selection.getRangeAt(0).commonAncestorContainer === this.#target
                || this.#target.contains(selection.getRangeAt(0).commonAncestorContainer));

        if (!hasValidRange) {
            const template = document.createElement('template');
            template.innerHTML = html;
            this.#target.appendChild(template.content);
            this.#saveSelection();
            return;
        }

        const range = selection.getRangeAt(0);
        range.deleteContents();
        const template = document.createElement('template');
        template.innerHTML = html;
        const fragment = template.content;
        const lastNode = fragment.lastChild;
        range.insertNode(fragment);
        if (lastNode) {
            range.setStartAfter(lastNode);
            range.collapse(true);
            selection.removeAllRanges();
            selection.addRange(range);
        }
        this.#saveSelection();
    }

    #insertText(text) {
        this.#restoreSelection();
        document.execCommand('insertText', false, text);
        this.#saveSelection();
    }

    #createLink(data) {
        if (!data) {
            return;
        }

        if (typeof data === 'string') {
            this.#restoreSelection();
            document.execCommand('createLink', false, data);
            return;
        }

        const { name, Name, href, Href, openInNewTab, OpenInNewTab, isAnchor, IsAnchor } = data;
        const effectiveIsAnchor = isAnchor ?? IsAnchor ?? false;
        const effectiveName = name ?? Name ?? '';
        const range = this.#peekRange();
        const selectedText = range && !range.collapsed ? range.toString() : '';

        if (effectiveIsAnchor) {
            if (!effectiveName) {
                return;
            }

            const anchorAttrs = `id="${escapeAttr(effectiveName)}" name="${escapeAttr(effectiveName)}"`;

            this.#insertHtml(selectedText
                ? `<a ${anchorAttrs}>${escapeHtml(selectedText)}</a>`
                : `<a ${anchorAttrs}></a>`);
            return;
        }

        const effectiveHref = href ?? Href ?? '';
        if (!effectiveHref) {
            return;
        }

        const effectiveText = effectiveName || selectedText || effectiveHref;
        const targetAttr = (openInNewTab ?? OpenInNewTab ?? false) ? ' target="_blank" rel="noopener noreferrer"' : '';

        this.#insertHtml(`<a href="${escapeAttr(effectiveHref)}"${targetAttr}>${escapeHtml(effectiveText)}</a>`);
    }

    #insertTable(data = {}) {
        const rows = Math.max(1, data.rows ?? data.Rows ?? 3);
        const cols = Math.max(1, data.columns ?? data.Columns ?? 3);
        const includeHeader = data.includeHeader ?? data.IncludeHeader ?? true;
        const caption = data.caption ?? data.Caption ?? '';

        let html = '<table class="forma-composer-table">';
        if (caption) {
            html += `<caption>${escapeHtml(caption)}</caption>`;
        }
        if (includeHeader) {
            html += '<thead><tr>';
            for (let c = 0; c < cols; c++) {
                html += '<th>&nbsp;</th>';
            }
            html += '</tr></thead>';
        }
        html += '<tbody>';
        const bodyRows = includeHeader ? rows - 1 : rows;
        for (let r = 0; r < Math.max(1, bodyRows); r++) {
            html += '<tr>';
            for (let c = 0; c < cols; c++) {
                html += '<td>&nbsp;</td>';
            }
            html += '</tr>';
        }
        html += '</tbody></table><p>&nbsp;</p>';

        this.#insertHtml(html);
    }

    #getClosestTableElement(selector) {
        this.#restoreSelection();
        const selection = window.getSelection();
        let node = (selection && selection.rangeCount > 0) ? selection.anchorNode : null;
        if (node && node.nodeType === Node.TEXT_NODE) {
            node = node.parentElement;
        }
        if (node && this.#target.contains(node)) {
            return node.closest(selector);
        }
        return null;
    }

    #tableAddRow(before) {
        const tr = this.#getClosestTableElement('tr');
        if (!tr) {
            return;
        }
        const colCount = tr.children.length;
        const newTr = document.createElement('tr');
        for (let i = 0; i < colCount; i++) {
            const td = document.createElement('td');
            td.innerHTML = '&nbsp;';
            newTr.appendChild(td);
        }
        if (before) {
            tr.before(newTr);
        } else {
            tr.after(newTr);
        }
    }

    #tableDeleteRow() {
        const tr = this.#getClosestTableElement('tr');
        if (!tr) {
            return;
        }
        const table = tr.closest('table');
        const rowCount = table?.querySelectorAll('tr').length ?? 0;
        if (rowCount <= 1) {
            table?.remove();
        } else {
            tr.remove();
        }
    }

    #tableAddCol(before) {
        const cell = this.#getClosestTableElement('td, th');
        if (!cell) {
            return;
        }
        const cellIndex = Array.from(cell.parentElement.children).indexOf(cell);
        const table = cell.closest('table');
        if (!table) {
            return;
        }

        for (const tr of table.querySelectorAll('tr')) {
            const isHeader = tr.parentElement?.tagName.toLowerCase() === 'thead';
            const newCell = document.createElement(isHeader ? 'th' : 'td');
            newCell.innerHTML = '&nbsp;';
            const targetCell = tr.children[cellIndex];
            if (targetCell) {
                if (before) {
                    targetCell.before(newCell);
                } else {
                    targetCell.after(newCell);
                }
            } else {
                tr.appendChild(newCell);
            }
        }
    }

    #tableDeleteCol() {
        const cell = this.#getClosestTableElement('td, th');
        if (!cell) {
            return;
        }
        const cellIndex = Array.from(cell.parentElement.children).indexOf(cell);
        const table = cell.closest('table');
        if (!table) {
            return;
        }

        const rows = table.querySelectorAll('tr');
        const firstRowColCount = rows[0]?.children.length ?? 0;
        if (firstRowColCount <= 1) {
            table.remove();
            return;
        }

        for (const tr of rows) {
            if (tr.children[cellIndex]) {
                tr.children[cellIndex].remove();
            }
        }
    }

    #tableToggleHeader() {
        const table = this.#getClosestTableElement('table');
        if (!table) {
            return;
        }
        const thead = table.querySelector('thead');
        if (thead) {
            const headerRow = thead.querySelector('tr');
            if (headerRow) {
                const tbody = table.querySelector('tbody') ?? table;
                for (const th of Array.from(headerRow.querySelectorAll('th'))) {
                    const td = document.createElement('td');
                    td.innerHTML = th.innerHTML;
                    th.replaceWith(td);
                }
                tbody.prepend(headerRow);
            }
            thead.remove();
        } else {
            const firstRow = table.querySelector('tr');
            if (firstRow) {
                const newThead = document.createElement('thead');
                for (const td of Array.from(firstRow.querySelectorAll('td, th'))) {
                    const th = document.createElement('th');
                    th.innerHTML = td.innerHTML;
                    td.replaceWith(th);
                }
                newThead.appendChild(firstRow);
                table.prepend(newThead);
            }
        }
    }

    #tableDelete() {
        const table = this.#getClosestTableElement('table');
        table?.remove();
    }

    #insertMedia(data = {}) {
        this.#restoreSelection();
        const src = data.source ?? data.Source ?? '';
        const alt = data.altText ?? data.AltText ?? '';
        const caption = data.caption ?? data.Caption ?? '';
        const alignment = data.alignment ?? data.Alignment ?? '';

        if (!src) {
            return;
        }

        const alignClass = alignment ? ` forma-media-align-${alignment.toLowerCase()}` : '';

        if (caption) {
            this.#insertHtml(`<figure class="forma-composer-media${alignClass}"><img src="${escapeAttr(src)}" alt="${escapeAttr(alt)}" /><figcaption>${escapeHtml(caption)}</figcaption></figure><p>&nbsp;</p>`);
        } else {
            this.#insertHtml(`<p><img src="${escapeAttr(src)}" alt="${escapeAttr(alt)}" class="forma-composer-media-img${alignClass}" /></p>`);
        }
    }

    #insertVideo(data = {}) {
        this.#restoreSelection();
        const url = data.source ?? data.Source ?? data.url ?? data.Url ?? '';
        const title = data.title ?? data.Title ?? '';
        const autoplay = data.autoplay ?? data.Autoplay ?? false;

        if (!url) {
            return;
        }

        const titleAttr = title ? ` data-video-title="${escapeAttr(title)}"` : '';
        const autoplayAttr = autoplay ? ' data-autoplay="true"' : '';

        this.#insertHtml(`<div class="forma-composer-video" data-video-url="${escapeAttr(url)}"${titleAttr}${autoplayAttr}><p><a href="${escapeAttr(url)}" target="_blank" rel="noopener noreferrer">${escapeHtml(title || url)}</a></p></div><p>&nbsp;</p>`);
    }

    #insertEmbed(data = {}) {
        this.#restoreSelection();
        const type = data.embedType ?? data.EmbedType ?? '';
        const id = data.identifier ?? data.Identifier ?? '';
        const settings = data.settings ?? data.Settings ?? '';

        if (!type && !id) {
            return;
        }

        const settingsAttr = settings ? ` data-settings="${escapeAttr(settings)}"` : '';
        this.#insertHtml(`<div class="forma-composer-embed" data-embed-type="${escapeAttr(type)}" data-embed-id="${escapeAttr(id)}"${settingsAttr}>[Embed: ${escapeHtml(type || id)}]</div><p>&nbsp;</p>`);
    }

    #replaceContent(html) {
        this.#target.innerHTML = html ?? '';
    }

    #pasteText() {
        if (!navigator.clipboard?.readText) {
            return;
        }

        navigator.clipboard.readText().then(text => {
            if (text) {
                this.#restoreSelection();
                this.#insertText(text);
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
                    this.#restoreSelection();
                    this.#insertHtml(sanitize(html));
                    this.#flushContent();
                    return;
                }
            }

            this.#pasteText();
        }).catch(() => { });
    }

    #getSelectionSnapshot() {
        const selection = window.getSelection();
        if (!selection || selection.rangeCount === 0) {
            return null;
        }

        const range = selection.getRangeAt(0);
        let anchor = selection.anchorNode;
        if (anchor && anchor.nodeType === Node.TEXT_NODE) {
            anchor = anchor.parentElement;
        }

        if (!anchor || !this.#target.contains(anchor)) {
            return null;
        }

        const isCollapsed = range.collapsed;
        const block = anchor.closest('p, h1, h2, h3, h4, h5, h6, blockquote, pre, li, div');
        const blockType = block ? block.tagName.toLowerCase() : 'p';

        const activeStyles = [];
        const commands = [
            ['bold', 'bold'],
            ['italic', 'italic'],
            ['underline', 'underline'],
            ['strikeThrough', 'strike'],
            ['subscript', 'subscript'],
            ['superscript', 'superscript'],
            ['insertUnorderedList', 'bullet-list'],
            ['insertOrderedList', 'ordered-list'],
            ['justifyLeft', 'align-left'],
            ['justifyCenter', 'align-center'],
            ['justifyRight', 'align-right'],
            ['justifyFull', 'align-justify'],
        ];

        for (const [cmd, name] of commands) {
            try {
                if (document.queryCommandState(cmd)) {
                    activeStyles.push(name);
                }
            } catch {
                // queryCommandState may be unsupported in some headless DOMs.
            }
        }

        if (anchor.closest('code')) {
            activeStyles.push('code');
        }

        if (this.#closestWithin(anchor, 'a[href]')) {
            activeStyles.push('link');
        }

        if (this.#closestWithin(anchor, 'a[name]')) {
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

        const values = [
            ['fontName', 'font-family'],
            ['foreColor', 'text-color'],
            ['hiliteColor', 'highlight-color'],
        ];

        for (const [cmd, name] of values) {
            try {
                const value = document.queryCommandValue(cmd);
                if (value) {
                    activeStyles.push(`${name}:${toCssHex(value.replaceAll('"', ''))}`);
                }
            } catch {
                // queryCommandValue may be unsupported in some headless DOMs.
            }
        }

        const sized = anchor.closest('[style*="font-size"]');
        const size = sized && this.#target.contains(sized) ? sized.style.fontSize : '';
        if (size) {
            activeStyles.push(`font-size:${size}`);
        }

        if (block) {
            const lineHeight = block.style.lineHeight;
            if (lineHeight) {
                activeStyles.push(`line-height:${lineHeight}`);
            }
        }

        const preRange = document.createRange();
        preRange.selectNodeContents(this.#target);
        preRange.setEnd(range.startContainer, range.startOffset);
        const start = preRange.toString().length;
        const end = start + range.toString().length;

        return {
            start,
            end,
            isCollapsed,
            blockType,
            activeStyles,
        };
    }
}

/**
 * RichTextEditor — visual contenteditable canvas and formatting operations
 */

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
            this.#reportSelection();
        }
    }

    #reportSelection() {
        if (this.#isDisposed) {
            return;
        }

        const snapshot = this.#getSelectionSnapshot();
        if (snapshot) {
            this.#owner?.invokeMethodAsync('HandleSelectionChangedAsync', snapshot);
        }
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

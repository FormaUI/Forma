/**
 * BubbleMenuPositioner — positions a floating bubble menu above or below an active text selection
 */

export function attach(target, editor = null, options = {}) {
    return new BubbleMenuPositioner(target, editor, options);
}

class BubbleMenuPositioner {
    #target;
    #editor;
    #activeClass;
    #controller = new AbortController();
    #isDisposed = false;

    constructor(target, editor = null, options = {}) {
        this.#target = target;
        this.#editor = editor;
        this.#activeClass = options?.activeClass ?? 'forma-composer-bubble-menu-active';

        const { signal } = this.#controller;

        document.addEventListener('selectionchange', () => this.update(), { signal });
        window.addEventListener('resize', () => this.update(), { signal });
        window.addEventListener('scroll', () => this.update(), { signal, capture: true, passive: true });
    }

    dispose() {
        this.#isDisposed = true;
        this.#controller.abort();
        this.#hide();
    }

    update() {
        if (this.#isDisposed) {
            return;
        }

        const selection = window.getSelection();
        if (!selection || selection.rangeCount === 0 || selection.isCollapsed) {
            this.#hide();
            return;
        }

        const range = selection.getRangeAt(0);
        const anchor = selection.anchorNode;
        const container = anchor?.nodeType === Node.TEXT_NODE ? anchor.parentElement : anchor;

        if (this.#editor && (!container || !this.#editor.contains(container))) {
            this.#hide();
            return;
        }

        const rangeRect = range.getBoundingClientRect();
        if (rangeRect.width === 0 && rangeRect.height === 0) {
            this.#hide();
            return;
        }

        this.#show();

        const menuRect = this.#target.getBoundingClientRect();
        const viewportWidth = window.innerWidth;
        const gap = 8;

        let left = rangeRect.left + (rangeRect.width / 2) - (menuRect.width / 2);
        left = Math.max(8, Math.min(left, viewportWidth - menuRect.width - 8));

        let top = rangeRect.top - menuRect.height - gap;
        if (top < 8) {
            top = rangeRect.bottom + gap;
        }

        this.#target.style.top = `${top}px`;
        this.#target.style.left = `${left}px`;
    }

    #show() {
        if (this.#activeClass) {
            this.#target.classList.add(this.#activeClass);
        }
    }

    #hide() {
        if (this.#activeClass) {
            this.#target.classList.remove(this.#activeClass);
        }
    }
}

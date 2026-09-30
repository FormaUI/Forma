/**
 * Virtualize — a virtualized list's browser side: where the list stands in what scrolls it, how much
 * room its drawn items take, and bringing one of them into view.
 */

import { tell } from '../../../core/scripts/Owner.js';

/**
 * @param {HTMLElement} start The spacer that opens the list.
 * @param {{ owner: import('../../../core/scripts/Owner.js').Owner, spacerAttribute: string }} options
 *   The owner told where the list stands, and the attribute every spacer carries.
 */
export function attach(start, options) {
    return new Virtualize(start, options);
}

class Virtualize {
    #start;
    #owner;
    #spacerAttribute;
    #scroller;
    #listening = new AbortController();
    #resizes;
    #drawings;
    #frame = 0;
    #reported = '';

    constructor(start, { owner, spacerAttribute }) {
        this.#start = start;
        this.#owner = owner;
        this.#spacerAttribute = spacerAttribute;
        this.#scroller = Virtualize.#scrollerOf(start.parentElement);

        const signal = this.#listening.signal;
        (this.#scroller ?? window).addEventListener('scroll', () => this.#schedule(), { passive: true, signal });
        window.addEventListener('resize', () => this.#schedule(), { signal });

        this.#resizes = new ResizeObserver(() => this.#schedule());
        this.#resizes.observe(start.parentElement);

        if (this.#scroller) {
            this.#resizes.observe(this.#scroller);
        }

        this.#drawings = new MutationObserver(() => this.#schedule());
        this.#drawings.observe(start.parentElement, { childList: true });
        this.#schedule();
    }

    dispose() {
        this.#listening.abort();
        this.#resizes.disconnect();
        this.#drawings.disconnect();
        cancelAnimationFrame(this.#frame);
    }

    /**
     * Scrolls so the point `offset` pixels into the list stands at the top of the viewport.
     * @param {number} offset
     */
    scrollTo(offset) {
        const change = offset - this.#offset();

        // Instant whatever the page's scroll-behavior, so the range the scroll reaches is the one the owner draws next.
        if (this.#scroller) {
            this.#scroller.scrollTo({ top: this.#scroller.scrollTop + change, behavior: 'instant' });
        } else {
            window.scrollBy({ top: change, behavior: 'instant' });
        }
    }

    /**
     * Brings the drawn item at `ordinal` into view.
     * @param {number} ordinal
     * @param {'nearest' | 'start'} alignment By the least scroll that shows it, or aligned to the top.
     * @returns {boolean} Whether the item is in view.
     */
    reveal(ordinal, alignment) {
        const item = this.#items()[ordinal];

        if (!item) {
            return false;
        }

        // Instant whatever the page's scroll-behavior, so whether the item is in view is known on return.
        item.scrollIntoView({ block: alignment === 'start' ? 'start' : 'nearest', inline: 'nearest', behavior: 'instant' });

        return this.#isInView(item);
    }

    #schedule() {
        if (this.#frame === 0) {
            this.#frame = requestAnimationFrame(() => {
                this.#frame = 0;
                this.#report();
            });
        }
    }

    #report() {
        // A list out of the document stands nowhere.
        if (!this.#start.isConnected) {
            return;
        }

        const size = this.#itemSize();
        const offset = Math.round(this.#offset());
        const viewport = Math.round(this.#viewport());
        const measured = Math.round(size * 10) / 10;
        const reported = `${offset}|${viewport}|${measured}`;

        if (reported === this.#reported) {
            return;
        }

        this.#reported = reported;
        tell(this.#owner, 'HandleViewportAsync', offset, viewport, measured);
    }

    /** The average room an item takes: from its top to the next drawn item's top, or its own height where no two drawn items stand together. */
    #itemSize() {
        const items = this.#items();
        const steps = items
            .filter(item => this.#isItem(item.nextElementSibling))
            .map(item => item.nextElementSibling.getBoundingClientRect().top - item.getBoundingClientRect().top);
        const sizes = steps.length > 0 ? steps : items.map(item => item.getBoundingClientRect().height);

        return sizes.length === 0 ? 0 : sizes.reduce((total, size) => total + size, 0) / sizes.length;
    }

    #isItem(element) {
        return element !== null && element.getAttribute(this.#spacerAttribute) === null;
    }

    // The elements between the opening spacer and the closing one, the spacers between them left out.
    #items() {
        const items = [];

        for (let element = this.#start.nextElementSibling; element; element = element.nextElementSibling) {
            const spacer = element.getAttribute(this.#spacerAttribute);

            if (spacer === 'end') {
                break;
            }

            if (spacer === null) {
                items.push(element);
            }
        }

        return items;
    }

    // How far the top of the viewport stands past the start of the list; negative while the list starts below it.
    #offset() {
        const top = this.#scroller ? this.#scroller.getBoundingClientRect().top + this.#scroller.clientTop : 0;

        return top - this.#start.getBoundingClientRect().top;
    }

    #viewport() {
        return this.#scroller ? this.#scroller.clientHeight : window.innerHeight;
    }

    #isInView(item) {
        const box = item.getBoundingClientRect();
        const top = this.#scroller ? this.#scroller.getBoundingClientRect().top + this.#scroller.clientTop : 0;
        const bottom = top + this.#viewport();

        return box.bottom > top && box.top < bottom;
    }

    // The nearest ancestor that scrolls its content vertically, or null where the page itself scrolls.
    static #scrollerOf(element) {
        for (let node = element; node && node !== document.body && node !== document.documentElement; node = node.parentElement) {
            const { overflowY } = getComputedStyle(node);

            if (overflowY === 'auto' || overflowY === 'scroll') {
                return node;
            }
        }

        return null;
    }
}

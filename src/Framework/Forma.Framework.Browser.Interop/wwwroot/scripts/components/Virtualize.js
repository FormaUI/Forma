/**
 * Virtualize — a virtualized list's browser side: where the list stands in what scrolls it, how much
 * room its drawn items take, and bringing one of them into view.
 */

import { tell } from '../../../core/scripts/Owner.js';
import { Scrolling } from '../../../core/scripts/Scrolling.js';

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
    #listenerAbort = new AbortController();
    #resizes;
    #drawings;
    #frame = 0;
    #reportedKey = '';
    #measuredItemSize = 0;
    #measuredAt = null;
    #measuredWidth = 0;
    #heights = new Map();

    constructor(start, { owner, spacerAttribute }) {
        this.#start = start;
        this.#owner = owner;
        this.#spacerAttribute = spacerAttribute;
        this.#resizes = new ResizeObserver(() => this.#schedule());
        this.#resizes.observe(start.parentElement);
        this.#chooseScroller();

        // What scrolls the list is chosen as it reports, so the page's scroll and every element's are heard, and those that move
        // the list are answered.
        const signal = this.#listenerAbort.signal;
        document.addEventListener('scroll', event => this.#handleScroll(event), { capture: true, passive: true, signal });
        window.addEventListener('resize', () => this.#schedule(), { signal });

        this.#drawings = new MutationObserver(() => this.#schedule());
        this.#drawings.observe(start.parentElement, { childList: true });
        this.#schedule();
    }

    dispose() {
        this.#listenerAbort.abort();
        this.#resizes.disconnect();
        this.#drawings.disconnect();
        cancelAnimationFrame(this.#frame);
    }

    /**
     * Scrolls so the point `offset` pixels into the list stands at the top of the viewport.
     * @param {number} offset
     */
    scrollTo(offset) {
        this.#chooseScroller();
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
        this.#chooseScroller();

        return this.#isInView(item);
    }

    // The page's scroll, or an ancestor's, moves the list; another element's leaves it where it stands.
    #handleScroll(event) {
        if (event.target instanceof Node && event.target.contains(this.#start)) {
            this.#schedule();
        }
    }

    // What scrolls the list: the nearest ancestor that scrolls it now, or the page where none does, so a container that grows with
    // its content, as one a narrow page lets grow does, leaves the list to the page rather than standing as a viewport the whole list
    // fits in. A container that bounds the list is watched for its size, since its height is the viewport's.
    #chooseScroller() {
        const scroller = Scrolling.nearestScroller(this.#start.parentElement, document.body);

        if (scroller === this.#scroller) {
            return;
        }

        if (this.#scroller) {
            this.#resizes.unobserve(this.#scroller);
        }

        if (scroller) {
            this.#resizes.observe(scroller);
        }

        this.#scroller = scroller;
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

        this.#chooseScroller();
        const offset = Math.round(this.#offset());
        const viewport = Math.round(this.#viewport());
        const measuredItemSize = this.#measure(offset);
        const reportedKey = `${offset}|${viewport}|${measuredItemSize}`;

        if (reportedKey === this.#reportedKey) {
            return;
        }

        this.#reportedKey = reportedKey;
        tell(this.#owner, 'HandleViewportAsync', offset, viewport, measuredItemSize);
    }

    /**
     * The size the owner is told: measured afresh where something the items depend on changed, the list scrolled, its width changed or a
     * drawn item resized, and otherwise the size already measured. What the owner does with a size is draw other items and size its
     * spacers, which changes none of those, so a render the report itself caused is never answered with a size of its own making.
     */
    #measure(offset) {
        const width = Math.round(this.#start.parentElement.getBoundingClientRect().width);
        const heights = new Map(this.#items().map(item => [item, item.getBoundingClientRect().height]));
        const isResized = [...heights].some(([item, height]) => this.#heights.has(item) && Math.abs(this.#heights.get(item) - height) >= 0.5);
        this.#heights = heights;

        if (this.#measuredItemSize === 0 || offset !== this.#measuredAt || width !== this.#measuredWidth || isResized) {
            this.#measuredItemSize = Math.round(this.#itemSize() * 10) / 10;
            this.#measuredAt = offset;
            this.#measuredWidth = width;
        }

        return this.#measuredItemSize;
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
}

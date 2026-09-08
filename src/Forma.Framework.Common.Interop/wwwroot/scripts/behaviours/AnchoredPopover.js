/**
 * AnchoredPopover — keeping a popover positioned against the element that opened it
 *
 * A popover in the top layer (via the popover attribute) is immune to every clipping and
 * scrolling ancestor, but the top layer keeps no layout relationship to whatever opened
 * it. The handle measures the anchor, applies the popover's position, and keeps the two
 * together for as long as the popover is open.
 */

import { Focus } from './Focus.js';
import { Direction } from '../../../core/scripts/Direction.js';
import { FrameScheduler } from '../../../core/scripts/FrameScheduler.js';

/** Distance between the anchor and the popover. */
const Gap = 4;

/**
 * @param {HTMLElement} target The popover.
 * @param {{
 *   anchor: HTMLElement,
 *   keepsFocus?: boolean,
 *   align?: 'auto' | 'start' | 'end'
 * }} options
 *   The element the popover hangs from, whether Tab moves between the popover's own controls instead of
 *   leaving it, and which inline edge of the anchor the popover lines up with: its reading-start edge,
 *   its reading-end edge, or by default the edge away from the nearer side of the viewport.
 */
export function attach(target, options) {
    return new AnchoredPopover(target, options);
}

class AnchoredPopover {
    #popover;
    #anchor;
    #keepsFocus;
    #align;
    #trigger = null;
    #placement = null;
    #resizeObserver = null;
    #scheduler = new FrameScheduler();
    #isRightToLeft = false;

    constructor(popover, { anchor, keepsFocus = false, align = 'auto' }) {
        this.#popover = popover;
        this.#anchor = anchor;
        this.#keepsFocus = keepsFocus;
        this.#align = align;
    }

    /** Shows the popover and keeps it positioned against its anchor until closed; focus returns to the trigger on a close that asks for it. */
    open(trigger = null) {
        this.close();
        this.#trigger = trigger;

        if (this.#popover.showPopover && !this.#popover.matches(':popover-open')) {
            this.#popover.showPopover();
        }

        this.#placement = new AbortController();
        const { signal } = this.#placement;
        const place = () => this.#schedule();

        // The anchor's direction holds for as long as the popover is open.
        this.#isRightToLeft = Direction.isRightToLeft(this.#anchor);

        // Captured because the anchor may sit in any scrolling ancestor, and scroll events
        // from those do not bubble.
        addEventListener('scroll', place, { capture: true, passive: true, signal });
        addEventListener('resize', place, { passive: true, signal });

        // The popover's own content can grow after it opens — a progressively loaded picker, a
        // search result count, a category switch — so its size alone must trigger repositioning.
        this.#resizeObserver = new ResizeObserver(place);
        this.#resizeObserver.observe(this.#popover);

        if (this.#keepsFocus) {
            new Focus(this.#popover, { wrap: true }, signal);
        }

        this.#position();
    }

    /** Hides the popover and releases the listeners held for it, first returning focus to the trigger when asked and focus is inside the popover. Safe to call when none are held. */
    close(returnsFocus = false) {
        // A trigger standing in a sticky part is focused where it stands; scrolling it into view would
        // move its scroller back to the part's unstuck position.
        if (returnsFocus && this.#trigger && this.#popover.contains(document.activeElement)) {
            this.#trigger.focus({ preventScroll: true });
        }

        this.#scheduler.cancel();
        this.#placement?.abort();
        this.#placement = null;
        this.#resizeObserver?.disconnect();
        this.#resizeObserver = null;

        if (this.#popover.hidePopover && this.#popover.matches(':popover-open')) {
            this.#popover.hidePopover();
        }
    }

    dispose() {
        this.close();
    }

    // Scroll and resize events arrive faster than frames are painted, so repositions are coalesced to one a frame.
    #schedule() {
        this.#scheduler.request(() => this.#position());
    }

    #position() {
        const anchor = this.#anchor.getBoundingClientRect();
        const width = this.#popover.offsetWidth;
        const height = this.#popover.offsetHeight;

        const preferredInlineStart = this.#hangsFromLeftEdge(anchor) ? anchor.left : anchor.right - width;
        const inlineStart = Math.max(Gap, Math.min(preferredInlineStart, innerWidth - width - Gap));

        const below = anchor.bottom + Gap;
        const above = anchor.top - height - Gap;
        const blockStart = below + height <= innerHeight || above < Gap ? below : above;

        this.#popover.style.left = `${Math.round(inlineStart)}px`;
        this.#popover.style.top = `${Math.round(blockStart)}px`;
    }

    // Whether the popover's left edge meets the anchor's left edge, extending rightward; otherwise its right edge
    // meets the anchor's right edge, extending leftward. The reading-start and reading-end edges are mirrored in
    // right-to-left text; the automatic choice extends toward the wider side of the viewport, and from dead
    // center toward the reading start.
    #hangsFromLeftEdge(anchorBounds) {
        switch (this.#align) {
            case 'start':
                return !this.#isRightToLeft;
            case 'end':
                return this.#isRightToLeft;
            default: {
                const anchorCenter = (anchorBounds.left + anchorBounds.right) / 2;
                const viewportCenter = innerWidth / 2;

                return anchorCenter === viewportCenter ? this.#isRightToLeft : anchorCenter < viewportCenter;
            }
        }
    }
}

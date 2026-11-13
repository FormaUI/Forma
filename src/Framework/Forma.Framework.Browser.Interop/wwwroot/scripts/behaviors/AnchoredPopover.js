/**
 * AnchoredPopover — a popover in the top layer kept positioned against its anchor
 *
 * The handle measures the anchor, applies the popover's position, and keeps the two
 * together for as long as the popover is open.
 */

import { FocusSurface } from '../focus/FocusSurface.mjs';
import { TabOrder } from '../focus/TabOrder.mjs';
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
 *   The element the popover hangs from, whether Tab moves between the popover's own controls, and which
 *   inline edge of the anchor the popover lines up with: its reading-start edge, its reading-end edge, or
 *   the edge away from the nearer side of the viewport.
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
    #directionObserver = null;
    #scheduler = new FrameScheduler();

    constructor(popover, { anchor, keepsFocus = false, align = 'auto' }) {
        this.#popover = popover;
        this.#anchor = anchor;
        this.#keepsFocus = keepsFocus;
        this.#align = align;
    }

    /** Shows the popover and keeps it positioned against its anchor until closed, holding the trigger focus returns to. */
    open(trigger = null) {
        this.close();
        this.#trigger = trigger;

        if (this.#popover.showPopover && !this.#popover.matches(':popover-open')) {
            this.#popover.showPopover();
        }

        this.#placement = new AbortController();
        const { signal } = this.#placement;
        const place = () => this.#schedule();

        // Captured, since scroll events from the anchor's scrolling ancestors do not bubble.
        addEventListener('scroll', place, { capture: true, passive: true, signal });
        addEventListener('resize', place, { passive: true, signal });

        this.#resizeObserver = new ResizeObserver(place);
        this.#resizeObserver.observe(this.#popover);

        // A direction can change while the popover is open with nothing resized or scrolled, so a change to any dir attribute places it again.
        this.#directionObserver = new MutationObserver(place);
        this.#directionObserver.observe(document.documentElement, { attributes: true, attributeFilter: ['dir'], subtree: true });

        if (this.#keepsFocus) {
            const tabOrder = new TabOrder(new FocusSurface(this.#popover), { wrap: true });
            signal.addEventListener('abort', () => tabOrder.dispose(), { once: true });
        }

        this.#position();
    }

    /** Hides the popover and releases the listeners held for it, first returning to the trigger, when asked, a focus inside it or one fallen to the page. */
    close(returnsFocus = false) {
        // WebKit focuses no button it presses, so a press inside leaves the focus on the page, which returns as a focus inside does.
        const focused = document.activeElement;

        if (returnsFocus && this.#trigger && (focused === null || focused === document.body || this.#popover.contains(focused))) {
            // Focused where it stands, so a trigger in a sticky part leaves its scroller in place.
            this.#trigger.focus({ preventScroll: true });
        }

        this.#scheduler.cancel();
        this.#placement?.abort();
        this.#placement = null;
        this.#resizeObserver?.disconnect();
        this.#resizeObserver = null;
        this.#directionObserver?.disconnect();
        this.#directionObserver = null;

        if (this.#popover.hidePopover && this.#popover.matches(':popover-open')) {
            this.#popover.hidePopover();
        }
    }

    dispose() {
        this.close();
    }

    #schedule() {
        this.#scheduler.request(() => this.#position());
    }

    #position() {
        const anchor = this.#anchor.getBoundingClientRect();
        const width = this.#popover.offsetWidth;
        const height = this.#popover.offsetHeight;

        const preferredInlineStart = this.#hangsFromLeftEdge(anchor, Direction.isRightToLeft(this.#anchor)) ? anchor.left : anchor.right - width;
        const inlineStart = Math.max(Gap, Math.min(preferredInlineStart, innerWidth - width - Gap));

        const below = anchor.bottom + Gap;
        const above = anchor.top - height - Gap;
        const blockStart = below + height <= innerHeight || above < Gap ? below : above;

        this.#popover.style.left = `${Math.round(inlineStart)}px`;
        this.#popover.style.top = `${Math.round(blockStart)}px`;
    }

    /**
     * Whether the popover's left edge meets the anchor's left edge, extending rightward, or its right edge meets
     * the anchor's right edge, extending leftward. The reading-start and reading-end edges mirror in right-to-left
     * text, the anchor's computed direction read at each placement; the automatic choice extends toward the wider side
     * of the viewport, and from dead center toward the reading start.
     */
    #hangsFromLeftEdge(anchorBounds, isRightToLeft) {
        switch (this.#align) {
            case 'start':
                return !isRightToLeft;
            case 'end':
                return isRightToLeft;
            default: {
                const anchorCenter = (anchorBounds.left + anchorBounds.right) / 2;
                const viewportCenter = innerWidth / 2;

                return anchorCenter === viewportCenter ? isRightToLeft : anchorCenter < viewportCenter;
            }
        }
    }
}

/**
 * BlockAlignment — rows kept level with the blocks of another element
 *
 * Each row names, by index, the block of the source it belongs to. Whenever the source scrolls,
 * resizes or changes, or the rows change, the rows are placed from the blocks' current geometry,
 * at most once per frame. A row naming a block that no longer exists is hidden.
 */

import { FrameScheduler } from '../../forma/core/scripts/FrameScheduler.js';

/**
 * @param {HTMLElement} target The element whose rows follow the blocks.
 * @param {{ source: HTMLElement, rowSelector?: string, blockAttribute?: string }} options
 * source holds the blocks as its children; rowSelector finds the rows inside the target, each
 * carrying its block's index in blockAttribute.
 */
export function attach(target, options = {}) {
    return new BlockAlignment(target, options);
}

class BlockAlignment {
    #target;
    #source;
    #rowSelector;
    #blockAttribute;
    #frames = new FrameScheduler();
    #controller = new AbortController();
    #resizes;
    #sourceChanges;
    #targetChanges;

    constructor(target, { source, rowSelector = '[data-block]', blockAttribute = 'data-block' }) {
        if (!(source instanceof Element)) {
            throw new Error('BlockAlignment needs the element whose blocks the rows follow.');
        }

        this.#target = target;
        this.#source = source;
        this.#rowSelector = rowSelector;
        this.#blockAttribute = blockAttribute;

        const { signal } = this.#controller;
        const schedule = () => this.#schedule();

        // The document scrolls inside its own box, so the rows move with it.
        source.addEventListener('scroll', schedule, { signal, passive: true });
        this.#resizes = new ResizeObserver(schedule);
        this.#resizes.observe(source);
        this.#sourceChanges = new MutationObserver(schedule);
        this.#sourceChanges.observe(source, { childList: true, subtree: true, characterData: true, attributes: true });
        this.#targetChanges = new MutationObserver(schedule);
        this.#targetChanges.observe(target, { childList: true, subtree: true, attributes: true, attributeFilter: [blockAttribute] });

        this.#layout();
    }

    dispose() {
        this.#controller.abort();
        this.#frames.cancel();
        this.#resizes.disconnect();
        this.#sourceChanges.disconnect();
        this.#targetChanges.disconnect();
    }

    #schedule() {
        this.#frames.request(() => this.#layout());
    }

    #layout() {
        const blocks = this.#source.children;
        const origin = this.#target.getBoundingClientRect().top;

        for (const row of this.#target.querySelectorAll(this.#rowSelector)) {
            const block = blocks[Number(row.getAttribute(this.#blockAttribute))];

            row.hidden = !block;

            if (block) {
                row.style.insetBlockStart = `${block.getBoundingClientRect().top - origin}px`;
            }
        }
    }
}

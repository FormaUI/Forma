/**
 * GridColumns — telling its owner how many columns a grid lays out
 *
 * The target's resolved tracks are counted each time it is laid out again, and the count is reported
 * to the owner only when it changes.
 */

import { tell } from '../../forma/core/scripts/Owner.js';

/** @typedef {import('../../forma/core/scripts/Owner.js').Owner} Owner */

/**
 * @param {HTMLElement} target The grid whose columns are counted.
 * @param {{ owner: Owner }} options The owner told the count through HandleGridColumnsAsync.
 */
export function attach(target, options) {
    return new GridColumns(target, options);
}

class GridColumns {
    #target;
    #owner;
    #observer;
    #last = 0;

    constructor(target, { owner }) {
        this.#target = target;
        this.#owner = owner;
        this.#observer = new ResizeObserver(() => this.#report());
        this.#observer.observe(target);
        this.#report();
    }

    dispose() {
        this.#observer.disconnect();
    }

    #report() {
        // A target out of the document lays nothing out.
        if (!this.#target.isConnected) {
            return;
        }

        const count = GridColumns.#count(getComputedStyle(this.#target).gridTemplateColumns);

        if (count === this.#last) {
            return;
        }

        this.#last = count;
        tell(this.#owner, 'HandleGridColumnsAsync', count);
    }

    // The resolved value lists one size per track, with any line names in brackets between them; a grid with none has one column.
    static #count(tracks) {
        const sizes = (tracks ?? '').replace(/\[[^\]]*\]/g, ' ').trim();

        return sizes === '' || sizes === 'none' ? 1 : sizes.split(/\s+/).length;
    }
}

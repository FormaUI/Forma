/**
 * ColumnFit — telling its owner the width a table has and the widths its columns want
 *
 * The table's container is watched for the width it gives, and the table itself for content that
 * changes what its columns need. On either, the table is laid out at its content's own width for one
 * synchronous read, so each marked header cell reports the width it wants rather than the share it was
 * given, and then laid out as it was before anything paints. The owner decides which columns fit; the
 * report is sent only when it differs from the last.
 */

import { tell } from '../../../core/scripts/Owner.js';

/** @typedef {import('../../../core/scripts/Owner.js').Owner} Owner */

/**
 * @param {HTMLTableElement} target The table whose columns are measured, laid out inside the container that gives it its width.
 * @param {{ owner: Owner, columnAttribute: string, measuringClass: string }} options
 *   The owner told the widths through HandleColumnFitAsync, the attribute a header cell names its column by, and the
 *   class that lays the table out at its content's own width while it is measured.
 */
export function attach(target, options) {
    return new ColumnFit(target, options);
}

class ColumnFit {
    #target;
    #owner;
    #columnAttribute;
    #measuringClass;
    #observer;
    #last = null;

    constructor(target, { owner, columnAttribute, measuringClass }) {
        this.#target = target;
        this.#owner = owner;
        this.#columnAttribute = columnAttribute;
        this.#measuringClass = measuringClass;
        this.#observer = new ResizeObserver(() => this.#report());
        this.#observer.observe(target.parentElement);
        this.#observer.observe(target);
        this.#report();
    }

    dispose() {
        this.#observer.disconnect();
    }

    #report() {
        // A table taken out of the document has no width, and its owner is on its way out with it.
        if (!this.#target.isConnected) {
            return;
        }

        const report = {
            availableWidth: this.#target.parentElement.clientWidth,
            columns: this.#measure(),
        };
        const key = JSON.stringify(report);

        if (key === this.#last) {
            return;
        }

        this.#last = key;
        tell(this.#owner, 'HandleColumnFitAsync', report);
    }

    // Both layouts happen inside one task, so the measuring one is never painted.
    #measure() {
        this.#target.classList.add(this.#measuringClass);

        try {
            return [...this.#target.querySelectorAll(`[${this.#columnAttribute}]`)].map(cell => ({
                key: cell.getAttribute(this.#columnAttribute),
                width: cell.getBoundingClientRect().width,
            }));
        } finally {
            this.#target.classList.remove(this.#measuringClass);
        }
    }
}

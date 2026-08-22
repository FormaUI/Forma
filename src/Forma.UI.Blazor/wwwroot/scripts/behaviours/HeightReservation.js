/**
 * HeightReservation — holding a container at the tallest height its content has needed
 *
 * A page holding fewer rows than the page size leaves a container shorter than a full one,
 * so whatever sits beneath it jumps up under the pointer between pages. How tall a row
 * stands follows from whatever the host puts in its cells, which is only known once laid
 * out, so the height to hold is measured rather than computed from a declared row size.
 *
 * Fractional box heights throughout: a row rounded to the whole pixel carries that error
 * once per row of the page, which is enough to shift the content below on its own.
 */

/**
 * @param {HTMLElement} target
 * @param {{ rowSelector?: string }} options Rows to measure; those rendered empty as padding are ignored.
 */
export function attach(target, options = {}) {
    return new HeightReservation(target, options);
}

class HeightReservation {
    #target;
    #rowSelector;
    #reserved;

    constructor(target, { rowSelector = 'tbody tr' }) {
        this.#target = target;
        this.#rowSelector = rowSelector;
    }

    /**
     * Measures the rows currently laid out and holds the container at a full page of them.
     * @param {number} pageSize
     */
    reserve(pageSize) {
        const table = this.#target.querySelector('table');

        if (!table) {
            return;
        }

        // A grid may pad a short page out to the page size, but with empty rows that carry
        // none of the content a row draws its height from, so they are no sample.
        const filled = [...this.#target.querySelectorAll(this.#rowSelector)]
            .filter(row => row.textContent.trim() !== '');

        if (filled.length === 0) {
            return;
        }

        // A full page states the height outright, and is worth keeping: rows of unequal
        // height make any projection from a short page an approximation of it.
        if (filled.length >= pageSize) {
            this.#reserved = table.getBoundingClientRect().height;
        } else if (this.#reserved === undefined) {
            const header = table.tHead?.getBoundingClientRect().height ?? 0;
            const tallest = Math.max(...filled.map(row => row.getBoundingClientRect().height));
            this.#reserved = header + tallest * pageSize;
        }

        this.#target.style.minBlockSize = `${this.#reserved}px`;
    }

    dispose() {
        this.#target.style.minBlockSize = '';
    }
}

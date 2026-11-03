/**
 * HeightReservation — holding a container at the tallest height its content has needed
 *
 * The height held is measured from the rows as laid out, in fractional box heights, since a
 * row rounded to the whole pixel carries that error once per row of the page.
 */

/**
 * @param {HTMLElement} target
 * @param {{ rowSelector?: string }} options The selector naming the rows to measure.
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

        // Empty rows pad a short page out to the page size and carry no content to measure.
        const filled = [...this.#target.querySelectorAll(this.#rowSelector)]
            .filter(row => row.textContent.trim() !== '');

        if (filled.length === 0) {
            return;
        }

        // A full page's height is held outright; a short page projects its tallest row only while nothing is held.
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

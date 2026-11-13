import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { attach } from '#dataview/HeightReservation.js';

describe('HeightReservation', () => {
    /** @type {HTMLElement} */
    let container;

    // Lays the table out with a header of the given height and one filled row of each given height.
    const lay = (headerHeight, rowHeights) => {
        container.innerHTML = `<table><thead><tr><th>Name</th></tr></thead><tbody>${rowHeights.map((_, index) => `<tr><td>Row ${index}</td></tr>`).join('')}</tbody></table>`;
        const table = container.querySelector('table');
        const rows = [...container.querySelectorAll('tbody tr')];
        table.tHead.getBoundingClientRect = () => ({ height: headerHeight });
        rows.forEach((row, index) => { row.getBoundingClientRect = () => ({ height: rowHeights[index] }); });
        table.getBoundingClientRect = () => ({ height: headerHeight + rowHeights.reduce((sum, height) => sum + height, 0) });
    };

    beforeEach(() => {
        container = document.createElement('div');
        document.body.append(container);
    });

    afterEach(() => {
        container.remove();
    });

    it('holds the container at a full page of the rows laid out', () => {
        const reservation = attach(container);
        lay(20, [30, 30, 30]);

        reservation.reserve(3);

        expect(container.style.minBlockSize).toBe('110px');
    });

    it('projects a full page from the tallest row of a short page', () => {
        const reservation = attach(container);
        lay(20, [30, 40]);

        reservation.reserve(4);

        expect(container.style.minBlockSize).toBe('180px');
    });

    it('lets the container take its natural height once released, measuring a later page afresh', () => {
        const reservation = attach(container);
        lay(20, [30, 30, 30]);
        reservation.reserve(3);

        reservation.release();
        lay(20, [10]);
        reservation.reserve(2);

        expect(container.style.minBlockSize).toBe('40px');
    });

    it('lets the container go once disposed', () => {
        const reservation = attach(container);
        lay(20, [30, 30, 30]);
        reservation.reserve(3);

        reservation.dispose();

        expect(container.style.minBlockSize).toBe('');
    });
});

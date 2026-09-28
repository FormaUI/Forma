import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { attach } from '#browser/behaviors/ColumnFit.js';
import { fakeOwner } from '#support/owner.js';
import { stubResizeObserver } from '#support/platform.js';

describe('ColumnFit', () => {
    /** @type {ReturnType<typeof stubResizeObserver>} */
    let resizes;
    /** @type {HTMLElement} */
    let container;
    /** @type {HTMLTableElement} */
    let table;
    /** @type {ReturnType<typeof fakeOwner>} */
    let owner;
    const options = () => ({ owner, columnAttribute: 'data-column', measuringClass: 'is-measuring' });

    // A headless DOM lays nothing out, so each header cell answers with the width it is given, and only while the table is being measured.
    const lay = (widths, available) => {
        Object.defineProperty(container, 'clientWidth', { value: available, configurable: true });

        for (const cell of table.querySelectorAll('th[data-column]')) {
            cell.getBoundingClientRect = () => ({ width: table.classList.contains('is-measuring') ? widths[cell.dataset.column] : 0 });
        }
    };

    beforeEach(() => {
        resizes = stubResizeObserver();
        document.body.innerHTML = '<div id="container"><table><thead><tr><th data-column="name"></th><th data-column="size"></th><th>menu</th></tr></thead></table></div>';
        container = document.querySelector('#container');
        table = document.querySelector('table');
        owner = fakeOwner();
        lay({ name: 120, size: 80 }, 400);
    });

    afterEach(() => {
        resizes.restore();
    });

    it('reports the width the container gives and the width each named column wants on attach', () => {
        attach(table, options());

        expect(owner.invokeMethodAsync).toHaveBeenCalledWith('HandleColumnFitAsync', {
            availableWidth: 400,
            columns: [{ key: 'name', width: 120 }, { key: 'size', width: 80 }],
        });
    });

    it('measures the table at its content width and lays it out again before returning', () => {
        attach(table, options());

        expect(table.classList.contains('is-measuring')).toBe(false);
        expect(owner.invokeMethodAsync.mock.calls[0][1].columns[0].width).toBe(120);
    });

    it('watches the container and the table', () => {
        attach(table, options());

        expect(resizes.latest().targets).toEqual([container, table]);
    });

    it('reports again when the container changes width', () => {
        attach(table, options());

        lay({ name: 120, size: 80 }, 300);
        resizes.latest().deliver([]);

        expect(owner.invokeMethodAsync).toHaveBeenLastCalledWith('HandleColumnFitAsync', expect.objectContaining({ availableWidth: 300 }));
        expect(owner.invokeMethodAsync).toHaveBeenCalledTimes(2);
    });

    it('stays quiet through a layout pass that changes nothing', () => {
        attach(table, options());

        resizes.latest().deliver([]);
        resizes.latest().deliver([]);

        expect(owner.invokeMethodAsync).toHaveBeenCalledTimes(1);
    });

    it('stays quiet once the table has left the document', () => {
        attach(table, options());

        table.remove();
        resizes.latest().deliver([]);

        expect(owner.invokeMethodAsync).toHaveBeenCalledTimes(1);
    });

    it('stops watching on dispose', () => {
        const handle = attach(table, options());

        handle.dispose();

        expect(resizes.latest().isConnected).toBe(false);
    });
});

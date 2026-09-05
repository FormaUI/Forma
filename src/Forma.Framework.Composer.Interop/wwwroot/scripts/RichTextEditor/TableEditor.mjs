/**
 * TableEditor — building a table and reshaping the one the caret sits in.
 */

import { Canvas } from './Canvas.mjs';
import { Escape } from './Markup.mjs';

export class TableEditor {
    #canvas;

    constructor(canvas) {
        this.#canvas = canvas;
    }

    /** The markup a table request builds. */
    static markup({ rows: rowCount = 3, columns: columnCount = 3, includeHeader = true, caption = '' } = {}) {
        const rows = Math.max(1, rowCount);
        const cols = Math.max(1, columnCount);

        let html = '<table class="forma-composer-table">';
        if (caption) {
            html += `<caption>${Escape.html(caption)}</caption>`;
        }
        if (includeHeader) {
            html += '<thead><tr>';
            for (let c = 0; c < cols; c++) {
                html += '<th>&nbsp;</th>';
            }
            html += '</tr></thead>';
        }
        html += '<tbody>';
        const bodyRows = includeHeader ? rows - 1 : rows;
        for (let r = 0; r < Math.max(1, bodyRows); r++) {
            html += '<tr>';
            for (let c = 0; c < cols; c++) {
                html += '<td>&nbsp;</td>';
            }
            html += '</tr>';
        }
        html += '</tbody></table><p>&nbsp;</p>';

        return html;
    }

    addRow(before) {
        const row = this.#closestPart('tr');
        if (!row) {
            return;
        }

        const added = document.createElement('tr');
        for (let i = 0; i < row.children.length; i++) {
            const cell = document.createElement('td');
            cell.innerHTML = '&nbsp;';
            added.appendChild(cell);
        }

        if (before) {
            row.before(added);
        } else {
            row.after(added);
        }
    }

    deleteRow() {
        const row = this.#closestPart('tr');
        if (!row) {
            return;
        }

        const table = row.closest('table');
        const rowCount = table?.querySelectorAll('tr').length ?? 0;
        if (rowCount <= 1) {
            table?.remove();
        } else {
            row.remove();
        }
    }

    addColumn(before) {
        const cell = this.#closestPart('td, th');
        const table = cell?.closest('table');
        if (!cell || !table) {
            return;
        }

        const cellIndex = [...cell.parentElement.children].indexOf(cell);

        for (const row of table.querySelectorAll('tr')) {
            const isHeader = row.parentElement?.tagName === 'THEAD';
            const added = document.createElement(isHeader ? 'th' : 'td');
            added.innerHTML = '&nbsp;';
            const at = row.children[cellIndex];
            if (at) {
                if (before) {
                    at.before(added);
                } else {
                    at.after(added);
                }
            } else {
                row.appendChild(added);
            }
        }
    }

    deleteColumn() {
        const cell = this.#closestPart('td, th');
        const table = cell?.closest('table');
        if (!cell || !table) {
            return;
        }

        const cellIndex = [...cell.parentElement.children].indexOf(cell);
        const rows = table.querySelectorAll('tr');

        if ((rows[0]?.children.length ?? 0) <= 1) {
            table.remove();
            return;
        }

        for (const row of rows) {
            row.children[cellIndex]?.remove();
        }
    }

    toggleHeader() {
        const table = this.#closestPart('table');
        if (!table) {
            return;
        }

        const head = table.querySelector('thead');

        if (head) {
            const headerRow = head.querySelector('tr');
            if (headerRow) {
                const body = table.querySelector('tbody') ?? table;
                for (const th of [...headerRow.querySelectorAll('th')]) {
                    const td = document.createElement('td');
                    td.innerHTML = th.innerHTML;
                    th.replaceWith(td);
                }
                body.prepend(headerRow);
            }
            head.remove();
        } else {
            const firstRow = table.querySelector('tr');
            if (firstRow) {
                const added = document.createElement('thead');
                for (const cell of [...firstRow.querySelectorAll('td, th')]) {
                    const th = document.createElement('th');
                    th.innerHTML = cell.innerHTML;
                    cell.replaceWith(th);
                }
                added.appendChild(firstRow);
                table.prepend(added);
            }
        }
    }

    deleteTable() {
        this.#closestPart('table')?.remove();
    }

    /** The nearest table part holding the selection, or null when the caret is not in one. */
    #closestPart(selector) {
        const selection = window.getSelection();
        const node = Canvas.elementOf((selection && selection.rangeCount > 0) ? selection.anchorNode : null);

        return node && this.#canvas.contains(node) ? node.closest(selector) : null;
    }
}

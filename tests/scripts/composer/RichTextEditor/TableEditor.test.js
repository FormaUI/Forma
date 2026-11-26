import { describe, it, expect, beforeEach } from 'vitest';
import { Canvas } from '#composer/RichTextEditor/Canvas.js';
import { TableEditor } from '#composer/RichTextEditor/TableEditor.js';

const NoBreakSpace = String.fromCharCode(0xa0);

describe('TableEditor', () => {
    /** @type {HTMLElement} */
    let canvas;
    /** @type {TableEditor} */
    let tables;

    beforeEach(() => {
        document.body.innerHTML = '<div id="canvas" contenteditable="true"><table>'
            + '<thead><tr><th>Stage<img src="x"></th><th>Rider</th></tr></thead>'
            + '<tbody><tr><td>12<img src="y"></td><td>Ana</td></tr></tbody>'
            + '</table></div>';
        canvas = document.querySelector('#canvas');
        tables = new TableEditor(new Canvas(canvas));
    });

    function placeCaretIn(cell) {
        const caret = document.createRange();
        caret.selectNodeContents(cell);
        caret.collapse(true);
        const selection = window.getSelection();
        selection.removeAllRanges();
        selection.addRange(caret);
    }

    it('moves a header cell\'s content into the body as the nodes it holds, never through markup', () => {
        const image = canvas.querySelector('th img');
        placeCaretIn(canvas.querySelector('td'));

        tables.toggleHeader();

        const cell = canvas.querySelector('tr').firstElementChild;
        expect(canvas.querySelector('thead')).toBeNull();
        expect(cell.tagName).toBe('TD');
        expect(cell.querySelector('img')).toBe(image);
        expect(cell.textContent).toBe('Stage');
    });

    it('moves a body cell\'s content into a header as the nodes it holds, never through markup', () => {
        placeCaretIn(canvas.querySelector('td'));
        tables.toggleHeader();
        const image = canvas.querySelector('tr').firstElementChild.querySelector('img');
        placeCaretIn(canvas.querySelector('td'));

        tables.toggleHeader();

        const cell = canvas.querySelector('thead th');
        expect(cell.querySelector('img')).toBe(image);
        expect(cell.textContent).toBe('Stage');
    });

    it('adds a row and a column of cells holding a no-break space as text', () => {
        placeCaretIn(canvas.querySelector('td'));

        tables.addRow(false);
        tables.addColumn(false);

        const addedRow = canvas.querySelectorAll('tbody tr')[1];
        expect(addedRow.children[0].textContent).toBe(NoBreakSpace);
        expect(addedRow.children[0].childNodes[0].nodeType).toBe(Node.TEXT_NODE);
        expect([...canvas.querySelectorAll('tr')].map(row => row.children[1].textContent)).toEqual([NoBreakSpace, NoBreakSpace, NoBreakSpace]);
    });
});

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { attach } from '#behaviours/BubbleMenuPositioner.js';

describe('BubbleMenuPositioner', () => {
    /** @type {HTMLElement} */
    let editor;
    /** @type {HTMLElement} */
    let menu;

    beforeEach(() => {
        document.body.innerHTML = `
            <div id="editor" contenteditable="true">
                <p id="text">Selected content</p>
            </div>
            <div id="menu" class="bubble-menu"></div>
        `;
        editor = document.querySelector('#editor');
        menu = document.querySelector('#menu');

        menu.getBoundingClientRect = () => ({
            top: 0,
            bottom: 30,
            left: 0,
            right: 120,
            width: 120,
            height: 30,
            x: 0,
            y: 0,
        });

        globalThis.innerWidth = 1024;
    });

    const selectText = (collapsed = false, outside = false) => {
        const textNode = (outside ? document.body : document.querySelector('#text')).firstChild;
        const range = document.createRange();

        if (collapsed) {
            range.setStart(textNode, 0);
            range.collapse(true);
        } else {
            range.setStart(textNode, 0);
            range.setEnd(textNode, 8);
        }

        range.getBoundingClientRect = () => (collapsed ? {
            top: 100, bottom: 120, left: 200, right: 200, width: 0, height: 20, x: 200, y: 100,
        } : {
            top: 100, bottom: 120, left: 200, right: 280, width: 80, height: 20, x: 200, y: 100,
        });

        const sel = window.getSelection();
        sel.removeAllRanges();
        sel.addRange(range);

        return range;
    };

    it('shows and positions the bubble menu when a range is selected inside the editor', () => {
        attach(menu, editor, { activeClass: 'is-visible' });
        selectText(false, false);

        document.dispatchEvent(new Event('selectionchange'));

        expect(menu.classList.contains('is-visible')).toBe(true);
        expect(menu.style.top).toBe('62px'); // 100 (top) - 30 (height) - 8 (gap)
    });

    it('hides the menu when selection is collapsed', () => {
        attach(menu, editor, { activeClass: 'is-visible' });
        selectText(false, false);
        document.dispatchEvent(new Event('selectionchange'));
        expect(menu.classList.contains('is-visible')).toBe(true);

        selectText(true, false);
        document.dispatchEvent(new Event('selectionchange'));

        expect(menu.classList.contains('is-visible')).toBe(false);
    });

    it('hides the menu when selection is outside the editor', () => {
        attach(menu, editor, { activeClass: 'is-visible' });
        selectText(false, true);

        document.dispatchEvent(new Event('selectionchange'));

        expect(menu.classList.contains('is-visible')).toBe(false);
    });

    it('positions below selection when not enough room above', () => {
        attach(menu, editor, { activeClass: 'is-visible' });
        const range = selectText(false, false);
        range.getBoundingClientRect = () => ({
            top: 10, bottom: 30, left: 200, right: 280, width: 80, height: 20, x: 200, y: 10,
        });

        document.dispatchEvent(new Event('selectionchange'));

        expect(menu.style.top).toBe('38px'); // 30 (bottom) + 8 (gap)
    });

    it('hides and removes listeners once disposed', () => {
        const handle = attach(menu, editor, { activeClass: 'is-visible' });
        selectText(false, false);
        document.dispatchEvent(new Event('selectionchange'));
        expect(menu.classList.contains('is-visible')).toBe(true);

        handle.dispose();

        expect(menu.classList.contains('is-visible')).toBe(false);

        document.dispatchEvent(new Event('selectionchange'));
        expect(menu.classList.contains('is-visible')).toBe(false);
    });
});

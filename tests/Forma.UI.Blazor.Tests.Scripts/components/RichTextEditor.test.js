import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { attach } from '#components/RichTextEditor.js';
import { fakeOwner } from '#support/owner.js';

describe('RichTextEditor', () => {
    /** @type {HTMLElement} */
    let editor;
    /** @type {ReturnType<typeof fakeOwner>} */
    let owner;

    beforeEach(() => {
        vi.useFakeTimers();
        document.body.innerHTML = '<div id="editor" contenteditable="true"><p>Hello world</p></div>';
        editor = document.querySelector('#editor');
        owner = fakeOwner();

        // Polyfill document.execCommand in happy-dom for testing
        document.execCommand = vi.fn((command, showUi, value) => {
            if (command === 'formatBlock' && value) {
                const tag = value.replace(/[<>]/g, '');
                const p = editor.querySelector('p');
                if (p) {
                    const block = document.createElement(tag);
                    block.innerHTML = p.innerHTML;
                    p.replaceWith(block);
                }
            }
            return true;
        });

        document.queryCommandState = vi.fn(command => command === 'bold');
    });

    afterEach(() => {
        vi.useRealTimers();
    });

    it('reads and sets HTML content', () => {
        const handle = attach(editor, { owner });

        expect(handle.getHtml()).toBe('<p>Hello world</p>');

        handle.setHtml('<p>Updated</p>');
        expect(handle.getHtml()).toBe('<p>Updated</p>');
    });

    it('debounces content changes on input', () => {
        attach(editor, { owner, debounceDelay: 300 });

        editor.innerHTML = '<p>Typing...</p>';
        editor.dispatchEvent(new Event('input', { bubbles: true }));

        expect(owner.invokeMethodAsync).not.toHaveBeenCalled();

        vi.advanceTimersByTime(300);

        expect(owner.invokeMethodAsync).toHaveBeenCalledWith('HandleContentChangedAsync', '<p>Typing...</p>');
    });

    it('flushes pending content changes on blur', () => {
        attach(editor, { owner, debounceDelay: 300 });

        editor.innerHTML = '<p>Immediate blur</p>';
        editor.dispatchEvent(new Event('input', { bubbles: true }));
        editor.dispatchEvent(new Event('blur', { bubbles: true }));

        expect(owner.invokeMethodAsync).toHaveBeenCalledWith('HandleContentChangedAsync', '<p>Immediate blur</p>');
    });

    it('executes formatting commands and returns the updated state', () => {
        const handle = attach(editor, { owner });

        const result = handle.exec('bold');

        expect(document.execCommand).toHaveBeenCalledWith('bold', false, null);
        expect(result).not.toBeNull();
        expect(result.html).toBe('<p>Hello world</p>');
    });

    it('executes formatBlock with angle brackets', () => {
        const handle = attach(editor, { owner });

        handle.exec('formatBlock', 'h1');

        expect(document.execCommand).toHaveBeenCalledWith('formatBlock', false, '<h1>');
    });

    // Without CSS styling the browser emits <font>, which the sanitizer drops on the round trip.
    it.each([
        ['foreColor', '#d32f2f'],
        ['hiliteColor', '#ffff00'],
        ['fontName', 'Georgia'],
    ])('turns CSS styling on around %s and off again', (command, value) => {
        const handle = attach(editor, { owner });

        handle.exec(command, value);

        const calls = document.execCommand.mock.calls;
        const index = calls.findIndex(call => call[0] === command);

        expect(calls[index - 1]).toEqual(['styleWithCSS', false, true]);
        expect(calls[index]).toEqual([command, false, value]);
        expect(calls[index + 1]).toEqual(['styleWithCSS', false, false]);
    });

    it('writes the font size exactly as the caller expressed it', () => {        document.execCommand = vi.fn((command, showUi, value) => {
            if (command === 'fontSize') {
                const p = editor.querySelector('p');
                p.innerHTML = `<span style="font-size: xxx-large;">${p.innerHTML}</span>`;
            }
            return true;
        });
        const handle = attach(editor, { owner });

        handle.exec('fontSize', '1.5rem');

        expect(editor.querySelector('span').style.fontSize).toBe('1.5rem');
    });

    it('unwraps the span when a font size is cleared', () => {
        document.execCommand = vi.fn((command, showUi, value) => {
            if (command === 'fontSize') {
                const p = editor.querySelector('p');
                p.innerHTML = `<span style="font-size: xxx-large;">${p.innerHTML}</span>`;
            }
            return true;
        });
        const handle = attach(editor, { owner });

        handle.exec('fontSize', null);

        expect(editor.querySelector('span')).toBeNull();
        expect(editor.querySelector('p').innerHTML).toBe('Hello world');
    });

    it('leaves CSS styling off for commands with semantic markup of their own', () => {
        const handle = attach(editor, { owner });

        handle.exec('bold');

        expect(document.execCommand).not.toHaveBeenCalledWith('styleWithCSS', false, true);
    });

    it('reports colours as hex so they match the toolbar options', () => {
        document.queryCommandValue = vi.fn(command => (command === 'foreColor' ? 'rgb(211, 47, 47)' : ''));
        const handle = attach(editor, { owner });

        const range = document.createRange();
        range.selectNodeContents(editor.querySelector('p'));
        const selected = window.getSelection();
        selected.removeAllRanges();
        selected.addRange(range);

        const { selection } = handle.exec('bold');

        expect(selection.activeStyles).toContain('text-color:#d32f2f');
    });

    it('toggles lead paragraph class', () => {
        const handle = attach(editor, { owner });

        handle.exec('lead');
        expect(editor.querySelector('p').classList.contains('forma-composer-lead')).toBe(true);

        handle.exec('lead');
        expect(editor.querySelector('p').classList.contains('forma-composer-lead')).toBe(false);
    });

    it('sets direction on the active block', () => {
        const handle = attach(editor, { owner });

        handle.exec('dir', 'rtl');
        expect(editor.querySelector('p').getAttribute('dir')).toBe('rtl');

        handle.exec('dir', 'ltr');
        expect(editor.querySelector('p').getAttribute('dir')).toBe('ltr');

        handle.exec('dir', null);
        expect(editor.querySelector('p').hasAttribute('dir')).toBe(false);
    });

    it('sets line height on the active block', () => {
        const handle = attach(editor, { owner });

        handle.exec('lineHeight', '1.8');
        expect(editor.querySelector('p').style.lineHeight).toBe('1.8');

        handle.exec('lineHeight', null);
        expect(editor.querySelector('p').style.lineHeight).toBe('');
    });

    it('inserts horizontal rule', () => {
        const handle = attach(editor, { owner });

        handle.exec('insertHorizontalRule');
        expect(document.execCommand).toHaveBeenCalledWith('insertHorizontalRule', false, null);
    });

    it('inserts link with href, target and title', () => {
        const handle = attach(editor, { owner });

        handle.exec('createLink', {
            href: 'https://example.com',
            text: 'Example Site',
            title: 'Visit Example',
            openInNewTab: true,
        });

        const link = editor.querySelector('a');
        expect(link).not.toBeNull();
        expect(link.getAttribute('href')).toBe('https://example.com');
        expect(link.getAttribute('target')).toBe('_blank');
        expect(link.getAttribute('title')).toBe('Visit Example');
        expect(link.textContent).toBe('Example Site');
    });

    it('inserts anchor with id and name', () => {
        const handle = attach(editor, { owner });

        handle.exec('createLink', {
            isAnchor: true,
            anchorName: 'section-1',
        });

        const anchor = editor.querySelector('a[name="section-1"]');
        expect(anchor).not.toBeNull();
        expect(anchor.getAttribute('id')).toBe('section-1');
    });

    it('wraps the selected text when anchoring rather than inserting an empty anchor', () => {
        editor.innerHTML = '<p>Stage report</p>';
        const handle = attach(editor, { owner });

        const range = document.createRange();
        range.selectNodeContents(editor.querySelector('p'));
        const selection = window.getSelection();
        selection.removeAllRanges();
        selection.addRange(range);

        handle.exec('createLink', { isAnchor: true, anchorName: 'stage' });

        const anchor = editor.querySelector('a[name="stage"]');
        expect(anchor).not.toBeNull();
        expect(anchor.textContent).toBe('Stage report');
    });

    it('reports the saved selection text so a dialog can prefill from it', () => {
        editor.innerHTML = '<p>Stage report</p>';
        const handle = attach(editor, { owner });

        const range = document.createRange();
        range.selectNodeContents(editor.querySelector('p'));
        const selection = window.getSelection();
        selection.removeAllRanges();
        selection.addRange(range);
        document.dispatchEvent(new Event('selectionchange'));

        expect(handle.getSelectionText()).toBe('Stage report');
    });

    it('anchors the text a dialog was opened against even after the caret collapses', () => {
        editor.innerHTML = '<p>Stage report</p>';
        const handle = attach(editor, { owner });

        const range = document.createRange();
        range.selectNodeContents(editor.querySelector('p'));
        const selection = window.getSelection();
        selection.removeAllRanges();
        selection.addRange(range);
        document.dispatchEvent(new Event('selectionchange'));

        // Opening the dialog freezes the range; closing it collapses the live caret.
        handle.getSelectionText();
        selection.collapseToEnd();
        document.dispatchEvent(new Event('selectionchange'));

        handle.exec('createLink', { isAnchor: true, anchorName: 'stage' });

        const anchor = editor.querySelector('a[name="stage"]');
        expect(anchor).not.toBeNull();
        expect(anchor.textContent).toBe('Stage report');
    });

    it('inserts table with rows, columns and header', () => {
        const handle = attach(editor, { owner });

        handle.exec('insertTable', {
            rows: 3,
            columns: 2,
            includeHeader: true,
            caption: 'Results',
        });

        const table = editor.querySelector('table');
        expect(table).not.toBeNull();
        expect(table.querySelector('caption').textContent).toBe('Results');
        expect(table.querySelectorAll('thead th').length).toBe(2);
        expect(table.querySelectorAll('tbody tr').length).toBe(2);
    });

    it('inserts media and video embeds', () => {
        const handle = attach(editor, { owner });

        handle.exec('insertMedia', {
            source: '/media/photo.jpg',
            altText: 'A photo',
            caption: 'Photo caption',
            alignment: 'center',
        });

        const figure = editor.querySelector('figure.forma-composer-media');
        expect(figure).not.toBeNull();
        expect(figure.querySelector('img').getAttribute('src')).toBe('/media/photo.jpg');
        expect(figure.querySelector('figcaption').textContent).toBe('Photo caption');

        handle.exec('insertVideo', {
            source: 'https://youtube.com/watch?v=123',
            title: 'Race Highlights',
        });

        const video = editor.querySelector('.forma-composer-video');
        expect(video).not.toBeNull();
        expect(video.getAttribute('data-video-url')).toBe('https://youtube.com/watch?v=123');
    });

    it('stops reporting after disposal', () => {
        const handle = attach(editor, { owner, debounceDelay: 300 });

        handle.dispose();

        editor.innerHTML = '<p>Disposed typing</p>';
        editor.dispatchEvent(new Event('input', { bubbles: true }));
        vi.advanceTimersByTime(300);

        expect(owner.invokeMethodAsync).not.toHaveBeenCalled();
        expect(handle.exec('bold')).toBeNull();
    });
});

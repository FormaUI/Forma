import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { attach } from '#composer/RichTextEditor.js';
import { fakeOwner } from '#support/owner.js';

describe('RichTextEditor', () => {
    /** @type {HTMLElement} */
    let editor;
    /** @type {ReturnType<typeof fakeOwner>} */
    let owner;

    function stubClipboard(clipboard) {
        Object.defineProperty(navigator, 'clipboard', { value: clipboard, configurable: true });
        return clipboard;
    }

    beforeEach(() => {
        vi.useFakeTimers();
        document.body.innerHTML = '<div id="editor" contenteditable="true"><p>Hello world</p></div>';
        editor = document.querySelector('#editor');
        owner = fakeOwner();
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
        const range = document.createRange();
        range.selectNodeContents(editor.querySelector('p'));
        const selection = window.getSelection();
        selection.removeAllRanges();
        selection.addRange(range);

        const result = handle.exec('bold');

        expect(result).not.toBeNull();
        expect(result.html).toBe('<p><b>Hello world</b></p>');
    });

    it('converts the block the caret sits in', () => {
        const handle = attach(editor, { owner });
        const text = editor.querySelector('p').firstChild;
        const range = document.createRange();
        range.setStart(text, 2);
        range.collapse(true);
        const selection = window.getSelection();
        selection.removeAllRanges();
        selection.addRange(range);

        handle.exec('formatBlock', 'h1');

        expect(editor.innerHTML).toBe('<h1>Hello world</h1>');
    });

    it('converts every block a selection spans, leaving nested ones to their innermost', () => {
        editor.innerHTML = '<p>first</p><blockquote><p>second</p></blockquote><p>third</p>';
        const handle = attach(editor, { owner });
        const range = document.createRange();
        range.setStart(editor.querySelector('p').firstChild, 1);
        range.setEnd(editor.querySelectorAll('p')[2].firstChild, 3);
        const selection = window.getSelection();
        selection.removeAllRanges();
        selection.addRange(range);

        handle.exec('formatBlock', 'h2');

        expect(editor.innerHTML).toBe('<h2>first</h2><blockquote><h2>second</h2></blockquote><h2>third</h2>');
    });

    it('aligns the block the caret sits in', () => {
        const handle = attach(editor, { owner });
        const text = editor.querySelector('p').firstChild;
        const range = document.createRange();
        range.setStart(text, 2);
        range.collapse(true);
        const selection = window.getSelection();
        selection.removeAllRanges();
        selection.addRange(range);

        handle.exec('justifyCenter');

        expect(editor.querySelector('p').style.textAlign).toBe('center');
    });

    it('selects the whole canvas', () => {
        const handle = attach(editor, { owner });

        handle.exec('selectAll');

        expect(window.getSelection().toString()).toBe('Hello world');
    });

    it.each([
        ['foreColor', 'color', '#d32f2f'],
        ['hiliteColor', 'background-color', '#ffff00'],
        ['fontName', 'font-family', 'Georgia'],
    ])('writes %s as an inline style over the selection', (command, property, value) => {
        const handle = attach(editor, { owner });
        const range = document.createRange();
        range.selectNodeContents(editor.querySelector('p'));
        const selection = window.getSelection();
        selection.removeAllRanges();
        selection.addRange(range);

        handle.exec(command, value);

        const span = editor.querySelector('span');
        expect(span).not.toBeNull();
        expect(span.style.getPropertyValue(property)).toBe(value);
        expect(span.textContent).toBe('Hello world');
    });

    it('writes the font size exactly as the caller expressed it', () => {
        const handle = attach(editor, { owner });
        const range = document.createRange();
        range.selectNodeContents(editor.querySelector('p'));
        const selection = window.getSelection();
        selection.removeAllRanges();
        selection.addRange(range);

        handle.exec('fontSize', '1.5rem');

        expect(editor.querySelector('span').style.fontSize).toBe('1.5rem');
    });

    it('unwraps the span when a font size is cleared', () => {
        editor.innerHTML = '<p><span style="font-size: 1.5rem;">Hello world</span></p>';
        const handle = attach(editor, { owner });
        const range = document.createRange();
        range.selectNodeContents(editor.querySelector('p'));
        const selection = window.getSelection();
        selection.removeAllRanges();
        selection.addRange(range);

        handle.exec('fontSize', null);

        expect(editor.querySelector('span')).toBeNull();
        expect(editor.querySelector('p').innerHTML).toBe('Hello world');
    });

    it('writes semantic markup for bold rather than a styled span', () => {
        const handle = attach(editor, { owner });
        const range = document.createRange();
        range.selectNodeContents(editor.querySelector('p'));
        const selection = window.getSelection();
        selection.removeAllRanges();
        selection.addRange(range);

        handle.exec('bold');

        expect(editor.querySelector('b')).not.toBeNull();
        expect(editor.querySelector('span')).toBeNull();
    });

    it('reports colors as hex so they match the toolbar options', () => {
        editor.innerHTML = '<p><span style="color: rgb(211, 47, 47);">Hello world</span></p>';
        const handle = attach(editor, { owner });

        const range = document.createRange();
        range.selectNodeContents(editor.querySelector('span').firstChild);
        const selected = window.getSelection();
        selected.removeAllRanges();
        selected.addRange(range);

        const { selection } = handle.exec('bold');

        expect(selection.activeStyles).toContain('text-color:#d32f2f');
    });

    it('reports link and anchor as active styles so the toolbar can offer removal', () => {
        editor.innerHTML = '<p><a href="https://example.com">Site</a><a id="stage" name="stage">Stage</a></p>';
        const handle = attach(editor, { owner });
        const [link, anchor] = editor.querySelectorAll('a');
        const selected = window.getSelection();

        const linkRange = document.createRange();
        linkRange.selectNodeContents(link);
        selected.removeAllRanges();
        selected.addRange(linkRange);

        expect(handle.exec('bold').selection.activeStyles).toContain('link');

        const anchorRange = document.createRange();
        anchorRange.selectNodeContents(anchor);
        selected.removeAllRanges();
        selected.addRange(anchorRange);

        expect(handle.exec('bold').selection.activeStyles).toContain('anchor');
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

    it('inserts a horizontal rule between the split halves of the block', () => {
        const handle = attach(editor, { owner });
        const text = editor.querySelector('p').firstChild;
        const range = document.createRange();
        range.setStart(text, 5);
        range.collapse(true);
        const selection = window.getSelection();
        selection.removeAllRanges();
        selection.addRange(range);

        handle.exec('insertHorizontalRule');

        expect(editor.innerHTML).toBe('<p>Hello</p><hr><p> world</p>');
    });

    it('inserts link with href and target', () => {
        const handle = attach(editor, { owner });

        handle.exec('createLink', {
            href: 'https://example.com',
            name: 'Example Site',
            openInNewTab: true,
        });

        const link = editor.querySelector('a');
        expect(link).not.toBeNull();
        expect(link.getAttribute('href')).toBe('https://example.com');
        expect(link.getAttribute('target')).toBe('_blank');
        expect(link.textContent).toBe('Example Site');
    });

    it('falls back to the href when no name is supplied', () => {
        const handle = attach(editor, { owner });

        handle.exec('createLink', { href: 'https://example.com' });

        expect(editor.querySelector('a').textContent).toBe('https://example.com');
    });

    it('inserts anchor with id and name', () => {
        const handle = attach(editor, { owner });

        handle.exec('createLink', {
            isAnchor: true,
            name: 'section-1',
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

        handle.exec('createLink', { isAnchor: true, name: 'stage' });

        const anchor = editor.querySelector('a[name="stage"]');
        expect(anchor).not.toBeNull();
        expect(anchor.textContent).toBe('Stage report');
    });

    it('uses the selected text as the link name when the dialog leaves it blank', () => {
        editor.innerHTML = '<p>Stage report</p>';
        const handle = attach(editor, { owner });

        const range = document.createRange();
        range.selectNodeContents(editor.querySelector('p'));
        const selection = window.getSelection();
        selection.removeAllRanges();
        selection.addRange(range);

        handle.exec('createLink', { href: 'https://example.com' });

        expect(editor.querySelector('a').textContent).toBe('Stage report');
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

        handle.exec('createLink', { isAnchor: true, name: 'stage' });

        const anchor = editor.querySelector('a[name="stage"]');
        expect(anchor).not.toBeNull();
        expect(anchor.textContent).toBe('Stage report');
    });

    it('removes an anchor while keeping the text it wrapped', () => {
        editor.innerHTML = '<p><a id="stage" name="stage">Stage report</a></p>';
        const handle = attach(editor, { owner });

        const range = document.createRange();
        range.selectNodeContents(editor.querySelector('a'));
        const selection = window.getSelection();
        selection.removeAllRanges();
        selection.addRange(range);

        handle.exec('removeAnchor');

        expect(editor.querySelector('a')).toBeNull();
        expect(editor.textContent).toBe('Stage report');
    });

    it('keeps the formatting when pasting from a word processor', async () => {
        const html = '<p class="MsoNormal" style="mso-layout:none">Word junk <b>Bold bit</b></p>';
        stubClipboard({
            read: vi.fn(() => Promise.resolve([{
                types: ['text/plain', 'text/html'],
                getType: () => Promise.resolve(new Blob([html], { type: 'text/html' })),
            }])),
            readText: vi.fn(() => Promise.resolve('Word junk Bold bit')),
        });
        const handle = attach(editor, { owner });

        handle.exec('pasteWord');
        await vi.waitFor(() => expect(editor.querySelector('b')).not.toBeNull());

        expect(editor.textContent).toContain('Bold bit');
        expect(editor.innerHTML).not.toContain('mso-');
        expect(editor.innerHTML).not.toContain('MsoNormal');
    });

    it('falls back to plain text when the clipboard carries no markup', async () => {
        const clipboard = stubClipboard({
            read: vi.fn(() => Promise.resolve([{ types: ['text/plain'], getType: () => Promise.resolve(new Blob([''])) }])),
            readText: vi.fn(() => Promise.resolve('Plain only')),
        });
        const handle = attach(editor, { owner });

        handle.exec('pasteWord');
        await vi.waitFor(() => expect(clipboard.readText).toHaveBeenCalled());
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

    it('inserts images and video embeds', () => {
        const handle = attach(editor, { owner });

        handle.exec('insertImage', {
            source: '/media/photo.jpg',
            altText: 'A photo',
            caption: 'Photo caption',
        });

        const figure = editor.querySelector('figure.forma-composer-image');
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

    it('inserts an uncaptioned image without a figure', () => {
        const handle = attach(editor, { owner });

        handle.exec('insertImage', {
            source: '/media/plain.jpg',
            altText: 'Plain',
        });

        expect(editor.querySelector('figure')).toBeNull();

        const image = editor.querySelector('img.forma-composer-image');
        expect(image).not.toBeNull();
        expect(image.getAttribute('alt')).toBe('Plain');
    });

    it('carries autoplay onto the video placeholder only when asked for', () => {
        const handle = attach(editor, { owner });

        handle.exec('insertVideo', { source: 'https://vimeo.com/1', title: 'Quiet' });
        expect(editor.querySelector('.forma-composer-video').hasAttribute('data-autoplay')).toBe(false);

        handle.exec('insertVideo', { source: 'https://vimeo.com/2', title: 'Loud', autoplay: true });
        const placeholders = editor.querySelectorAll('.forma-composer-video');
        const loud = [...placeholders].find(node => node.getAttribute('data-video-title') === 'Loud');
        expect(loud.getAttribute('data-autoplay')).toBe('true');
    });

    it('renders a chosen resource according to what was asked of it', () => {
        const handle = attach(editor, { owner });

        handle.exec('insertResource', { source: '/media/a.png', name: 'a.png', kind: 'image', embed: true, link: false });
        expect(editor.querySelector('img.forma-composer-image').getAttribute('src')).toBe('/media/a.png');
        expect(editor.querySelector('a')).toBeNull();

        editor.innerHTML = '';
        handle.exec('insertResource', { source: '/media/b.mp4', name: 'b.mp4', kind: 'video', embed: true, link: true });
        expect(editor.querySelector('video').getAttribute('src')).toBe('/media/b.mp4');
        expect(editor.querySelector('a').textContent).toBe('b.mp4');

        editor.innerHTML = '';
        handle.exec('insertResource', { source: '/media/c.wav', name: 'c.wav', kind: 'audio', embed: true, link: false });
        expect(editor.querySelector('audio').getAttribute('src')).toBe('/media/c.wav');
    });

    it('links a resource the browser cannot render even when embedding was asked for', () => {
        const handle = attach(editor, { owner });

        handle.exec('insertResource', { source: '/media/report.pdf', name: 'report.pdf', kind: 'file', embed: true, link: false });

        expect(editor.querySelector('img')).toBeNull();
        expect(editor.querySelector('video')).toBeNull();
        expect(editor.querySelector('a').getAttribute('href')).toBe('/media/report.pdf');
    });

    it('reports a selection once until it changes', () => {
        attach(editor, { owner });
        const text = editor.querySelector('p').firstChild;
        const selection = window.getSelection();
        const place = (start, end) => {
            const range = document.createRange();
            range.setStart(text, start);
            range.setEnd(text, end);
            selection.removeAllRanges();
            selection.addRange(range);
        };
        const reports = () => owner.invokeMethodAsync.mock.calls.filter(([method]) => method === 'HandleSelectionChangedAsync');

        place(0, 5);
        document.dispatchEvent(new Event('selectionchange'));
        editor.dispatchEvent(new KeyboardEvent('keyup', { bubbles: true }));
        editor.dispatchEvent(new Event('click', { bubbles: true }));

        expect(reports()).toHaveLength(1);

        place(0, 11);
        document.dispatchEvent(new Event('selectionchange'));

        expect(reports()).toHaveLength(2);
        expect(reports()[1][1]).toMatchObject({ start: 0, end: 11 });
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

    describe('unlink', () => {
        function selectWithin(startNode, startOffset, endNode, endOffset) {
            const range = document.createRange();
            range.setStart(startNode, startOffset);
            range.setEnd(endNode, endOffset);
            const selection = window.getSelection();
            selection.removeAllRanges();
            selection.addRange(range);
            return range;
        }

        it('removes the link the caret sits inside without any selection', () => {
            editor.innerHTML = '<p>I have two <a href="https://a.test">first</a> and <a href="https://b.test">second</a> links</p>';
            const handle = attach(editor, { owner });
            const inside = editor.querySelectorAll('a')[0].firstChild;

            selectWithin(inside, 2, inside, 2);
            handle.exec('unlink');

            const remaining = [...editor.querySelectorAll('a')].map(a => a.getAttribute('href'));
            expect(remaining).toEqual(['https://b.test']);
            expect(editor.textContent).toContain('first');
        });

        it('removes every link a loose selection touches', () => {
            editor.innerHTML = '<p>two <a href="https://a.test">first</a> and <a href="https://b.test">second</a> links</p>';
            const handle = attach(editor, { owner });
            const paragraph = editor.querySelector('p');

            // Starts inside the leading text and ends inside the trailing text, covering neither link exactly.
            selectWithin(paragraph.firstChild, 1, paragraph.lastChild, 2);
            handle.exec('unlink');

            expect(editor.querySelectorAll('a')).toHaveLength(0);
            expect(editor.textContent).toContain('first');
            expect(editor.textContent).toContain('second');
        });

        it('leaves every link alone when the caret is outside them all', () => {
            editor.innerHTML = '<p>plain <a href="https://a.test">link</a> here</p>';
            const handle = attach(editor, { owner });
            const trailing = editor.querySelector('p').lastChild;

            selectWithin(trailing, 1, trailing, 1);
            handle.exec('unlink');

            expect(editor.querySelectorAll('a')).toHaveLength(1);
        });

        it('removes the named anchor the caret sits inside', () => {
            editor.innerHTML = '<p>see <a name="results">the results</a> below</p>';
            const handle = attach(editor, { owner });
            const inside = editor.querySelector('a').firstChild;

            selectWithin(inside, 3, inside, 3);
            handle.exec('removeAnchor');

            expect(editor.querySelectorAll('a')).toHaveLength(0);
            expect(editor.textContent).toContain('the results');
        });

        it('reports a link the selection merely overlaps, so the toggle offers removal', () => {
            editor.innerHTML = '<p>two <a href="https://a.test">first</a> and <a href="https://b.test">second</a> links</p>';
            const handle = attach(editor, { owner });
            const paragraph = editor.querySelector('p');

            // Begins in plain text ahead of the first link and ends in plain text after the second.
            selectWithin(paragraph.firstChild, 1, paragraph.lastChild, 2);
            const { selection } = handle.exec('insertText', '');

            expect(selection.activeStyles).toContain('link');
        });

        it('reports no link when the selection touches none', () => {
            editor.innerHTML = '<p>plain text only</p>';
            const handle = attach(editor, { owner });
            const text = editor.querySelector('p').firstChild;

            selectWithin(text, 1, text, 4);
            const { selection } = handle.exec('insertText', '');

            expect(selection.activeStyles).not.toContain('link');
        });

        it('offers the address back when the selection touches exactly one link', () => {
            editor.innerHTML = '<p>text <a href="https://a.test">library ships is</a> more</p>';
            const handle = attach(editor, { owner });
            const par = editor.querySelector('p');
            const texts = [];
            const walker = document.createTreeWalker(par, NodeFilter.SHOW_TEXT);
            while (walker.nextNode()) texts.push(walker.currentNode);

            // Starts in the text ahead of the link and ends inside it, covering it only partly.
            selectWithin(texts[0], 2, texts[1], 7);
            const target = handle.beginLinkDialog();

            expect(target.count).toBe(1);
            expect(target.href).toBe('https://a.test');
            expect(target.text).toContain('library');
        });

        it('offers no address when the selection touches several links', () => {
            editor.innerHTML = '<p>a <a href="https://a.test">one</a> and <a href="https://b.test">two</a> b</p>';
            const handle = attach(editor, { owner });
            const par = editor.querySelector('p');
            const texts = [];
            const walker = document.createTreeWalker(par, NodeFilter.SHOW_TEXT);
            while (walker.nextNode()) texts.push(walker.currentNode);

            selectWithin(texts[0], 0, texts[texts.length - 1], 1);
            const target = handle.beginLinkDialog();

            expect(target.count).toBe(2);
            expect(target.href).toBeNull();
            expect(target.text).toContain('one');
            expect(target.text).toContain('two');
        });

        it('replaces every link the selection covered with the new one', () => {
            editor.innerHTML = '<p>a <a href="https://a.test">one</a> and <a href="https://b.test">two</a> b</p>';
            const handle = attach(editor, { owner });
            const par = editor.querySelector('p');
            const texts = [];
            const walker = document.createTreeWalker(par, NodeFilter.SHOW_TEXT);
            while (walker.nextNode()) texts.push(walker.currentNode);

            selectWithin(texts[0], 0, texts[texts.length - 1], 1);
            handle.beginLinkDialog();
            handle.exec('createLink', { href: 'https://new.test', name: 'everything' });

            const hrefs = [...editor.querySelectorAll('a')].map(a => a.getAttribute('href'));
            expect(hrefs).toEqual(['https://new.test']);
        });

        it('does not remove a named anchor when unlinking a hyperlink', () => {
            editor.innerHTML = '<p>see <a name="results">the results</a> below</p>';
            const handle = attach(editor, { owner });
            const inside = editor.querySelector('a').firstChild;

            selectWithin(inside, 3, inside, 3);
            handle.exec('unlink');

            expect(editor.querySelectorAll('a[name]')).toHaveLength(1);
        });
    });

    describe('text insertion', () => {
        function selectWithin(startNode, startOffset, endNode, endOffset) {
            const range = document.createRange();
            range.setStart(startNode, startOffset);
            range.setEnd(endNode, endOffset);
            const selection = window.getSelection();
            selection.removeAllRanges();
            selection.addRange(range);
            return range;
        }

        it('inserts at the saved caret after focus has moved to a toolbar control', () => {
            const handle = attach(editor, { owner });
            const text = editor.querySelector('p').firstChild;

            // The caret sits after "Hello", the editor saves it on click, then the selection leaves the canvas.
            selectWithin(text, 5, text, 5);
            editor.dispatchEvent(new Event('click', { bubbles: true }));
            window.getSelection().removeAllRanges();

            handle.exec('insertText', '\u{1F600}');

            expect(editor.textContent).toBe('Hello\u{1F600} world');
        });

        it('replaces the saved selection, as typing over it would', () => {
            const handle = attach(editor, { owner });
            const text = editor.querySelector('p').firstChild;

            selectWithin(text, 0, text, 5);
            editor.dispatchEvent(new Event('click', { bubbles: true }));
            window.getSelection().removeAllRanges();

            handle.exec('insertText', '\u{1F600}');

            expect(editor.textContent).toBe('\u{1F600} world');
        });

        it('lands consecutive inserts side by side', () => {
            const handle = attach(editor, { owner });
            const text = editor.querySelector('p').firstChild;

            selectWithin(text, 5, text, 5);
            editor.dispatchEvent(new Event('click', { bubbles: true }));

            handle.exec('insertText', '\u{1F600}');
            handle.exec('insertText', '\u{1F98E}');

            expect(editor.textContent).toBe('Hello\u{1F600}\u{1F98E} world');
        });

        it('appends at the end when no selection was ever made in the canvas', () => {
            const handle = attach(editor, { owner });

            window.getSelection().removeAllRanges();
            handle.exec('insertText', '\u{1F600}');

            expect(editor.textContent).toBe('Hello world\u{1F600}');
        });

        it('does not restore a selection that new content has detached', () => {
            const handle = attach(editor, { owner });
            const text = editor.querySelector('p').firstChild;

            selectWithin(text, 0, text, 5);
            editor.dispatchEvent(new Event('click', { bubbles: true }));

            // The host pushes fresh content, detaching every node the saved range pointed into.
            handle.setHtml('<p>Replaced content</p>');
            window.getSelection().removeAllRanges();

            handle.exec('insertText', '\u{1F600}');

            expect(editor.textContent).toBe('Replaced content\u{1F600}');
        });

        it('leaves the content alone when there is nothing to insert', () => {
            const handle = attach(editor, { owner });

            handle.exec('insertText', '');

            expect(editor.textContent).toBe('Hello world');
        });
    });

    describe('inline formatting engine', () => {
        function select(startNode, startOffset, endNode, endOffset) {
            const range = document.createRange();
            range.setStart(startNode, startOffset);
            range.setEnd(endNode, endOffset);
            const selection = window.getSelection();
            selection.removeAllRanges();
            selection.addRange(range);
            return range;
        }

        it('marks only the covered part of a word', () => {
            const handle = attach(editor, { owner });
            const text = editor.querySelector('p').firstChild;

            select(text, 0, text, 5);
            handle.exec('bold');

            expect(editor.querySelector('p').innerHTML).toBe('<b>Hello</b> world');
        });

        it('removes the mark again when toggled over the same selection', () => {
            const handle = attach(editor, { owner });
            const text = editor.querySelector('p').firstChild;

            select(text, 0, text, 5);
            handle.exec('bold');
            handle.exec('bold');

            expect(editor.querySelector('b')).toBeNull();
            expect(editor.textContent).toBe('Hello world');
        });

        it('unmarks just the selected middle of a marked run', () => {
            editor.innerHTML = '<p><b>Hello world</b></p>';
            const handle = attach(editor, { owner });
            const text = editor.querySelector('b').firstChild;

            select(text, 2, text, 8);
            handle.exec('bold');

            const html = editor.querySelector('p').innerHTML;
            expect(html).toBe('<b>He</b>llo wo<b>rld</b>');
        });

        it('extends a mark over a selection that is only partly marked', () => {
            editor.innerHTML = '<p><b>Hello</b> world</p>';
            const handle = attach(editor, { owner });
            const bold = editor.querySelector('b').firstChild;
            const tail = editor.querySelector('p').lastChild;

            select(bold, 0, tail, 6);
            handle.exec('bold');

            expect(editor.querySelectorAll('b')).toHaveLength(1);
            expect(editor.querySelector('b').textContent).toBe('Hello world');
        });

        it('marks each paragraph of a cross-block selection without wrapping the blocks', () => {
            editor.innerHTML = '<p>first</p><p>second</p>';
            const handle = attach(editor, { owner });
            const [one, two] = editor.querySelectorAll('p');

            select(one.firstChild, 0, two.firstChild, 6);
            handle.exec('italic');

            expect(editor.innerHTML).toBe('<p><i>first</i></p><p><i>second</i></p>');
        });

        it('arms a mark at the caret so the next typed text carries it', () => {
            const handle = attach(editor, { owner });
            const text = editor.querySelector('p').firstChild;

            select(text, 5, text, 5);
            handle.exec('bold');

            const armed = editor.querySelector('b');
            expect(armed).not.toBeNull();
            expect(window.getSelection().anchorNode.parentElement).toBe(armed);
        });

        it('keeps armed markers out of the reported content', () => {
            const handle = attach(editor, { owner });
            const text = editor.querySelector('p').firstChild;

            select(text, 5, text, 5);
            const { html } = handle.exec('bold');

            expect(html).toBe('<p>Hello world</p>');
        });

        it('dissolves an armed mark nothing was typed into when focus leaves', () => {
            const handle = attach(editor, { owner });
            const text = editor.querySelector('p').firstChild;

            select(text, 5, text, 5);
            handle.exec('bold');
            editor.dispatchEvent(new Event('blur', { bubbles: true }));

            expect(editor.querySelector('b')).toBeNull();
        });

        it('strips marks and styles together, leaving links standing', () => {
            editor.innerHTML = '<p><b>bold</b> <span style="color: red;">red</span> <a href="https://a.test">link</a></p>';
            const handle = attach(editor, { owner });
            const paragraph = editor.querySelector('p');

            select(paragraph.firstChild ?? paragraph, 0, paragraph.lastChild, paragraph.lastChild.childNodes.length);
            const range = document.createRange();
            range.selectNodeContents(paragraph);
            const selection = window.getSelection();
            selection.removeAllRanges();
            selection.addRange(range);
            handle.exec('removeFormat');

            expect(editor.querySelector('b')).toBeNull();
            expect(editor.querySelector('span')).toBeNull();
            expect(editor.querySelector('a')).not.toBeNull();
            expect(editor.textContent).toBe('bold red link');
        });
    });

    describe('list engine', () => {
        function selectContents(node) {
            const range = document.createRange();
            range.selectNodeContents(node);
            const selection = window.getSelection();
            selection.removeAllRanges();
            selection.addRange(range);
        }

        it('turns adjacent paragraphs into one list', () => {
            editor.innerHTML = '<p>one</p><p>two</p>';
            const handle = attach(editor, { owner });

            selectContents(editor);
            handle.exec('insertUnorderedList');

            expect(editor.innerHTML).toBe('<ul><li>one</li><li>two</li></ul>');
        });

        it('dissolves a list back into paragraphs when toggled again', () => {
            editor.innerHTML = '<ul><li>one</li><li>two</li></ul>';
            const handle = attach(editor, { owner });

            selectContents(editor);
            handle.exec('insertUnorderedList');

            expect(editor.innerHTML).toBe('<p>one</p><p>two</p>');
        });

        it('re-types a whole list into the other kind', () => {
            editor.innerHTML = '<ul><li>one</li><li>two</li></ul>';
            const handle = attach(editor, { owner });

            selectContents(editor);
            handle.exec('insertOrderedList');

            expect(editor.innerHTML).toBe('<ol><li>one</li><li>two</li></ol>');
        });

        it('nests an item under its predecessor on indent', () => {
            editor.innerHTML = '<ul><li>one</li><li>two</li></ul>';
            const handle = attach(editor, { owner });
            const second = editor.querySelectorAll('li')[1];

            selectContents(second.firstChild);
            handle.exec('indent');

            expect(editor.innerHTML).toBe('<ul><li>one<ul><li>two</li></ul></li></ul>');
        });

        it('lifts a nested item back out on outdent', () => {
            editor.innerHTML = '<ul><li>one<ul><li>two</li></ul></li></ul>';
            const handle = attach(editor, { owner });
            const nested = editor.querySelector('ul ul li');

            selectContents(nested.firstChild);
            handle.exec('outdent');

            expect(editor.innerHTML).toBe('<ul><li>one</li><li>two</li></ul>');
        });

        it('indents a paragraph by a reading-direction-aware margin', () => {
            const handle = attach(editor, { owner });
            const paragraph = editor.querySelector('p');

            selectContents(paragraph.firstChild);
            handle.exec('indent');
            expect(paragraph.style.marginInlineStart).toBe('40px');

            handle.exec('outdent');
            expect(paragraph.style.marginInlineStart).toBe('');
        });
    });

    describe('reopening what a dialog built', () => {
        function doubleClick(node) {
            node.dispatchEvent(new MouseEvent('dblclick', { bubbles: true, cancelable: true }));
        }

        it('reports an image with what it already holds', () => {
            editor.innerHTML = '<figure class="forma-composer-image"><img src="/cat.png" alt="A cat" /><figcaption>Tiddles</figcaption></figure>';
            attach(editor, { owner });

            doubleClick(editor.querySelector('img'));

            expect(owner.invokeMethodAsync).toHaveBeenCalledWith('HandleImageActivatedAsync', {
                source: '/cat.png',
                altText: 'A cat',
                caption: 'Tiddles',
            });
        });

        it('reports a video with what it already holds', () => {
            editor.innerHTML = '<div class="forma-composer-video" data-video-url="https://example.com/v" data-video-title="Clip" data-autoplay="true"><p>Clip</p></div>';
            attach(editor, { owner });

            doubleClick(editor.querySelector('.forma-composer-video p'));

            expect(owner.invokeMethodAsync).toHaveBeenCalledWith('HandleVideoActivatedAsync', {
                source: 'https://example.com/v',
                title: 'Clip',
                autoplay: true,
            });
        });

        it('reports an embed with what it already holds', () => {
            editor.innerHTML = '<div class="forma-composer-embed" data-embed-type="form" data-embed-id="42" data-settings="wide">[Embed: form]</div>';
            attach(editor, { owner });

            doubleClick(editor.querySelector('.forma-composer-embed'));

            expect(owner.invokeMethodAsync).toHaveBeenCalledWith('HandleEmbedActivatedAsync', {
                embedType: 'form',
                identifier: '42',
                settings: 'wide',
            });
        });

        it('reports a link so its own dialog reopens on it', () => {
            editor.innerHTML = '<p>Read <a href="https://example.com">the notice</a> first.</p>';
            attach(editor, { owner });

            doubleClick(editor.querySelector('a[href]'));

            expect(owner.invokeMethodAsync).toHaveBeenCalledWith('HandleLinkActivatedAsync', false);
        });

        it('replaces the element rather than adding a second, even after a dialog took the caret', () => {
            editor.innerHTML = '<p>before</p><figure class="forma-composer-image"><img src="/cat.png" alt="A cat" /></figure>';
            const handle = attach(editor, { owner });

            doubleClick(editor.querySelector('img'));

            // What closing a dialog does: hands the canvas a collapsed caret somewhere else entirely.
            const elsewhere = document.createRange();
            elsewhere.selectNodeContents(editor.querySelector('p').firstChild);
            elsewhere.collapse(true);
            const selection = window.getSelection();
            selection.removeAllRanges();
            selection.addRange(elsewhere);

            handle.exec('insertHtml', '<figure class="forma-composer-image"><img src="/dog.png" alt="A dog" /></figure>');

            expect(editor.querySelectorAll('img')).toHaveLength(1);
            expect(editor.querySelector('img').getAttribute('src')).toBe('/dog.png');
        });

        it('says nothing about plain text, which no dialog built', () => {
            attach(editor, { owner });

            doubleClick(editor.querySelector('p'));

            expect(owner.invokeMethodAsync).not.toHaveBeenCalledWith('HandleImageActivatedAsync', expect.anything());
        });
    });

});

import { describe, it, expect } from 'vitest';

import { sanitize } from '#core/Sanitizer.js';

import addresses from '../../support/address-vectors.json';

describe('Sanitizer', () => {
    it('keeps allowed elements and their formatting', () => {
        const result = sanitize('<p>Base <b>Bold bit</b> and <em>emphasis</em></p>');

        expect(result).toBe('<p>Base <b>Bold bit</b> and <em>emphasis</em></p>');
    });

    it('strips event handler attributes that would otherwise run on insertion', () => {
        const result = sanitize('<img src="https://example.com/a.png" onerror="alert(1)" onload="alert(2)">');

        expect(result).not.toContain('onerror');
        expect(result).not.toContain('onload');
        expect(result).toContain('src="https://example.com/a.png"');
    });

    it('discards script and style along with their content', () => {
        const result = sanitize('<p>Keep</p><script>alert(1)</script><style>p{color:red}</style>');

        expect(result).toBe('<p>Keep</p>');
    });

    it('drops a javascript URL while keeping a safe one', () => {
        expect(sanitize('<a href="javascript:alert(1)">x</a>')).not.toContain('href');
        expect(sanitize('<a href="https://example.com">x</a>')).toContain('href="https://example.com"');
        expect(sanitize('<a href="#section">x</a>')).toContain('href="#section"');
    });

    it('unwraps a disallowed element rather than losing the text inside it', () => {
        const result = sanitize('<p>Before <marquee>middle</marquee> after</p>');

        expect(result).toBe('<p>Before middle after</p>');
    });

    it('removes the artefacts a word processor leaves behind', () => {
        const result = sanitize(
            '<!--[if gte mso 9]><xml>junk</xml><![endif]-->'
            + '<p class="MsoNormal ownClass" style="mso-layout:none;color:red"><o:p>hidden</o:p>Body</p>');

        expect(result).not.toContain('mso-');
        expect(result).not.toContain('MsoNormal');
        expect(result).not.toContain('<o:p>');
        expect(result).toContain('ownClass');
        expect(result).toContain('color:red');
        expect(result).toContain('Body');
    });

    it('drops a style rule that fetches an address or runs an expression', () => {
        const result = sanitize('<p style="color:red;background-image:url(https://example.com/track);width:expression(alert(1))">Body</p>');

        expect(result).toBe('<p style="color:red;">Body</p>');
    });

    it('removes a style attribute left empty after its unsafe rules are dropped', () => {
        const result = sanitize('<p style="mso-layout:none">Body</p>');

        expect(result).toBe('<p>Body</p>');
    });

    it('rejects a non-image data URL', () => {
        expect(sanitize('<img src="data:text/html;base64,PHN2Zz4=">')).not.toContain('src');
        expect(sanitize('<img src="data:image/png;base64,iVBOR">')).toContain('src');
    });

    // A script's address: plain, in mixed case, behind spaces and control characters, split by a tab, in VBScript, and as a page of HTML.
    const hostileAddresses = [
        'javascript:alert(1)',
        'JaVaScRiPt:alert(1)',
        ' \u0001\u001fjavascript:alert(1)',
        'java\tscript:alert(1)',
        'vbscript:msgbox(1)',
        'data:text/html,<script>alert(1)</script>',
    ];

    it.each(hostileAddresses)('drops %j from a link and from an image', address => {
        const link = document.createElement('a');
        link.setAttribute('href', address);
        link.textContent = 'Results';
        const image = document.createElement('img');
        image.setAttribute('src', address);

        const result = sanitize(link.outerHTML + image.outerHTML);

        expect(result).toBe('<a>Results</a><img>');
    });

    it('drops a script and an image error handler from markup', () => {
        const result = sanitize('<p>Stage<script>alert(1)</script><img src=x onerror="alert(1)"></p>');

        expect(result).toBe('<p>Stage<img src="x"></p>');
    });

    it('keeps a link\'s web, mail and telephone addresses and drops a data or blob one', () => {
        expect(sanitize('<a href="mailto:editor@example.com">x</a>')).toContain('href="mailto:editor@example.com"');
        expect(sanitize('<a href="tel:+573001234567">x</a>')).toContain('href="tel:+573001234567"');
        expect(sanitize('<a href="/news/stage-12">x</a>')).toContain('href="/news/stage-12"');
        expect(sanitize('<a href="data:image/png;base64,iVBOR">x</a>')).not.toContain('href');
        expect(sanitize('<a href="blob:https://example.com/8f4b0c2e">x</a>')).not.toContain('href');
    });

    it('keeps a source only of its element\'s own kind', () => {
        expect(sanitize('<img src="data:image/svg+xml,<svg onload=alert(1)>">')).not.toContain('src');
        expect(sanitize('<img src="mailto:editor@example.com">')).not.toContain('src');
        expect(sanitize('<img src="blob:https://example.com/8f4b0c2e">')).toContain('src');
        expect(sanitize('<video src="data:video/mp4;base64,AAAA" poster="data:image/png;base64,iVBOR"></video>')).toBe('<video src="data:video/mp4;base64,AAAA" poster="data:image/png;base64,iVBOR"></video>');
        expect(sanitize('<video src="data:image/png;base64,iVBOR"></video>')).not.toContain('src');
        expect(sanitize('<audio src="data:audio/mpeg;base64,AAAA"></audio>')).toContain('src');
        expect(sanitize('<audio src="data:video/mp4;base64,AAAA"></audio>')).not.toContain('src');
    });

    // The addresses the C# address policy's tests read too, so the two rules judge each one alike.
    it.each(addresses.vectors)('judges the $kind address $address as the shared addresses say', ({ kind, address, allowed }) => {
        const attribute = kind === 'link' ? 'href' : 'src';
        const element = document.createElement({ link: 'a', image: 'img', video: 'video', audio: 'audio' }[kind]);
        element.setAttribute(attribute, address);
        const template = document.createElement('template');

        template.innerHTML = sanitize(element.outerHTML);

        expect(template.content.firstElementChild.hasAttribute(attribute)).toBe(allowed);
    });});

import { describe, it, expect } from 'vitest';

import { sanitize } from '#core/Sanitizer.js';

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

    it('removes a style attribute left empty after its unsafe rules are dropped', () => {
        const result = sanitize('<p style="mso-layout:none">Body</p>');

        expect(result).toBe('<p>Body</p>');
    });

    it('rejects a non-image data URL', () => {
        expect(sanitize('<img src="data:text/html;base64,PHN2Zz4=">')).not.toContain('src');
        expect(sanitize('<img src="data:image/png;base64,iVBOR">')).toContain('src');
    });
});

import { describe, it, expect, beforeEach } from 'vitest';
import { compose, composePage } from '#browser/operations/Print.js';

describe('Print', () => {
    beforeEach(() => {
        document.documentElement.lang = 'he';
        document.documentElement.dir = 'rtl';
        document.head.innerHTML = '<style>.prose { color: rgb(1, 2, 3); }</style>';
        document.body.innerHTML = '<main><h1>Orders</h1></main><script>window.ran = true;</script>';
    });

    // Reads a composed document back, as the host's file holds it.
    function read(written) {
        return new DOMParser().parseFromString(written, 'text/html');
    }

    describe('composing a document', () => {
        it('writes the markup alone into a document of its own, titled, in the page\'s language and direction', () => {
            // Act
            const written = read(compose('<p>Report</p>', 'Quarter', 'prose').value);

            // Assert
            expect(written.title).toBe('Quarter');
            expect(written.documentElement.lang).toBe('he');
            expect(written.documentElement.dir).toBe('rtl');
            expect(written.body.innerHTML).toBe('<article class="prose"><p>Report</p></article>');
        });

        it('writes the page\'s own styles into the document', () => {
            // Act
            const written = read(compose('<p>Report</p>', 'Quarter', 'prose').value);

            // Assert
            expect(written.querySelector('style').textContent).toContain('.prose');
        });

        it('starts the document with its doctype and its character set', () => {
            // Act
            const written = compose('<p>Report</p>', 'Quarter', 'prose').value;

            // Assert
            expect(written.startsWith('<!DOCTYPE html>')).toBe(true);
            expect(read(written).querySelector('meta[charset]').getAttribute('charset')).toBe('utf-8');
        });
    });

    describe('composing the page', () => {
        it('writes the page as it stands, titled, leaving its scripts out', () => {
            // Act
            const written = read(composePage('Orders').value);

            // Assert
            expect(written.title).toBe('Orders');
            expect(written.querySelector('h1').textContent).toBe('Orders');
            expect(written.querySelector('script')).toBeNull();
            expect(document.querySelector('script')).not.toBeNull();
        });
    });
});

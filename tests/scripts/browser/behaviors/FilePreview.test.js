import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { attach } from '#browser/behaviors/FilePreview.js';
import { stubObjectUrls } from '#support/platform.js';

describe('FilePreview', () => {
    /** @type {ReturnType<typeof stubObjectUrls>} */
    let urls;
    /** @type {HTMLElement} */
    let picker;

    beforeEach(() => {
        urls = stubObjectUrls();
        document.body.innerHTML = `
            <div id="picker">
                <input type="file" />
                <img data-forma-preview alt="" />
                <img data-forma-preview alt="" />
            </div>`;
        picker = document.querySelector('#picker');
    });

    afterEach(() => urls.restore());

    const selectFiles = (...names) => {
        const files = names.map(name => ({ name }));
        Object.defineProperty(picker.querySelector('input'), 'files', { value: files, configurable: true });
    };

    const images = () => [...picker.querySelectorAll('[data-forma-preview]')];

    it('renders a thumbnail for each named file', () => {
        selectFiles('one.png', 'two.png');
        const handle = attach(picker);

        handle.show(['one.png', 'two.png']);

        expect(images().map(image => image.getAttribute('src'))).toEqual(urls.created);
    });

    it('leaves a thumbnail blank when no file matches its name', () => {
        selectFiles('one.png');
        const handle = attach(picker);

        handle.show(['one.png', 'missing.png']);

        expect(images()[1].hasAttribute('src')).toBe(false);
    });

    it('releases a thumbnail once its image has loaded', () => {
        selectFiles('one.png');
        const handle = attach(picker);
        handle.show(['one.png']);

        images()[0].dispatchEvent(new Event('load'));

        expect(urls.outstanding).toHaveLength(0);
    });

    it('releases and blanks a thumbnail whose image failed', () => {
        selectFiles('one.png');
        const handle = attach(picker);
        handle.show(['one.png']);

        images()[0].dispatchEvent(new Event('error'));

        expect(urls.outstanding).toHaveLength(0);
        expect(images()[0].hasAttribute('src')).toBe(false);
    });

    it('releases the previous thumbnails when shown again', () => {
        selectFiles('one.png');
        const handle = attach(picker);
        handle.show(['one.png']);

        handle.show(['one.png']);

        expect(urls.revoked).toContain(urls.created[0]);
    });

    it('releases the thumbnails it holds when cleared', () => {
        selectFiles('one.png', 'two.png');
        const handle = attach(picker);
        handle.show(['one.png', 'two.png']);

        handle.clear();

        expect(urls.outstanding).toHaveLength(0);
    });

    // An image removed before it loaded fires neither load nor error.
    it('releases thumbnails that never settled when disposed', () => {
        selectFiles('one.png', 'two.png');
        const handle = attach(picker);
        handle.show(['one.png', 'two.png']);

        handle.dispose();

        expect(urls.outstanding).toHaveLength(0);
    });

    it('does nothing without a file input to read', () => {
        picker.querySelector('input').remove();
        const handle = attach(picker);

        handle.show(['one.png']);

        expect(urls.created).toHaveLength(0);
    });
});

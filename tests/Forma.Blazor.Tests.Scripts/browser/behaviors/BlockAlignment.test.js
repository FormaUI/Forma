import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { attach } from '#browser/behaviors/BlockAlignment.js';
import { stubAnimationFrames, stubResizeObserver, stackRows } from '#support/platform.js';

describe('BlockAlignment', () => {
    /** @type {ReturnType<typeof stubAnimationFrames>} */
    let frames;
    /** @type {ReturnType<typeof stubResizeObserver>} */
    let resizes;
    /** @type {HTMLElement} */
    let gutter;
    /** @type {HTMLElement} */
    let canvas;

    beforeEach(() => {
        frames = stubAnimationFrames();
        resizes = stubResizeObserver();
        document.body.innerHTML = `
            <div id="editor">
                <div id="gutter">
                    <div class="row" data-block="0"></div>
                    <div class="row" data-block="2"></div>
                    <div class="row" data-block="5"></div>
                </div>
                <div id="canvas"><p>one</p><p>two</p><p>three</p></div>
            </div>`;
        gutter = document.querySelector('#gutter');
        canvas = document.querySelector('#canvas');
        gutter.getBoundingClientRect = () => ({ top: 200, bottom: 400, height: 200, left: 0, right: 24, width: 24, x: 0, y: 200 });
        stackRows([...canvas.children], { height: 40, top: 216 });
    });

    afterEach(() => {
        frames.restore();
        resizes.restore();
    });

    const rows = () => [...gutter.querySelectorAll('.row')];

    it('places each row level with the block it names', () => {
        attach(gutter, { source: canvas, rowSelector: '.row' });

        expect(rows()[0].style.insetBlockStart).toBe('16px');
        expect(rows()[1].style.insetBlockStart).toBe('96px');
    });

    it('hides a row naming a block the document no longer has', () => {
        attach(gutter, { source: canvas, rowSelector: '.row' });

        expect(rows()[2].hidden).toBe(true);
        expect(rows()[0].hidden).toBe(false);
    });

    it('places the rows again in the next frame after the document scrolls', () => {
        attach(gutter, { source: canvas, rowSelector: '.row' });
        stackRows([...canvas.children], { height: 40, top: 176 });

        canvas.dispatchEvent(new Event('scroll'));
        expect(rows()[0].style.insetBlockStart).toBe('16px');
        frames.flush();

        expect(rows()[0].style.insetBlockStart).toBe('-24px');
    });

    it('places the rows again after the document changes size', () => {
        attach(gutter, { source: canvas, rowSelector: '.row' });
        stackRows([...canvas.children], { height: 60, top: 216 });

        resizes.latest().deliver([]);
        frames.flush();

        expect(rows()[1].style.insetBlockStart).toBe('136px');
    });

    // A change to the document's blocks is observed rather than announced, so the rows follow it on their own.
    it('shows a row again once its block exists', async () => {
        attach(gutter, { source: canvas, rowSelector: '.row' });
        canvas.insertAdjacentHTML('beforeend', '<p>four</p><p>five</p><p>six</p>');
        stackRows([...canvas.children], { height: 40, top: 216 });

        await new Promise(resolve => setTimeout(resolve, 0));
        frames.flush();

        expect(rows()[2].hidden).toBe(false);
        expect(rows()[2].style.insetBlockStart).toBe('216px');
    });

    it('places a row Blazor adds beside its block', async () => {
        attach(gutter, { source: canvas, rowSelector: '.row' });
        gutter.insertAdjacentHTML('beforeend', '<div class="row" data-block="1" hidden></div>');

        await new Promise(resolve => setTimeout(resolve, 0));
        frames.flush();

        expect(rows()[3].hidden).toBe(false);
        expect(rows()[3].style.insetBlockStart).toBe('56px');
    });

    it('stops following once disposed', () => {
        const handle = attach(gutter, { source: canvas, rowSelector: '.row' });
        handle.dispose();
        stackRows([...canvas.children], { height: 40, top: 176 });

        canvas.dispatchEvent(new Event('scroll'));
        frames.flush();

        expect(rows()[0].style.insetBlockStart).toBe('16px');
    });

    it('refuses to attach without the element whose blocks the rows follow', () => {
        expect(() => attach(gutter, { rowSelector: '.row' })).toThrow();
    });
});

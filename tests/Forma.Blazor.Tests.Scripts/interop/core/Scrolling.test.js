import { describe, it, expect, beforeEach } from 'vitest';
import { Scrolling } from '#interop/core/Scrolling.js';
import { sizeScroller } from '#support/platform.js';

describe('Scrolling', () => {
    /** @type {HTMLElement} */
    let panel;
    /** @type {HTMLElement} */
    let inner;
    /** @type {HTMLElement} */
    let leaf;

    beforeEach(() => {
        document.body.innerHTML = '<div id="panel" style="overflow-y: auto"><div id="inner"><span id="leaf">Leaf</span></div></div>';
        panel = document.querySelector('#panel');
        inner = document.querySelector('#inner');
        leaf = document.querySelector('#leaf');
    });

    it('says an element scrolls when its content outgrows its height', () => {
        sizeScroller(panel, { scrollHeight: 400, clientHeight: 200 });

        expect(Scrolling.scrolls(panel)).toBe(true);
    });

    it('says an element does not scroll when its content fits', () => {
        sizeScroller(panel, { scrollHeight: 200, clientHeight: 200 });

        expect(Scrolling.scrolls(panel)).toBe(false);
    });

    it('takes an overflowing element as a scroller only when its overflow lets it scroll', () => {
        sizeScroller(panel, { scrollHeight: 400, clientHeight: 200 });
        sizeScroller(inner, { scrollHeight: 400, clientHeight: 200 });

        expect(Scrolling.isScroller(panel)).toBe(true);
        expect(Scrolling.isScroller(inner)).toBe(false);
    });

    it('finds the nearest scrolling ancestor of an element', () => {
        sizeScroller(panel, { scrollHeight: 400, clientHeight: 200 });

        expect(Scrolling.nearestScroller(leaf)).toBe(panel);
    });

    it('answers with the element itself when it is the scroller', () => {
        sizeScroller(panel, { scrollHeight: 400, clientHeight: 200 });

        expect(Scrolling.nearestScroller(panel)).toBe(panel);
    });

    it('stops the walk short of the element it was told to stop at', () => {
        sizeScroller(panel, { scrollHeight: 400, clientHeight: 200 });

        expect(Scrolling.nearestScroller(leaf, panel)).toBeNull();
    });

    it('finds no scroller when nothing on the way scrolls', () => {
        sizeScroller(panel, { scrollHeight: 200, clientHeight: 200 });

        expect(Scrolling.nearestScroller(leaf)).toBeNull();
    });
});

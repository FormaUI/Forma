import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { attach as attachPlace } from '#showcase/ReadingPlace.js';
import { sizeScroller, stubResizeObserver } from '#support/platform.js';

const options = { links: '.culture-switcher a', sections: '.demo-section', current: ':scope > a.active' };

describe('ReadingPlace', () => {
    /** @type {HTMLElement} */
    let list;
    /** @type {import('vitest').MockInstance} */
    let scrollTo;
    /** @type {(event: Event) => void} */
    let stayOnPage;
    /** @type {{ dispose(): void }[]} */
    let places;
    /** @type {ReturnType<typeof stubResizeObserver>} */
    let resizes;

    // Every place attached in a test, so none outlives it with a listener on the document.
    const attach = (target, settings) => {
        const place = attachPlace(target, settings);
        places.push(place);
        return place;
    };

    beforeEach(() => {
        places = [];
        resizes = stubResizeObserver();
        sessionStorage.clear();
        history.replaceState(null, '', '/number-input');
        document.body.innerHTML = `
            <nav id="list">
                <div class="culture-switcher"><a href="/he/number-input">he</a><a class="active" href="/number-input">en</a></div>
                <a href="/button">Button</a>
                <a class="active" href="/number-input">Number Input</a>
            </nav>
            <main><section class="demo-section"></section><section class="demo-section"></section></main>`;
        list = document.querySelector('#list');
        sizeScroller(list, { clientHeight: 300, scrollTop: 0 });
        list.getBoundingClientRect = () => ({ top: 0, bottom: 300, height: 300 });
        placeEntry({ top: 100, bottom: 140, height: 40 });
        scrollTo = vi.spyOn(window, 'scrollTo').mockImplementation(() => {});
        setScrollY(0);

        // A language link would leave the page; the test stays on it.
        stayOnPage = event => event.preventDefault();
        document.addEventListener('click', stayOnPage);
    });

    afterEach(() => {
        places.forEach(place => place.dispose());
        delete globalThis.Blazor;
        document.removeEventListener('click', stayOnPage);
        scrollTo.mockRestore();
        resizes.restore();
    });

    it('scrolls the list to center its current entry when the entry is out of view', () => {
        placeEntry({ top: 900, bottom: 940, height: 40 });

        attach(list, options);

        expect(list.scrollTop).toBe(900 - ((300 - 40) / 2));
        expect(scrollTo).not.toHaveBeenCalled();
    });

    it('leaves the list where it is when its current entry is in view', () => {
        attach(list, options);

        expect(list.scrollTop).toBe(0);
    });

    it('scrolls the list to its current entry again when asked, as after a navigation that kept the list', () => {
        const place = attach(list, options);
        placeEntry({ top: 1500, bottom: 1540, height: 40 });

        place.reveal();

        expect(list.scrollTop).toBe(1500 - ((300 - 40) / 2));
    });

    it('scrolls the list to its current entry once the list is shown after being hidden, as a drawer opens', () => {
        sizeScroller(list, { clientHeight: 0, scrollTop: 0 });
        list.getBoundingClientRect = () => ({ top: 0, bottom: 0, height: 0 });
        placeEntry({ top: 0, bottom: 0, height: 0 });
        attach(list, options);

        sizeScroller(list, { clientHeight: 300, scrollTop: 0 });
        list.getBoundingClientRect = () => ({ top: 0, bottom: 300, height: 300 });
        placeEntry({ top: 900, bottom: 940, height: 40 });
        resizes.latest().deliver([]);

        expect(list.scrollTop).toBe(900 - ((300 - 40) / 2));
    });

    it('leaves the list where the reader scrolled it while it stays shown', () => {
        attach(list, options);

        list.scrollTop = 500;
        placeEntry({ top: -400, bottom: -360, height: 40 });
        resizes.latest().deliver([]);

        expect(list.scrollTop).toBe(500);
    });

    it('returns the page a language link leads to to the section the reader left, at the same depth into it', () => {
        setScrollY(1000);
        placeSections([{ top: -600, bottom: -100 }, { top: -100, bottom: 400 }]);
        const place = attach(list, options);

        clickLink('he');
        place.dispose();
        arriveAt('/he/number-input');
        placeSections([{ top: 200, bottom: 1300 }, { top: 1300, bottom: 1900 }]);
        placeEntry({ top: 900, bottom: 940, height: 40 });
        attach(list, options);

        expect(scrollTo).toHaveBeenCalledWith({ top: 1400, behavior: 'instant' });
        expect(list.scrollTop).toBe(900 - ((300 - 40) / 2));
    });

    it('keeps the scroll of the window itself where the window stands above the first section', () => {
        setScrollY(120);
        placeSections([{ top: 180, bottom: 700 }, { top: 700, bottom: 1200 }]);
        const place = attach(list, options);

        clickLink('he');
        place.dispose();
        arriveAt('/he/number-input');
        placeSections([{ top: 330, bottom: 900 }, { top: 900, bottom: 1400 }]);
        attach(list, options);

        expect(scrollTo).toHaveBeenCalledWith({ top: 120, behavior: 'instant' });
    });

    it('returns a page reached through an enhanced navigation to the section the reader left', () => {
        const blazor = stubBlazor();
        setScrollY(1000);
        placeSections([{ top: -600, bottom: -100 }, { top: -100, bottom: 400 }]);
        attach(list, options);

        clickLink('he');
        arriveAt('/he/number-input');
        placeSections([{ top: 200, bottom: 1300 }, { top: 1300, bottom: 1900 }]);
        blazor.dispatch('enhancedload');

        expect(scrollTo).toHaveBeenCalledWith({ top: 1400, behavior: 'instant' });
        expect(sessionStorage.length).toBe(0);
    });

    it('holds the place while the page reached through an enhanced navigation renders again', async () => {
        const blazor = stubBlazor();
        setScrollY(1000);
        placeSections([{ top: -600, bottom: -100 }, { top: -100, bottom: 400 }]);
        attach(list, options);
        clickLink('he');
        arriveAt('/he/number-input');
        placeSections([{ top: 200, bottom: 1300 }, { top: 1300, bottom: 1900 }]);
        blazor.dispatch('enhancedload');

        placeSections([{ top: 248, bottom: 1348 }, { top: 1348, bottom: 1948 }]);
        document.querySelector('main').append(document.createElement('p'));
        await Promise.resolve();

        expect(scrollTo).toHaveBeenLastCalledWith({ top: 1448, behavior: 'instant' });
    });

    it('lets the reader move the window while the page renders again', async () => {
        const blazor = stubBlazor();
        setScrollY(1000);
        placeSections([{ top: -600, bottom: -100 }, { top: -100, bottom: 400 }]);
        attach(list, options);
        clickLink('he');
        arriveAt('/he/number-input');
        placeSections([{ top: 200, bottom: 1300 }, { top: 1300, bottom: 1900 }]);
        blazor.dispatch('enhancedload');

        document.dispatchEvent(new WheelEvent('wheel', { bubbles: true }));
        document.querySelector('main').append(document.createElement('p'));
        await Promise.resolve();

        expect(scrollTo).toHaveBeenCalledTimes(1);
    });

    it('stops returning to a place after an enhanced navigation once disposed', () => {
        const blazor = stubBlazor();
        const place = attach(list, options);

        place.dispose();

        expect(blazor.listeners.get('enhancedload')).toEqual(new Set());
    });

    it('drops a place noted for another page', () => {
        const place = attach(list, options);

        clickLink('he');
        place.dispose();
        expect(sessionStorage.length).toBe(1);
        arriveAt('/button');
        attach(list, options);

        expect(scrollTo).not.toHaveBeenCalled();
        expect(sessionStorage.length).toBe(0);
    });

    it('notes no place for a language link opened in another tab', () => {
        attach(list, options);

        clickLink('he', { ctrlKey: true });

        expect(sessionStorage.length).toBe(0);
    });

    it('notes no place once disposed', () => {
        attach(list, options).dispose();

        clickLink('he');

        expect(sessionStorage.length).toBe(0);
    });

    function setScrollY(value) {
        Object.defineProperty(window, 'scrollY', { value, configurable: true });
    }

    function placeEntry(rect) {
        list.querySelector(':scope > a.active').getBoundingClientRect = () => rect;
    }

    function placeSections(rects) {
        document.querySelectorAll('.demo-section').forEach((section, index) => {
            section.getBoundingClientRect = () => rects[index];
        });
    }

    function clickLink(text, modifiers = {}) {
        const link = [...document.querySelectorAll('.culture-switcher a')].find(anchor => anchor.textContent === text);

        link.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true, button: 0, ...modifiers }));
    }

    // The framework's own event registry, which tells a module that an enhanced navigation landed.
    function stubBlazor() {
        const listeners = new Map();
        globalThis.Blazor = {
            listeners,
            addEventListener: (name, callback) => listeners.set(name, (listeners.get(name) ?? new Set()).add(callback)),
            removeEventListener: (name, callback) => listeners.get(name)?.delete(callback),
            dispatch: name => listeners.get(name)?.forEach(callback => callback()),
        };

        return globalThis.Blazor;
    }

    function arriveAt(path) {
        history.replaceState(null, '', path);
        setScrollY(0);
    }
});

/**
 * ReadingPlace — keeping the reader's place across a change of language
 *
 * A link to the same page in another language reloads the page. Before it does, the place is noted
 * for the page it leads to as the section at the top of the window and how far into it the window
 * stands, so the place survives text that runs longer or shorter in the other language; above the
 * first section, the window's own scroll is the place. The page the link leads to returns to the
 * place, and the target, the page's list, is scrolled to its current entry on every page, whenever
 * the target is shown after being hidden, as a drawer is opened, and whenever its owner asks, as
 * after a navigation that kept the list.
 */

const storageKey = 'forma-showcase-reading-place';

/**
 * @param {HTMLElement} target The list holding the entry for the current page.
 * @param {{ links: string, sections: string, current: string }} options
 *   Selectors for the links that change the language, the sections the place is measured by, and the
 *   target's entry for the current page.
 */
export function attach(target, options) {
    return new ReadingPlace(target, options);
}

class ReadingPlace {
    #target;
    #sections;
    #current;
    #isShown;
    #observer;
    #abort = new AbortController();

    constructor(target, { links, sections, current }) {
        this.#target = target;
        this.#sections = sections;
        this.#current = current;

        const place = takePlace(location.pathname);

        if (place) {
            this.#restore(place);
        }

        this.reveal();

        this.#isShown = target.clientHeight > 0;
        this.#observer = new ResizeObserver(() => this.#handleResize());
        this.#observer.observe(target);

        document.addEventListener('click', event => {
            const link = event.target instanceof Element ? event.target.closest(links) : null;

            // A link opened elsewhere leaves this page, and its place, where they are.
            if (link instanceof HTMLAnchorElement && !event.defaultPrevented && event.button === 0
                && !event.ctrlKey && !event.metaKey && !event.shiftKey && !event.altKey) {
                notePlace({ path: link.pathname, ...this.#measure() });
            }
        }, { capture: true, signal: this.#abort.signal });
    }

    dispose() {
        this.#observer.disconnect();
        this.#abort.abort();
    }

    // Centers the current entry in the target when the target's scroll leaves it out of view.
    reveal() {
        const entry = this.#target.querySelector(this.#current);

        if (!entry) {
            return;
        }

        const bounds = this.#target.getBoundingClientRect();
        const box = entry.getBoundingClientRect();

        if (box.top >= bounds.top && box.bottom <= bounds.bottom) {
            return;
        }

        this.#target.scrollTop += box.top - bounds.top - ((this.#target.clientHeight - box.height) / 2);
    }

    // A target shown after being hidden scrolls to its entry, which it could not while it had no size.
    #handleResize() {
        const isShown = this.#target.clientHeight > 0;

        if (isShown && !this.#isShown) {
            this.reveal();
        }

        this.#isShown = isShown;
    }

    // The first section still showing at the top of the window and how far the window stands below its top, or the
    // window's own scroll where the window stands above the first section or past the last.
    #measure() {
        const sections = [...document.querySelectorAll(this.#sections)];
        const index = sections.findIndex(section => section.getBoundingClientRect().bottom > 0);
        const top = index < 0 ? 0 : sections[index].getBoundingClientRect().top;

        return index < 0 || (index === 0 && top > 0)
            ? { section: -1, offset: window.scrollY }
            : { section: index, offset: -top };
    }

    #restore({ section, offset }) {
        const anchor = section < 0 ? null : document.querySelectorAll(this.#sections)[section];
        const top = anchor ? anchor.getBoundingClientRect().top + window.scrollY + offset : offset;

        window.scrollTo({ top, behavior: 'instant' });
    }
}

// Session storage can be refused, by a private window or a policy, and the place is then simply not kept.
function notePlace(place) {
    try {
        sessionStorage.setItem(storageKey, JSON.stringify(place));
    } catch {
        // Nothing to keep the place in.
    }
}

// The place noted for this page, taken so that it is used once; a place noted for another page is dropped.
function takePlace(path) {
    try {
        const text = sessionStorage.getItem(storageKey);
        sessionStorage.removeItem(storageKey);
        const place = text ? JSON.parse(text) : null;

        return place?.path === path ? place : null;
    } catch {
        return null;
    }
}

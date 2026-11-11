/**
 * ReadingPlace — keeping the reader's place across a change of language
 *
 * A link to the same page in another language leads to that page, in the same document through an
 * enhanced navigation or in a new one. Before it does, the place is noted for the page it leads to
 * as the section at the top of the window and how far into it the window stands, so the place
 * survives text that runs longer or shorter in the other language; above the first section, the
 * window's own scroll is the place. The page the link leads to returns to the place once it has
 * loaded, either way, and after an enhanced navigation holds it while the page's interactive parts
 * render again in the new language, until the reader moves, the page settles or the next navigation
 * begins. The target, the page's list, is scrolled to its current entry
 * on every page, whenever the target is shown after being hidden, as a drawer is opened, and
 * whenever its owner asks, as after a navigation that kept the list.
 */

const storageKey = 'forma-showcase-reading-place';

// How long a page reached through an enhanced navigation holds the place while its interactive parts render again.
const holdDuration = 2000;

/**
 * @param {HTMLElement} target The list holding the entry for the current page.
 * @param {{ links: string, sections: string, currentEntry: string }} options
 *   Selectors for the links that change the language, the sections the place is measured by, and the
 *   target's entry for the current page.
 */
export function attach(target, options) {
    return new ReadingPlace(target, options);
}

class ReadingPlace {
    #target;
    #sections;
    #currentEntry;
    #isShown;
    #observer;
    #abort = new AbortController();
    #holdAbort = null;
    #handleEnhancedLoad = () => {
        this.#holdAbort?.abort();
        this.#returnToPlace({ isHeld: true });
    };

    constructor(target, { links, sections, currentEntry }) {
        this.#target = target;
        this.#sections = sections;
        this.#currentEntry = currentEntry;

        this.#returnToPlace({ isHeld: false });
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

        // A page reached through an enhanced navigation keeps this document, which loads no module again.
        globalThis.Blazor?.addEventListener('enhancedload', this.#handleEnhancedLoad);
    }

    dispose() {
        this.#observer.disconnect();
        this.#abort.abort();
        this.#holdAbort?.abort();
        globalThis.Blazor?.removeEventListener('enhancedload', this.#handleEnhancedLoad);
    }

    // Centers the current entry in the target when the target's scroll leaves it out of view.
    reveal() {
        const entry = this.#target.querySelector(this.#currentEntry);

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

    #returnToPlace({ isHeld }) {
        const place = takePlace(location.pathname);

        if (!place) {
            return;
        }

        this.#restore(place);

        if (isHeld) {
            this.#hold(place);
        }
    }

    // A page reached through an enhanced navigation renders its interactive parts again in the new language after it
    // loads, moving the text above the place, so the place is returned to on each change until the reader moves, the
    // page settles or the next navigation begins. The hold belongs to the address it began at, which a navigation
    // changes before the next page's content arrives.
    #hold(place) {
        this.#holdAbort?.abort();

        const holdAbort = new AbortController();
        const address = location.href;
        const end = () => holdAbort.abort();
        const observer = new MutationObserver(() => location.href === address ? this.#restore(place) : end());
        const timer = setTimeout(end, holdDuration);

        this.#holdAbort = holdAbort;
        observer.observe(document.body, { childList: true, subtree: true, characterData: true });
        holdAbort.signal.addEventListener('abort', () => {
            observer.disconnect();
            clearTimeout(timer);
        });

        for (const name of ['wheel', 'touchstart', 'keydown', 'pointerdown']) {
            document.addEventListener(name, end, { capture: true, signal: holdAbort.signal });
        }

        window.addEventListener('popstate', end, { signal: holdAbort.signal });
        globalThis.navigation?.addEventListener('navigate', end, { signal: holdAbort.signal });
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

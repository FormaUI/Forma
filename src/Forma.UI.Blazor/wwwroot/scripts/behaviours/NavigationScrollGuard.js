/**
 * NavigationScrollGuard — stopping the page scrolling under a list that owns its arrow keys
 *
 * A listbox or menu owns the arrow, Home and End keys, but the browser still scrolls the
 * page with them. Blazor fixes preventDefault at render time and cannot choose per key,
 * so the default is suppressed here for those keys alone, leaving Enter, Space and Tab to
 * act normally. The keys themselves are still handled in C#; this only stops the scroll.
 */

const NAVIGATION_KEYS = new Set(['ArrowUp', 'ArrowDown', 'Home', 'End']);

/** @param {HTMLElement} target */
export function attach(target) {
    return new NavigationScrollGuard(target);
}

class NavigationScrollGuard {
    #controller = new AbortController();

    constructor(target) {
        target.addEventListener('keydown', event => {
            if (NAVIGATION_KEYS.has(event.key)) {
                event.preventDefault();
            }
        }, { signal: this.#controller.signal });
    }

    dispose() {
        this.#controller.abort();
    }
}

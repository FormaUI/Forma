/**
 * FormaDropDownMenu — navigation key guard
 *
 * A menu owns the arrow, Home and End keys, but the browser still scrolls the page with
 * them. Blazor fixes preventDefault at render time and cannot choose per key, so the
 * default is suppressed here for those keys alone, leaving Enter, Space and Tab to act
 * normally. The keys themselves are still handled in C#; this only stops the scroll.
 */

const NAVIGATION_KEYS = new Set(['ArrowUp', 'ArrowDown', 'Home', 'End']);

// Keyed weakly, and the menu element is discarded when the popup closes, so the listener
// it holds goes with it rather than needing a matching detach call.
const guards = new WeakMap();

/** @param {HTMLElement} menu */
export function attachNavigationKeys(menu) {
    guards.get(menu)?.abort();

    const controller = new AbortController();
    guards.set(menu, controller);

    menu.addEventListener('keydown', event => {
        if (NAVIGATION_KEYS.has(event.key)) {
            event.preventDefault();
        }
    }, { signal: controller.signal });
}

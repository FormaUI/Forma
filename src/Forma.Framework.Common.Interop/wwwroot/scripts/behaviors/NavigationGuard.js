/**
 * NavigationGuard — a list that owns its navigation keys keeps them from scrolling the page
 *
 * Blazor fixes preventDefault at render time and cannot choose per key, so the default is
 * suppressed here for the arrow, Home and End keys alone, leaving Enter, Space and Tab to act
 * normally; the keys themselves are still handled in C#.
 */

import { Controls } from '../../../core/scripts/Controls.js';

/**
 * @param {HTMLElement} target The list that owns the navigation keys.
 */
export function attach(target) {
    return new NavigationGuard(target);
}

class NavigationGuard {
    #controller = new AbortController();

    constructor(target) {
        target.addEventListener('keydown', event => {
            if (Controls.NavigationKeys.has(event.key)) {
                event.preventDefault();
            }
        }, { signal: this.#controller.signal });
    }

    dispose() {
        this.#controller.abort();
    }
}

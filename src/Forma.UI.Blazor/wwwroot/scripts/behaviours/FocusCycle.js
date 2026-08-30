/**
 * FocusCycle — Tab kept inside one surface
 *
 * A transient surface such as a menu or a selection bar holds focus for as long as it is
 * open: Tab and Shift+Tab move between its own controls and wrap at either end, so the
 * page behind it is never tabbed into by accident. Escape is the surface's own affair.
 */

/** What Tab moves between. */
const FocusableSelector = 'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

/**
 * @param {HTMLElement} target The surface whose controls Tab cycles through.
 */
export function attach(target) {
    const controller = new AbortController();
    const cycle = new FocusCycle(target, controller.signal);

    return { cycle, dispose: () => controller.abort() };
}

export class FocusCycle {
    #target;

    /**
     * @param {HTMLElement} target The surface whose controls Tab cycles through.
     * @param {AbortSignal} signal Ends the cycling.
     */
    constructor(target, signal) {
        this.#target = target;
        target.addEventListener('keydown', event => this.#handle(event), { signal });
    }

    #handle(event) {
        if (event.key !== 'Tab') {
            return;
        }

        const controls = [...this.#target.querySelectorAll(FocusableSelector)];
        if (controls.length === 0) {
            return;
        }

        event.preventDefault();

        const index = controls.indexOf(document.activeElement);
        const step = event.shiftKey ? -1 : 1;
        const next = index < 0 ? (step > 0 ? 0 : controls.length - 1) : (index + step + controls.length) % controls.length;
        controls[next].focus();
    }
}

/**
 * KeyBindings — dispatching a command when its key combination is pressed
 */

import { tell } from '../core/Owner.js';

/**
 * @param {HTMLElement} target
 * @param {{
 *   owner: import('../core/Owner.js').Owner,
 *   bindings?: { key: string, control: boolean, alt: boolean, shift: boolean, command: string }[]
 * }} options
 */
export function attach(target, options) {
    return new KeyBindings(target, options);
}

class KeyBindings {
    #controller = new AbortController();

    constructor(target, { owner, bindings = [] }) {
        // Keys compare without case; each binding's is lowered once.
        const lowered = bindings.map(binding => ({ ...binding, key: binding.key.toLowerCase() }));

        target.addEventListener('keydown', event => {
            const key = event.key.toLowerCase();
            const match = lowered.find(binding =>
                binding.key === key &&
                binding.control === (event.ctrlKey || event.metaKey) &&
                binding.alt === event.altKey &&
                binding.shift === event.shiftKey);

            if (!match) {
                return;
            }

            // The browser's own undo would otherwise run alongside the caller's and diverge from its history.
            event.preventDefault();
            tell(owner, 'HandleCommandAsync', match.command);
        }, { signal: this.#controller.signal });
    }

    dispose() {
        this.#controller.abort();
    }
}

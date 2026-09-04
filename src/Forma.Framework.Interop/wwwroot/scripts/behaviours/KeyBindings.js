/**
 * KeyBindings — dispatching a command when its key combination is pressed
 */

/**
 * @param {HTMLElement} target
 * @param {{
 *   owner: { invokeMethodAsync: (name: string, ...args: unknown[]) => Promise<unknown> },
 *   bindings?: { key: string, control: boolean, alt: boolean, shift: boolean, command: string }[]
 * }} options
 */
export function attach(target, options) {
    return new KeyBindings(target, options);
}

class KeyBindings {
    #controller = new AbortController();

    constructor(target, { owner, bindings = [] }) {
        target.addEventListener('keydown', event => {
            const match = bindings.find(binding =>
                binding.key.toLowerCase() === event.key.toLowerCase() &&
                binding.control === (event.ctrlKey || event.metaKey) &&
                binding.alt === event.altKey &&
                binding.shift === event.shiftKey);

            if (!match) {
                return;
            }

            // The browser's own undo would otherwise run alongside the caller's and diverge from its history.
            event.preventDefault();
            owner.invokeMethodAsync('HandleCommandAsync', match.command);
        }, { signal: this.#controller.signal });
    }

    dispose() {
        this.#controller.abort();
    }
}

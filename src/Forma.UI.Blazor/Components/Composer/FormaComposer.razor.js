// FormaComposer.razor.js — Browser asset loader and interop bridge for FormaComposer

const loadedAssets = new Set();

export async function loadAsset(kind, url) {
    if (loadedAssets.has(url)) {
        return true;
    }

    try {
        if (kind === 0) {
            await import(url);
        } else if (kind === 1 || kind === 2) {
            if (!document.querySelector(`link[href="${url}"]`)) {
                const link = document.createElement('link');
                link.rel = 'stylesheet';
                link.href = url;
                document.head.appendChild(link);
            }
        }
        loadedAssets.add(url);
        return true;
    } catch {
        return false;
    }
}

export function initComposer(element, dotNetRef, keyBindings) {
    if (!element) {
        return;
    }

    const controller = new AbortController();
    const bindings = keyBindings ?? [];

    element.addEventListener('keydown', event => {
        const match = bindings.find(binding =>
            binding.key.toLowerCase() === event.key.toLowerCase() &&
            binding.control === (event.ctrlKey || event.metaKey) &&
            binding.alt === event.altKey &&
            binding.shift === event.shiftKey);

        if (!match) {
            return;
        }

        // The browser's own undo would otherwise run alongside the composer's and diverge from its history.
        event.preventDefault();
        dotNetRef.invokeMethodAsync('ExecuteCommandAsync', match.command);
    }, { signal: controller.signal });

    element.__formaComposer = { dotNetRef, controller };
}

export function disposeComposer(element) {
    element?.__formaComposer?.controller.abort();

    if (element) {
        delete element.__formaComposer;
    }
}

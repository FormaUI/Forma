// Drag-and-drop hands the dropped files to the native input, so Blazor's own
// change handler processes them exactly like a click-picked selection.

// Keyed weakly, so a drop zone's listeners go with it, and re-attaching replaces them
// rather than doubling them.
const bindings = new WeakMap();

export function attach(dropZone) {
    bindings.get(dropZone)?.abort();

    const controller = new AbortController();
    const { signal } = controller;

    bindings.set(dropZone, controller);

    const input = dropZone.querySelector('input[type="file"]');
    let depth = 0;

    dropZone.addEventListener('dragenter', event => {
        event.preventDefault();
        depth += 1;
        dropZone.classList.add('forma-file-picker-dragging');
    }, { signal });

    dropZone.addEventListener('dragover', event => {
        event.preventDefault();
        event.dataTransfer.dropEffect = 'copy';
    }, { signal });

    dropZone.addEventListener('dragleave', () => {
        depth = Math.max(depth - 1, 0);

        if (depth === 0) {
            dropZone.classList.remove('forma-file-picker-dragging');
        }
    }, { signal });

    dropZone.addEventListener('drop', event => {
        event.preventDefault();
        depth = 0;
        dropZone.classList.remove('forma-file-picker-dragging');

        if (input.disabled || event.dataTransfer.files.length === 0) {
            return;
        }

        input.files = event.dataTransfer.files;
        input.dispatchEvent(new Event('change', { bubbles: true }));
    }, { signal });
}

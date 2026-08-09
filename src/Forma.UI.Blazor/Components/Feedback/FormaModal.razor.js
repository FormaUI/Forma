/**
 * FormaModal — minimal JS interop for the native <dialog> element.
 *
 * Blazor cannot call dialog.showModal() / dialog.close() directly because
 * they are imperative DOM APIs with no HTML-attribute equivalents.
 * This module is the only JS needed for the modal system.
 */

/**
 * Opens the dialog as a modal (with backdrop and focus trap).
 * @param {HTMLDialogElement} dialog
 * @param {boolean} closeOnBackdropClick
 */
export function showModal(dialog, closeOnBackdropClick) {
    if (!dialog || dialog.open) return;

    dialog.showModal();

    if (closeOnBackdropClick) {
        // The backdrop is part of the dialog element itself.
        // Clicks on the backdrop hit the dialog; clicks on content hit child elements.
        dialog._backdropHandler ??= (e) => {
            if (e.target === dialog) {
                dialog.close();
            }
        };
        dialog.addEventListener("click", dialog._backdropHandler);
    }
}

/**
 * Closes the dialog if it is open.
 * @param {HTMLDialogElement} dialog
 */
export function closeModal(dialog) {
    if (!dialog || !dialog.open) return;

    dialog.removeEventListener("click", dialog._backdropHandler);
    dialog.close();
}

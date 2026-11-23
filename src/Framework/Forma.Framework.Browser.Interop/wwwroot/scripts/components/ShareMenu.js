/**
 * ShareMenu — the commands a share menu's sheet draws on a statically rendered page
 *
 * A static page has no renderer to run Copy Link, Print or the device's own share, so the sheet draws each as a button
 * naming its command, disabled. Installed once for the document, this enables each command the browser can run, shows
 * the device's share where the browser offers one, hides a command the browser can't run, and runs the one pressed,
 * closing its sheet first. The buttons are found again after each enhanced update, and a press is heard on the
 * document, so a sheet an update replaced is answered too.
 */

import { write } from '../operations/Clipboard.js';
import { page } from '../operations/Print.js';
import { isAvailable, share } from '../operations/Share.js';

const CommandSelector = 'button[data-forma-share-command]';

let isInstalled = false;

/**
 * Brings the document's static share sheets to life, now and after each enhanced update the Blazor object reports.
 * @param {{ addEventListener?: (name: string, listener: () => void) => void } | null} blazor
 */
export function install(blazor = null) {
    if (isInstalled) {
        return;
    }

    isInstalled = true;
    prepare(document);
    blazor?.addEventListener?.('enhancedload', () => prepare(document));
    document.addEventListener('click', run);
}

/**
 * Enables each command under the root the browser can run and hides each it can't, the device's share among them.
 * @param {ParentNode} root
 */
export function prepare(root) {
    for (const button of root.querySelectorAll(CommandSelector)) {
        const isRunnable = canRun(button.dataset.formaShareCommand);
        button.disabled = !isRunnable;
        button.closest('li')?.toggleAttribute('hidden', !isRunnable);
    }
}

// Whether the browser runs a command: the clipboard only in a secure context, the device's share where it offers one.
function canRun(command) {
    switch (command) {
        case 'copy':
            return window.isSecureContext && typeof navigator.clipboard?.writeText === 'function';
        case 'print':
            return typeof window.print === 'function';
        case 'share':
            return isAvailable();
        default:
            return false;
    }
}

function run(event) {
    const button = event.target instanceof Element ? event.target.closest(CommandSelector) : null;

    if (button === null || button.disabled) {
        return;
    }

    const { formaShareCommand: command, formaShareUrl: url = '', formaShareTitle: title = '' } = button.dataset;
    const sheet = button.closest('[popover]');

    if (sheet?.matches(':popover-open')) {
        sheet.hidePopover();
    }

    switch (command) {
        case 'copy':
            write(url).catch(error => console.error('Forma couldn\'t copy the link.', error));
            break;
        case 'print':
            page();
            break;
        case 'share':
            share(title, url);
            break;
    }
}

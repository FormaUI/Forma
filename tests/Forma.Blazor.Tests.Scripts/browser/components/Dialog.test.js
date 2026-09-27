import { describe, it, expect, beforeEach, vi } from 'vitest';
import { attach } from '#browser/components/Dialog.js';

describe('Dialog', () => {
    /** @type {HTMLDialogElement} */
    let dialog;

    beforeEach(() => {
        document.body.innerHTML = '<dialog><p>Body</p></dialog>';
        dialog = document.querySelector('dialog');
    });

    const clickScrim = () => dialog.dispatchEvent(new MouseEvent('click', { bubbles: true }));

    it('opens the dialog as a modal', () => {
        const handle = attach(dialog);

        handle.open();

        expect(dialog.open).toBe(true);
    });

    it('closes the dialog', () => {
        const handle = attach(dialog);
        handle.open();

        handle.close();

        expect(dialog.open).toBe(false);
    });

    // showModal lands on the first control in the dialog, the header's close button, which is
    // the last thing an author opening a form wants to reach first.
    it('focuses the first control inside the part asked for', () => {
        document.body.innerHTML = '<dialog><header><button id="close">Close</button></header><div class="body"><input id="url"><input id="text"></div></dialog>';
        dialog = document.querySelector('dialog');
        const handle = attach(dialog);

        handle.open({ focusWithin: '.body' });

        expect(document.activeElement.id).toBe('url');
    });

    it('leaves focus where the browser put it when the part has no control', () => {
        document.body.innerHTML = '<dialog><header><button id="close">Close</button></header><div class="body"><p>Body</p></div></dialog>';
        dialog = document.querySelector('dialog');
        const handle = attach(dialog);
        dialog.querySelector('#close').focus();

        handle.open({ focusWithin: '.body' });

        expect(document.activeElement.id).toBe('close');
    });

    it('closes on a scrim click when asked', () => {
        const handle = attach(dialog);
        handle.open({ closeOnScrimClick: true });

        clickScrim();

        expect(dialog.open).toBe(false);
    });

    it('leaves a click on the content alone', () => {
        const handle = attach(dialog);
        handle.open({ closeOnScrimClick: true });

        dialog.querySelector('p').dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(dialog.open).toBe(true);
    });

    it('ignores a scrim click when not asked', () => {
        const handle = attach(dialog);
        handle.open();

        clickScrim();

        expect(dialog.open).toBe(true);
    });

    // A dialog can close by Escape or a method="dialog" form without close() running, which
    // is what let the previous listener survive into the next open and close twice over.
    it('closes once on a scrim click after the dialog closed itself and reopened', () => {
        const handle = attach(dialog);
        const close = vi.spyOn(dialog, 'close');

        handle.open({ closeOnScrimClick: true });
        dialog.close();
        handle.open({ closeOnScrimClick: true });
        close.mockClear();
        clickScrim();

        expect(close).toHaveBeenCalledTimes(1);
    });

    it('stops closing on a scrim click once disposed', () => {
        const handle = attach(dialog);
        handle.open({ closeOnScrimClick: true });

        handle.dispose();
        clickScrim();

        expect(dialog.open).toBe(true);
    });
});

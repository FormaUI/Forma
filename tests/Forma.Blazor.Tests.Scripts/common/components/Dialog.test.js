import { describe, it, expect, beforeEach, vi } from 'vitest';
import { attach } from '#common/components/Dialog.js';

describe('Dialog', () => {
    /** @type {HTMLDialogElement} */
    let dialog;

    beforeEach(() => {
        document.body.innerHTML = '<dialog><p>Body</p></dialog>';
        dialog = document.querySelector('dialog');
    });

    const clickBackdrop = () => dialog.dispatchEvent(new MouseEvent('click', { bubbles: true }));

    it('opens the dialog as a modal', () => {
        const handle = attach(dialog);

        handle.open(false);

        expect(dialog.open).toBe(true);
    });

    it('closes the dialog', () => {
        const handle = attach(dialog);
        handle.open(false);

        handle.close();

        expect(dialog.open).toBe(false);
    });

    it('closes on a backdrop click when asked', () => {
        const handle = attach(dialog);
        handle.open(true);

        clickBackdrop();

        expect(dialog.open).toBe(false);
    });

    it('leaves a click on the content alone', () => {
        const handle = attach(dialog);
        handle.open(true);

        dialog.querySelector('p').dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(dialog.open).toBe(true);
    });

    it('ignores a backdrop click when not asked', () => {
        const handle = attach(dialog);
        handle.open(false);

        clickBackdrop();

        expect(dialog.open).toBe(true);
    });

    // A dialog can close by Escape or a method="dialog" form without close() running, which
    // is what let the previous listener survive into the next open and close twice over.
    it('closes once on a backdrop click after the dialog closed itself and reopened', () => {
        const handle = attach(dialog);
        const close = vi.spyOn(dialog, 'close');

        handle.open(true);
        dialog.close();
        handle.open(true);
        close.mockClear();
        clickBackdrop();

        expect(close).toHaveBeenCalledTimes(1);
    });

    it('stops closing on a backdrop click once disposed', () => {
        const handle = attach(dialog);
        handle.open(true);

        handle.dispose();
        clickBackdrop();

        expect(dialog.open).toBe(true);
    });
});

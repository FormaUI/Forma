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

    // The dialog closing itself, as Escape or a method="dialog" form does, bypasses the handle's close().
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

    describe('handing the focus back', () => {
        const nextFrame = () => new Promise(resolve => requestAnimationFrame(() => resolve()));

        // The dialog closing, its close event delivered, and the focus left on nothing, as the browser leaves it where it can't hand it back.
        const closeOnto = async (handle, focus = () => document.body.focus()) => {
            handle.close();
            dialog.dispatchEvent(new Event('close'));
            focus();
            await nextFrame();
        };

        it('hands the focus back to the element that held it as the dialog opened', async () => {
            document.body.insertAdjacentHTML('afterbegin', '<button id="opener">Rename</button>');
            const opener = document.querySelector('#opener');
            opener.focus();
            const handle = attach(dialog);
            handle.open();

            await closeOnto(handle);

            expect(document.activeElement).toBe(opener);
        });

        it('hands the focus to the nearest ancestor that can take it once a menu holding the opener has gone', async () => {
            document.body.insertAdjacentHTML('afterbegin', '<div id="item" tabindex="-1"><div popover><button id="rename">Rename</button></div></div>');
            document.querySelector('#rename').focus();
            const handle = attach(dialog);
            handle.open();
            document.querySelector('[popover]').remove();

            await closeOnto(handle);

            expect(document.activeElement).toBe(document.querySelector('#item'));
        });

        it('hands the focus back from a control of the closed dialog still holding it, as WebKit leaves it', async () => {
            dialog.innerHTML = '<input id="name" />';
            document.body.insertAdjacentHTML('afterbegin', '<div id="item" tabindex="-1"><div popover><button id="rename">Rename</button></div></div>');
            document.querySelector('#rename').focus();
            const handle = attach(dialog);
            handle.open();
            document.querySelector('[popover]').remove();

            await closeOnto(handle, () => document.querySelector('#name').focus());

            expect(document.activeElement).toBe(document.querySelector('#item'));
        });

        it('leaves the focus in the dialog once it opens again', async () => {
            dialog.innerHTML = '<input id="name" />';
            document.body.insertAdjacentHTML('afterbegin', '<button id="opener">Rename</button>');
            document.querySelector('#opener').focus();
            const handle = attach(dialog);
            handle.open();

            await closeOnto(handle, () => {
                handle.open();
                document.querySelector('#name').focus();
            });

            expect(document.activeElement).toBe(document.querySelector('#name'));
        });

        it('hands the focus back once disposed after its owner took it away while open', async () => {
            document.body.insertAdjacentHTML('afterbegin', '<div id="item" tabindex="-1"><div popover><button id="rename">Rename</button></div></div>');
            document.querySelector('#rename').focus();
            const handle = attach(dialog);
            handle.open();
            document.querySelector('[popover]').remove();
            dialog.remove();

            handle.dispose();
            document.body.focus();
            await nextFrame();

            expect(document.activeElement).toBe(document.querySelector('#item'));
        });

        it('leaves a focus that has gone somewhere where it went', async () => {
            document.body.insertAdjacentHTML('afterbegin', '<button id="opener">Rename</button><input id="elsewhere" />');
            document.querySelector('#opener').focus();
            const handle = attach(dialog);
            handle.open();

            await closeOnto(handle, () => document.querySelector('#elsewhere').focus());

            expect(document.activeElement).toBe(document.querySelector('#elsewhere'));
        });
    });
});

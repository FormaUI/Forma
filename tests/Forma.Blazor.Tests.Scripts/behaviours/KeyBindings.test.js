import { describe, it, expect, beforeEach } from 'vitest';
import { attach } from '#behaviours/KeyBindings.js';
import { fakeOwner } from '#support/owner.js';

describe('KeyBindings', () => {
    /** @type {HTMLElement} */
    let surface;

    const undo = { key: 'z', control: true, alt: false, shift: false, command: 'Undo' };
    const redo = { key: 'z', control: true, alt: false, shift: true, command: 'Redo' };

    beforeEach(() => {
        document.body.innerHTML = '<div id="surface" contenteditable="true"></div>';
        surface = document.querySelector('#surface');
    });

    const press = (key, modifiers = {}) => {
        const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true, ...modifiers });
        surface.dispatchEvent(event);

        return event;
    };

    it('dispatches the command whose combination was pressed', () => {
        const owner = fakeOwner();
        attach(surface, { owner, bindings: [undo, redo] });

        press('z', { ctrlKey: true });

        expect(owner.invokeMethodAsync).toHaveBeenCalledExactlyOnceWith('HandleCommandAsync', 'Undo');
    });

    it('tells two commands on the same key apart by their modifiers', () => {
        const owner = fakeOwner();
        attach(surface, { owner, bindings: [undo, redo] });

        press('z', { ctrlKey: true, shiftKey: true });

        expect(owner.invokeMethodAsync).toHaveBeenCalledExactlyOnceWith('HandleCommandAsync', 'Redo');
    });

    it('accepts the command key in place of control', () => {
        const owner = fakeOwner();
        attach(surface, { owner, bindings: [undo] });

        press('z', { metaKey: true });

        expect(owner.invokeMethodAsync).toHaveBeenCalledExactlyOnceWith('HandleCommandAsync', 'Undo');
    });

    it('matches the key whatever case the browser reports', () => {
        const owner = fakeOwner();
        attach(surface, { owner, bindings: [undo] });

        press('Z', { ctrlKey: true });

        expect(owner.invokeMethodAsync).toHaveBeenCalledTimes(1);
    });

    it('stops the browser running its own version of the command', () => {
        attach(surface, { owner: fakeOwner(), bindings: [undo] });

        expect(press('z', { ctrlKey: true }).defaultPrevented).toBe(true);
    });

    it('ignores a key that no binding claims', () => {
        const owner = fakeOwner();
        attach(surface, { owner, bindings: [undo] });

        const event = press('z');

        expect(owner.invokeMethodAsync).not.toHaveBeenCalled();
        expect(event.defaultPrevented).toBe(false);
    });

    it('ignores a bound key pressed with an extra modifier', () => {
        const owner = fakeOwner();
        attach(surface, { owner, bindings: [undo] });

        press('z', { ctrlKey: true, altKey: true });

        expect(owner.invokeMethodAsync).not.toHaveBeenCalled();
    });

    it('stops dispatching once disposed', () => {
        const owner = fakeOwner();
        const handle = attach(surface, { owner, bindings: [undo] });

        handle.dispose();
        press('z', { ctrlKey: true });

        expect(owner.invokeMethodAsync).not.toHaveBeenCalled();
    });
});

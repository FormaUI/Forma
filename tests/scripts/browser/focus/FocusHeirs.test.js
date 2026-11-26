import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { FocusHeirs } from '#browser/focus/FocusHeirs.js';

describe('FocusHeirs', () => {
    // The registrations made in a test, disposed after it so the next starts with none.
    const registrations = [];
    const register = handOn => {
        const registration = FocusHeirs.register(handOn);
        registrations.push(registration);

        return registration;
    };

    beforeEach(() => {
        document.body.innerHTML = '<div id="list"><div id="item"><button id="delete">Delete</button></div><button id="next">Next</button></div>';
    });

    afterEach(() => registrations.splice(0).forEach(registration => registration.dispose()));

    // A surface that took its item away and hands the focus of anything inside it to the next item.
    const registerList = () => {
        const item = document.querySelector('#item');

        return register(element => {
            if (item.isConnected || !item.contains(element)) {
                return false;
            }

            document.querySelector('#next').focus();
            return true;
        });
    };

    it('hands the focus of an element taken away to the heir its surface names', () => {
        registerList();
        const deleteButton = document.querySelector('#delete');
        document.querySelector('#item').remove();

        const isHandedOn = FocusHeirs.handOn(deleteButton);

        expect(isHandedOn).toBe(true);
        expect(document.activeElement.id).toBe('next');
    });

    it('asks the next surface where one never held the element', () => {
        register(() => false);
        registerList();
        const deleteButton = document.querySelector('#delete');
        document.querySelector('#item').remove();

        const isHandedOn = FocusHeirs.handOn(deleteButton);

        expect(isHandedOn).toBe(true);
        expect(document.activeElement.id).toBe('next');
    });

    it('hands on nothing for an element still on the page', () => {
        registerList();

        const isHandedOn = FocusHeirs.handOn(document.querySelector('#delete'));

        expect(isHandedOn).toBe(false);
        expect(document.activeElement).toBe(document.body);
    });

    it('hands on nothing for an element no surface took away', () => {
        registerList();
        const stray = document.createElement('button');

        const isHandedOn = FocusHeirs.handOn(stray);

        expect(isHandedOn).toBe(false);
    });

    it('hands on nothing for a surface whose registration was disposed', () => {
        registerList().dispose();
        const deleteButton = document.querySelector('#delete');
        document.querySelector('#item').remove();

        const isHandedOn = FocusHeirs.handOn(deleteButton);

        expect(isHandedOn).toBe(false);
        expect(document.activeElement).toBe(document.body);
    });
});

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { RegistrationTable } from '#browser/RegistrationTable.mjs';

describe('RegistrationTable', () => {
    /** @type {RegistrationTable} */
    let registrations;

    beforeEach(() => {
        registrations = new RegistrationTable();
    });

    const resource = () => ({ dispose: vi.fn() });

    it('holds the resource of the generation it began', () => {
        const held = resource();

        registrations.begin('1', 1);
        const isInstalled = registrations.install('1', 1, held);

        expect(isInstalled).toBe(true);
        expect(registrations.get('1')).toBe(held);
    });

    it('releases what an earlier generation holds as a later one begins', () => {
        const earlier = resource();
        registrations.begin('1', 1);
        registrations.install('1', 1, earlier);

        registrations.begin('1', 2);

        expect(earlier.dispose).toHaveBeenCalledOnce();
        expect(registrations.get('1')).toBeNull();
    });

    it('drops an attach a later generation overtook while it loaded', () => {
        const late = resource();
        registrations.begin('1', 1);
        registrations.begin('1', 2);

        const isInstalled = registrations.install('1', 1, late);

        expect(isInstalled).toBe(false);
        expect(late.dispose).toHaveBeenCalledOnce();
    });

    it('refuses to begin a generation behind the latest', () => {
        registrations.begin('1', 2);

        expect(registrations.begin('1', 1)).toBe(false);
    });

    it('drops an attach that a detach overtook while it loaded', () => {
        const late = resource();
        registrations.begin('1', 1);

        registrations.remove('1', 2);

        expect(registrations.install('1', 1, late)).toBe(false);
        expect(late.dispose).toHaveBeenCalledOnce();
    });

    it('releases what the token holds when it is removed', () => {
        const held = resource();
        registrations.begin('1', 1);
        registrations.install('1', 1, held);

        registrations.remove('1', 1);

        expect(held.dispose).toHaveBeenCalledOnce();
        expect(registrations.get('1')).toBeNull();
    });

    it('leaves a later generation alone when an earlier one is removed', () => {
        const held = resource();
        registrations.begin('1', 2);
        registrations.install('1', 2, held);

        registrations.remove('1', 1);

        expect(held.dispose).not.toHaveBeenCalled();
    });

    it('releases every resource once disposed and refuses any later registration', () => {
        const first = resource();
        const second = resource();
        registrations.begin('1', 1);
        registrations.install('1', 1, first);
        registrations.begin('2', 1);
        registrations.install('2', 1, second);

        registrations.dispose();

        expect(first.dispose).toHaveBeenCalledOnce();
        expect(second.dispose).toHaveBeenCalledOnce();
        expect(registrations.begin('3', 1)).toBe(false);
    });

    it('drops an attach that a release overtook while it loaded', () => {
        const late = resource();
        registrations.begin('1', 1);

        registrations.dispose();

        expect(registrations.install('1', 1, late)).toBe(false);
        expect(late.dispose).toHaveBeenCalledOnce();
    });
});

import { describe, it, expect, beforeEach, vi } from 'vitest';

const root = document.documentElement;

// The module keeps the document's state at module level and reads the served pair as it loads, so each test loads it afresh
// over the attributes it was served.
async function load(served = { lang: 'en', dir: 'ltr' }) {
    setAttributes(served);
    vi.resetModules();

    return await import('#globalization/DocumentLanguage.js');
}

async function loadInitializer() {
    vi.resetModules();

    return await import('../../../src/Forma.Framework/Forma.Framework.Globalization.Interop/wwwroot/formaui-net.Framework.Globalization.Interop.lib.module.js');
}

function setAttributes(attributes) {
    for (const name of ['lang', 'dir']) {
        if (attributes[name] === undefined) {
            root.removeAttribute(name);
        } else {
            root.setAttribute(name, attributes[name]);
        }
    }
}

function readAttributes() {
    return { lang: root.getAttribute('lang'), dir: root.getAttribute('dir') };
}

// A stand-in for the Blazor object an initializer receives, raising the events a test names.
function fakeBlazor() {
    const listeners = new Map();

    return {
        addEventListener: (name, callback) => listeners.set(name, [...(listeners.get(name) ?? []), callback]),
        raise: name => (listeners.get(name) ?? []).forEach(callback => callback({})),
        count: name => (listeners.get(name) ?? []).length,
    };
}

// An enhanced navigation whose response patches the document's attributes to `served`.
function navigate(blazor, served) {
    blazor.raise('enhancednavigationstart');
    setAttributes(served);
    blazor.raise('enhancedload');
}

beforeEach(() => {
    setAttributes({ lang: 'en', dir: 'ltr' });
});

describe('DocumentLanguage', () => {
    describe('registering', () => {
        it('sets both attributes to the owner\'s pair', async () => {
            // Arrange
            const { register } = await load();

            // Act
            register('a', 'he', 'rtl');

            // Assert
            expect(readAttributes()).toEqual({ lang: 'he', dir: 'rtl' });
        });

        it('recovers the live registration under its identity and applies the latest pair', async () => {
            // Arrange
            const { register } = await load();
            const first = register('a', 'he', 'rtl');

            // Act
            const second = register('a', 'de', 'ltr');

            // Assert
            expect(second).toBe(first);
            expect(readAttributes()).toEqual({ lang: 'de', dir: 'ltr' });
        });

        it('refuses another identity while the owner is live', async () => {
            // Arrange
            const { register } = await load();
            register('a', 'he', 'rtl');

            // Act & Assert
            expect(() => register('b', 'de', 'ltr')).toThrow('A document follows one culture state.');
            expect(readAttributes()).toEqual({ lang: 'he', dir: 'rtl' });
        });
    });

    describe('updating', () => {
        it('sets the owner\'s new pair and reports that it still owns the document', async () => {
            // Arrange
            const { register } = await load();
            const registration = register('a', 'he', 'rtl');

            // Act
            const isOwner = registration.update('de', 'ltr');

            // Assert
            expect(isOwner).toBe(true);
            expect(readAttributes()).toEqual({ lang: 'de', dir: 'ltr' });
        });

        it('leaves the document alone for a registration that no longer owns it', async () => {
            // Arrange
            const { register, release } = await load();
            const released = register('a', 'he', 'rtl');
            release();
            register('b', 'de', 'ltr');

            // Act
            const isOwner = released.update('fr', 'ltr');
            released.dispose();

            // Assert
            expect(isOwner).toBe(false);
            expect(readAttributes()).toEqual({ lang: 'de', dir: 'ltr' });
        });
    });

    describe('releasing', () => {
        it('restores the pair the document loaded with', async () => {
            // Arrange
            const { register, release } = await load({ lang: 'en', dir: 'ltr' });
            register('a', 'he', 'rtl');

            // Act
            release();

            // Assert
            expect(readAttributes()).toEqual({ lang: 'en', dir: 'ltr' });
        });

        it('removes an attribute the server left out', async () => {
            // Arrange
            const { register, release } = await load({ lang: 'en' });
            register('a', 'he', 'rtl');

            // Act
            release();

            // Assert
            expect(readAttributes()).toEqual({ lang: 'en', dir: null });
        });

        it('releases through the owner\'s own disposal and lets another identity register', async () => {
            // Arrange
            const { register } = await load();
            register('a', 'he', 'rtl').dispose();

            // Act
            register('b', 'de', 'ltr');

            // Assert
            expect(readAttributes()).toEqual({ lang: 'de', dir: 'ltr' });
        });
    });

    describe('tracking the page updates', () => {
        it('sets the owner\'s pair again after a navigation patches it away', async () => {
            // Arrange
            const { register, track } = await load();
            const blazor = fakeBlazor();
            track(blazor);
            register('a', 'he', 'rtl');

            // Act
            navigate(blazor, { lang: 'en', dir: 'ltr' });

            // Assert
            expect(readAttributes()).toEqual({ lang: 'he', dir: 'rtl' });
        });

        it('restores the pair the last navigation served, read before the owner set its own', async () => {
            // Arrange
            const { register, release, track } = await load();
            const blazor = fakeBlazor();
            track(blazor);
            register('a', 'he', 'rtl');
            navigate(blazor, { lang: 'fr', dir: 'ltr' });

            // Act
            release();

            // Assert
            expect(readAttributes()).toEqual({ lang: 'fr', dir: 'ltr' });
        });

        it('keeps the served pair across a streaming update, which patches no attribute', async () => {
            // Arrange
            const { register, release, track } = await load();
            const blazor = fakeBlazor();
            track(blazor);
            register('a', 'he', 'rtl');

            // Act
            blazor.raise('enhancedload');
            release();

            // Assert
            expect(readAttributes()).toEqual({ lang: 'en', dir: 'ltr' });
        });

        it('reads the navigation that superseded a streaming response', async () => {
            // Arrange
            const { register, release, track } = await load();
            const blazor = fakeBlazor();
            track(blazor);
            register('a', 'he', 'rtl');
            navigate(blazor, { lang: 'fr', dir: 'ltr' });
            blazor.raise('enhancedload');

            // Act
            navigate(blazor, { lang: 'ar', dir: 'rtl' });
            blazor.raise('enhancedload');
            release();

            // Assert
            expect(readAttributes()).toEqual({ lang: 'ar', dir: 'rtl' });
        });

        it('keeps reading what the server serves while nothing owns the document', async () => {
            // Arrange
            const { register, release, track } = await load();
            const blazor = fakeBlazor();
            track(blazor);
            register('a', 'he', 'rtl');
            release();
            navigate(blazor, { lang: 'fr', dir: 'ltr' });
            navigate(blazor, { lang: 'de', dir: 'ltr' });
            register('b', 'he', 'rtl');

            // Act
            release();

            // Assert
            expect(readAttributes()).toEqual({ lang: 'de', dir: 'ltr' });
        });

        it('leaves a document nothing owns as the server served it', async () => {
            // Arrange
            const { track } = await load();
            const blazor = fakeBlazor();
            track(blazor);

            // Act
            navigate(blazor, { lang: 'fr' });

            // Assert
            expect(readAttributes()).toEqual({ lang: 'fr', dir: null });
        });

        it('follows the page updates once however often it is asked to', async () => {
            // Arrange
            const { track } = await load();
            const blazor = fakeBlazor();

            // Act
            track(blazor);
            track(blazor);

            // Assert
            expect(blazor.count('enhancedload')).toBe(1);
            expect(blazor.count('enhancednavigationstart')).toBe(1);
        });
    });

    describe('the initializer', () => {
        it('releases the owner when the client closes the circuit', async () => {
            // Arrange
            const { register } = await load({ lang: 'en', dir: 'ltr' });
            const { beforeWebStart } = await import('../../../src/Forma.Framework/Forma.Framework.Globalization.Interop/wwwroot/formaui-net.Framework.Globalization.Interop.lib.module.js');
            const options = {};
            beforeWebStart(options);
            register('a', 'he', 'rtl');

            // Act
            options.circuit.circuitHandlers.forEach(handler => handler.onCircuitClosed?.());

            // Assert
            expect(readAttributes()).toEqual({ lang: 'en', dir: 'ltr' });
        });

        it('adds its handler beside the ones the host configured', async () => {
            // Arrange
            const { beforeWebStart } = await loadInitializer();
            const hostHandler = { onCircuitOpened: () => {} };
            const options = { circuit: { circuitHandlers: [hostHandler] } };

            // Act
            beforeWebStart(options);

            // Assert
            expect(options.circuit.circuitHandlers).toHaveLength(2);
            expect(options.circuit.circuitHandlers[0]).toBe(hostHandler);
        });

        it('follows the page updates once Blazor has started', async () => {
            // Arrange
            const { afterWebStarted } = await loadInitializer();
            const blazor = fakeBlazor();

            // Act
            afterWebStarted(blazor);

            // Assert
            expect(blazor.count('enhancedload')).toBe(1);
        });
    });
});

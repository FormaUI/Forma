import { describe, it, expect, beforeEach, vi } from 'vitest';

const root = document.documentElement;

// The module keeps the document's state at module level and reads the served pair as it loads, so each test loads it afresh
// over the attributes it was served.
async function load(served = { lang: 'en', dir: 'ltr' }) {
    setAttributes(served);
    vi.resetModules();

    return await import('#culture/DocumentCultureSynchronizer.js');
}

async function loadInitializer() {
    vi.resetModules();

    return await import('../../../src/Forma.Framework/Forma.Framework.Culture.Browser.Interop/wwwroot/formaui-net.Framework.Culture.Browser.Interop.lib.module.js');
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

// A stand-in for the circuit's receiver, recording each landing reported to it with the means to answer it.
function fakeReceiver() {
    const reports = [];

    return {
        reports,
        invokeMethodAsync: (method, address) => new Promise((resolve, reject) => reports.push({ method, address, resolve, reject })),
    };
}

// Answers a report and lets the synchronizer's reaction to the answer run, which is queued as the answer settles.
async function reply(report, holdsPair) {
    report.resolve(holdsPair);
    await Promise.resolve();
}

async function refuse(report) {
    report.reject(new Error('The circuit dropped.'));
    await Promise.resolve();
}

beforeEach(() => {
    setAttributes({ lang: 'en', dir: 'ltr' });
});

describe('DocumentCultureSynchronizer', () => {
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

    describe('holding through a navigation', () => {
        it('lets the patch\'s pair stand and keeps the owner\'s pairs until the reply, then sets the latest', async () => {
            // Arrange
            const { register, track } = await load();
            const blazor = fakeBlazor();
            const receiver = fakeReceiver();
            track(blazor);
            const registration = register('a', 'en', 'ltr', receiver);

            // Act
            navigate(blazor, { lang: 'he', dir: 'rtl' });
            const isOwner = registration.update('de', 'ltr');
            const held = readAttributes();
            await reply(receiver.reports[0], true);

            // Assert
            expect(isOwner).toBe(true);
            expect(held).toEqual({ lang: 'he', dir: 'rtl' });
            expect(readAttributes()).toEqual({ lang: 'de', dir: 'ltr' });
        });

        it('reports the address the navigation landed at, once', async () => {
            // Arrange
            const { register, track } = await load();
            const blazor = fakeBlazor();
            const receiver = fakeReceiver();
            track(blazor);
            register('a', 'en', 'ltr', receiver);

            // Act
            navigate(blazor, { lang: 'he', dir: 'rtl' });
            blazor.raise('enhancedload');

            // Assert
            expect(receiver.reports).toHaveLength(1);
            expect(receiver.reports[0].method).toBe('ReportLandingAsync');
            expect(receiver.reports[0].address).toBe(location.href);
        });

        it('keeps the patch\'s pair across a streaming update while held', async () => {
            // Arrange
            const { register, track } = await load();
            const blazor = fakeBlazor();
            track(blazor);
            register('a', 'en', 'ltr', fakeReceiver());
            navigate(blazor, { lang: 'he', dir: 'rtl' });

            // Act
            blazor.raise('enhancedload');

            // Assert
            expect(readAttributes()).toEqual({ lang: 'he', dir: 'rtl' });
        });

        it('stays held when the reply says the browser lacks the pair', async () => {
            // Arrange
            const { register, track } = await load();
            const blazor = fakeBlazor();
            const receiver = fakeReceiver();
            track(blazor);
            const registration = register('a', 'en', 'ltr', receiver);
            navigate(blazor, { lang: 'he', dir: 'rtl' });

            // Act
            await reply(receiver.reports[0], false);
            registration.update('de', 'ltr');

            // Assert
            expect(readAttributes()).toEqual({ lang: 'he', dir: 'rtl' });
        });

        it('ignores the reply for a navigation another replaced', async () => {
            // Arrange — the first navigation landed and reported; a second started before its reply
            const { register, track } = await load();
            const blazor = fakeBlazor();
            const receiver = fakeReceiver();
            track(blazor);
            const registration = register('a', 'en', 'ltr', receiver);
            navigate(blazor, { lang: 'he', dir: 'rtl' });
            blazor.raise('enhancednavigationstart');
            registration.update('fr', 'ltr');

            // Act
            await reply(receiver.reports[0], true);
            const afterReplacedReply = readAttributes();
            setAttributes({ lang: 'ar', dir: 'rtl' });
            blazor.raise('enhancedload');
            await reply(receiver.reports[1], true);

            // Assert
            expect(afterReplacedReply).toEqual({ lang: 'he', dir: 'rtl' });
            expect(readAttributes()).toEqual({ lang: 'fr', dir: 'ltr' });
        });

        it('ends the hold when the navigation ends without landing', async () => {
            // Arrange
            const { register, track } = await load();
            const blazor = fakeBlazor();
            const receiver = fakeReceiver();
            track(blazor);
            const registration = register('a', 'en', 'ltr', receiver);
            blazor.raise('enhancednavigationstart');
            registration.update('de', 'ltr');

            // Act
            blazor.raise('enhancednavigationend');

            // Assert
            expect(receiver.reports).toHaveLength(0);
            expect(readAttributes()).toEqual({ lang: 'de', dir: 'ltr' });
        });

        it('waits for the reply when the navigation ends after landing', async () => {
            // Arrange
            const { register, track } = await load();
            const blazor = fakeBlazor();
            const receiver = fakeReceiver();
            track(blazor);
            const registration = register('a', 'en', 'ltr', receiver);
            navigate(blazor, { lang: 'he', dir: 'rtl' });
            registration.update('de', 'ltr');

            // Act
            blazor.raise('enhancednavigationend');
            const afterEnd = readAttributes();
            await reply(receiver.reports[0], true);

            // Assert
            expect(afterEnd).toEqual({ lang: 'he', dir: 'rtl' });
            expect(readAttributes()).toEqual({ lang: 'de', dir: 'ltr' });
        });

        it('reports the landing again over the next registration after a refused report', async () => {
            // Arrange
            const { register, track } = await load();
            const blazor = fakeBlazor();
            const receiver = fakeReceiver();
            track(blazor);
            register('a', 'en', 'ltr', receiver);
            navigate(blazor, { lang: 'he', dir: 'rtl' });
            await refuse(receiver.reports[0]);

            // Act — the connection comes up again and the owner registers under its identity
            register('a', 'he', 'rtl', receiver);
            const held = readAttributes();
            await reply(receiver.reports[1], true);

            // Assert
            expect(receiver.reports).toHaveLength(2);
            expect(receiver.reports[1].address).toBe(receiver.reports[0].address);
            expect(held).toEqual({ lang: 'he', dir: 'rtl' });
            expect(readAttributes()).toEqual({ lang: 'he', dir: 'rtl' });
        });

        it('drops a released registration\'s pending reply and retry', async () => {
            // Arrange — a report pending as the circuit closes, then a new owner held by a navigation of its own
            const { register, release, track } = await load();
            const blazor = fakeBlazor();
            const released = fakeReceiver();
            const receiver = fakeReceiver();
            track(blazor);
            register('a', 'en', 'ltr', released);
            navigate(blazor, { lang: 'he', dir: 'rtl' });
            release();
            register('b', 'fr', 'ltr', receiver);
            navigate(blazor, { lang: 'ar', dir: 'rtl' });

            // Act
            await reply(released.reports[0], true);

            // Assert
            expect(released.reports).toHaveLength(1);
            expect(readAttributes()).toEqual({ lang: 'ar', dir: 'rtl' });
        });

        it('restores the served pair as the circuit closes during a hold', async () => {
            // Arrange
            const { register, release, track } = await load();
            const blazor = fakeBlazor();
            track(blazor);
            const registration = register('a', 'en', 'ltr', fakeReceiver());
            navigate(blazor, { lang: 'he', dir: 'rtl' });
            registration.update('de', 'ltr');

            // Act
            release();

            // Assert
            expect(readAttributes()).toEqual({ lang: 'he', dir: 'rtl' });
        });

        it('holds nothing for an owner that follows no addresses', async () => {
            // Arrange
            const { register, track } = await load();
            const blazor = fakeBlazor();
            track(blazor);
            const registration = register('a', 'en', 'ltr');

            // Act
            blazor.raise('enhancednavigationstart');
            registration.update('de', 'ltr');

            // Assert
            expect(readAttributes()).toEqual({ lang: 'de', dir: 'ltr' });
        });
    });

    describe('the initializer', () => {
        it('releases the owner when the client closes the circuit', async () => {
            // Arrange
            const { register } = await load({ lang: 'en', dir: 'ltr' });
            const { beforeWebStart } = await import('../../../src/Forma.Framework/Forma.Framework.Culture.Browser.Interop/wwwroot/formaui-net.Framework.Culture.Browser.Interop.lib.module.js');
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

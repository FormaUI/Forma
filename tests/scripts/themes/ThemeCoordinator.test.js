import { describe, it, expect, beforeEach, vi } from 'vitest';

import { linkThemeStylesheet } from '#support/stylesheets.js';

const root = document.documentElement;

// The module keeps the document's coordinator at module level and reads the marker and the stylesheets as it loads, so each
// test loads it afresh over a page the server rendered with the marker, the record and the stylesheets it names.
async function load({ theme = 'forma', scheme = 'system', revision = 0, keepsNothing = false, inStorage = false, themes = { forma: 'light dark', harbor: 'light dark', paper: 'light' }, sheets = {} } = {}) {
    document.cookie = 'forma-theme=; max-age=0; path=/';
    root.setAttribute('data-forma-theme', theme);
    root.setAttribute('data-forma-scheme', scheme);
    document.head.innerHTML = '';

    const marker = document.createElement('meta');
    marker.setAttribute('name', 'forma-theme');
    marker.setAttribute('data-theme', theme);
    marker.setAttribute('data-scheme', scheme);
    marker.setAttribute('data-revision', String(revision));

    if (keepsNothing) {
        marker.setAttribute('data-keeps-nothing', '');
    }

    if (inStorage) {
        marker.setAttribute('data-record', 'storage');
        marker.setAttribute('data-default-theme', 'forma');
        marker.setAttribute('data-default-scheme', 'system');
    }

    document.head.append(marker);

    for (const [name, schemes] of Object.entries(themes)) {
        document.head.append(linkThemeStylesheet(name, schemes, sheets[name]));
    }

    vi.resetModules();
    const coordinator = await import('#themes/ThemeCoordinator.js');
    coordinator.initialize();

    return coordinator;
}

// A stand-in for a theme state's receiver: it records each choice delivered and each store write asked of it, answering a
// delivery at once and a write at once, or holding the write while `holdsWrites` is set, or failing it where `failsWrites`.
function fakeReceiver({ holdsWrites = false, failsWrites = false } = {}) {
    const deliveries = [];
    const writes = [];
    const heldWrites = [];

    return {
        deliveries,
        writes,
        heldWrites,
        invokeMethodAsync(method, theme, scheme, third, number) {
            if (method === 'WriteStoreAsync') {
                writes.push(`${theme}/${scheme} @${third}`);

                if (failsWrites) {
                    return Promise.reject(new Error('The store is down.'));
                }

                return holdsWrites ? new Promise(resolve => heldWrites.push(resolve)) : Promise.resolve();
            }

            deliveries.push(`${theme}/${scheme} #${number}`);

            return Promise.resolve();
        },
    };
}

function fakeBlazor() {
    const listeners = new Map();

    return {
        addEventListener: (name, callback) => listeners.set(name, [...(listeners.get(name) ?? []), callback]),
        raise: name => (listeners.get(name) ?? []).forEach(callback => callback({})),
    };
}

function readDocument() {
    return `${root.getAttribute('data-forma-theme')}/${root.getAttribute('data-forma-scheme')}`;
}

function readRecord() {
    const entry = document.cookie.split('; ').find(cookie => cookie.startsWith('forma-theme='));

    return entry === undefined || entry === 'forma-theme=' ? null : decodeURIComponent(entry.slice('forma-theme='.length));
}

async function settle() {
    for (let i = 0; i < 20; i++) {
        await Promise.resolve();
    }
}

beforeEach(() => {
    document.cookie = 'forma-theme=; max-age=0; path=/';
});

describe('ThemeCoordinator', () => {
    it('delivers the marker\'s choice, number zero, to a registration as it registers', async () => {
        const coordinator = await load({ theme: 'harbor', scheme: 'dark' });
        const receiver = fakeReceiver();

        await coordinator.register('scope', receiver);

        expect(receiver.deliveries).toEqual(['harbor/dark #0']);
    });

    it('accepts a switch for the document, the record and every other registration, once the record reads back', async () => {
        const coordinator = await load();
        const requesting = fakeReceiver();
        const other = fakeReceiver();
        const registration = await coordinator.register('requesting', requesting);
        await coordinator.register('other', other, 'webassembly');

        const outcome = await registration.request('harbor', 'dark', 1);
        await settle();

        expect(outcome).toBe('accepted');
        expect(readDocument()).toBe('harbor/dark');
        expect(readRecord()).toBe('theme=harbor&scheme=dark&revision=0');
        expect(requesting.deliveries).toEqual(['forma/system #0', 'harbor/dark #1']);
        expect(other.deliveries).toEqual(['forma/system #0', 'harbor/dark #1']);
    });

    it('draws a theme with a light scheme alone light, the reader\'s preference kept for the next theme', async () => {
        const coordinator = await load();
        const receiver = fakeReceiver();
        const registration = await coordinator.register('scope', receiver);

        await registration.request('paper', 'dark', 1);

        expect(readDocument()).toBe('paper/light');
        expect(receiver.deliveries.at(-1)).toBe('paper/dark #1');
    });

    it('refuses a switch to a theme whose stylesheet didn\'t load, or one not registered, changing nothing', async () => {
        const coordinator = await load({ sheets: { harbor: 'failed' } });
        const registration = await coordinator.register('scope', fakeReceiver());

        const unloaded = await registration.request('harbor', 'dark', 1);
        const unregistered = await registration.request('dusk', 'dark', 2);

        expect([unloaded, unregistered]).toEqual(['unselectable', 'unselectable']);
        expect(readDocument()).toBe('forma/system');
        expect(readRecord()).toBeNull();
    });

    it('cancels a request made before one already received, changing nothing', async () => {
        const coordinator = await load();
        const registration = await coordinator.register('scope', fakeReceiver());
        await registration.request('harbor', 'light', 2);

        const outcome = await registration.request('paper', 'light', 1);

        expect(outcome).toBe('canceled');
        expect(readDocument()).toBe('harbor/light');
    });

    it('accepts a switch at once on a page that keeps nothing, writing neither the record nor the store', async () => {
        const coordinator = await load({ keepsNothing: true });
        const receiver = fakeReceiver();
        const registration = await coordinator.register('scope', receiver, 'server', true);

        await registration.request('harbor', 'dark', 1);

        expect(readDocument()).toBe('harbor/dark');
        expect(readRecord()).toBeNull();
        expect(receiver.writes).toEqual([]);
    });

    it('writes the store through the requester one revision past the page\'s, then the record with that revision', async () => {
        const coordinator = await load({ revision: 4 });
        const receiver = fakeReceiver();
        const registration = await coordinator.register('scope', receiver, 'server', true);

        await registration.request('harbor', 'dark', 1);

        expect(receiver.writes).toEqual(['harbor/dark @5']);
        expect(readRecord()).toBe('theme=harbor&scheme=dark&revision=5');
    });

    it('writes the store in the order the choices were accepted, a newer choice taking the place of a write not yet started', async () => {
        const coordinator = await load();
        const receiver = fakeReceiver({ holdsWrites: true });
        const registration = await coordinator.register('scope', receiver, 'server', true);
        const outcomes = [];

        const first = registration.request('harbor', 'dark', 1).then(outcome => outcomes.push(`first ${outcome}`));
        await settle();
        const second = registration.request('paper', 'light', 2).then(outcome => outcomes.push(`second ${outcome}`));
        await settle();
        const third = registration.request('forma', 'dark', 3).then(outcome => outcomes.push(`third ${outcome}`));
        await settle();

        // The first write is under way; the second, waiting, was replaced by the third.
        expect(receiver.writes).toEqual(['harbor/dark @1']);
        receiver.heldWrites.shift()();
        await settle();
        expect(receiver.writes).toEqual(['harbor/dark @1', 'forma/dark @2']);
        receiver.heldWrites.shift()();
        await Promise.all([first, second, third]);

        expect(outcomes).toEqual(['first accepted', 'second accepted', 'third accepted']);
        expect(readRecord()).toBe('theme=forma&scheme=dark&revision=2');
    });

    it('writes the store through another runtime with a store where the requester has none', async () => {
        const coordinator = await load();
        const requesting = fakeReceiver();
        const writing = fakeReceiver();
        const registration = await coordinator.register('requesting', requesting, 'webassembly', false);
        await coordinator.register('writing', writing, 'server', true);

        await registration.request('harbor', 'light', 1);

        expect(requesting.writes).toEqual([]);
        expect(writing.writes).toEqual(['harbor/light @1']);
    });

    it('fails the switch with the store\'s failure, the choice staying accepted and the record on the earlier revision', async () => {
        const coordinator = await load({ revision: 2 });
        const registration = await coordinator.register('scope', fakeReceiver({ failsWrites: true }), 'server', true);

        await expect(registration.request('harbor', 'dark', 1)).rejects.toThrow('The store is down.');

        expect(readDocument()).toBe('harbor/dark');
        expect(readRecord()).toBe('theme=harbor&scheme=dark&revision=2');
    });

    it('accepts a landing\'s choice from its response\'s marker as the next switch', async () => {
        const coordinator = await load();
        const blazor = fakeBlazor();
        const receiver = fakeReceiver();
        await coordinator.register('scope', receiver);
        coordinator.track(blazor);

        blazor.raise('enhancednavigationstart');
        document.head.querySelector('meta[name="forma-theme"]').setAttribute('data-theme', 'harbor');
        root.setAttribute('data-forma-theme', 'harbor');
        blazor.raise('enhancedload');
        await settle();

        expect(receiver.deliveries.at(-1)).toBe('harbor/system #1');
        expect(readDocument()).toBe('harbor/system');
    });

    it('sets the accepted choice again after a page update that sets the document\'s attributes', async () => {
        const coordinator = await load();
        const blazor = fakeBlazor();
        const registration = await coordinator.register('scope', fakeReceiver());
        coordinator.track(blazor);
        await registration.request('harbor', 'dark', 1);

        root.setAttribute('data-forma-theme', 'forma');
        blazor.raise('enhancedload');

        expect(readDocument()).toBe('harbor/dark');
    });

    it("keeps a standalone app's record in local storage, the cookie unwritten", async () => {
        const coordinator = await load({ inStorage: true });
        const registration = await coordinator.register('scope', fakeReceiver(), 'webassembly');

        await registration.request('harbor', 'dark', 1);

        expect(localStorage.getItem('forma-theme')).toBe('theme=harbor&scheme=dark&revision=0');
        expect(readRecord()).toBeNull();
        localStorage.clear();
    });

    it("accepts a standalone app's switch for the document's life where the browser refuses storage", async () => {
        const coordinator = await load({ inStorage: true });
        const registration = await coordinator.register('scope', fakeReceiver(), 'webassembly');
        const setItem = vi.spyOn(localStorage, 'setItem').mockImplementation(() => {
            throw new DOMException('The storage is refused.', 'SecurityError');
        });

        const outcome = await registration.request('harbor', 'dark', 1);
        setItem.mockRestore();

        expect(outcome).toBe('accepted');
        expect(readDocument()).toBe('harbor/dark');
    });

    it("describes the themes and the default a standalone app's page declares, and nothing of a page a server rendered", async () => {
        const standalone = await load({ inStorage: true });
        const described = standalone.describePage();
        const served = await load();

        expect(described).toEqual({ themes: ['forma', 'harbor', 'paper'], defaultTheme: 'forma', defaultScheme: 'system' });
        expect(served.describePage()).toBeNull();
    });

    it("counts a theme whose stylesheet the browser refused as unselectable, and one of another origin read without CORS as selectable", async () => {
        const coordinator = await load({ themes: { forma: 'light dark', harbor: 'light dark', dusk: 'light dark' }, sheets: { harbor: 'refused', dusk: 'foreign' } });

        expect([coordinator.isSelectable('forma'), coordinator.isSelectable('harbor'), coordinator.isSelectable('dusk')]).toEqual([true, false, true]);
    });

    it("lists the themes whose stylesheets loaded, in the head's order, and none on a page without a theme marker", async () => {
        const coordinator = await load({ sheets: { harbor: 'failed' } });
        const selectable = coordinator.getSelectableThemes();
        document.head.innerHTML = '';
        vi.resetModules();
        const unmarked = await import('#themes/ThemeCoordinator.js');
        unmarked.initialize();

        expect(selectable).toEqual(['forma', 'paper']);
        expect(unmarked.getSelectableThemes()).toEqual([]);
    });

    it('refuses a registration on a page without a theme marker', async () => {
        document.head.innerHTML = '';
        vi.resetModules();
        const coordinator = await import('#themes/ThemeCoordinator.js');
        coordinator.initialize();

        await expect(coordinator.register('scope', fakeReceiver())).rejects.toThrow('no valid theme marker');
    });
});

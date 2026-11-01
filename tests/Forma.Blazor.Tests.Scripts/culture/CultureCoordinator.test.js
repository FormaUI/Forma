import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import routeCases from './culture-routes.json';

const root = document.documentElement;

// The module keeps the document's coordinator at module level and reads the served pair as it loads, so each test loads it
// afresh over a page the server rendered with the marker and policy it names.
async function load({ culture = 'en-US', uiCulture = culture, lang = 'en-US', dir = 'ltr', policy = null, marker = true, omitted = [] } = {}) {
    setAttributes({ lang, dir });
    setHead({ culture, uiCulture, policy, marker, omitted });
    vi.resetModules();

    const coordinator = await import('#culture/CultureCoordinator.js');
    coordinator.initialize();

    return coordinator;
}

// A standalone app's page: no server published a marker or a policy, and nothing initializes the coordinator as the document
// loads, since only a Web App's script runs the initializer that does.
async function loadStandalone({ lang = 'en', dir = 'ltr' } = {}) {
    setAttributes({ lang, dir });
    document.head.innerHTML = '';
    vi.resetModules();

    return await import('#culture/CultureCoordinator.js');
}

// What a WebAssembly runtime's startup supplies: its pair as a marker, the switch's policy, and the pair's direction.
function startOf({ culture = 'de-DE', uiCulture = 'he', direction = 'rtl', policy = { content: 'state' } } = {}) {
    return { marker: { 'data-culture': culture, 'data-ui-culture': uiCulture }, policy, direction };
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

// The head a response carries: the culture marker FormaPageHead publishes, without the attributes `omitted` names, and,
// where one is named, the switch's policy.
function setHead({ culture, uiCulture, policy = null, marker = true, omitted = [] }) {
    document.head.innerHTML = '';

    if (marker) {
        const meta = document.createElement('meta');
        meta.setAttribute('name', 'forma-culture');

        for (const [name, value] of [['data-culture', culture], ['data-ui-culture', uiCulture]]) {
            if (!omitted.includes(name)) {
                meta.setAttribute(name, value);
            }
        }

        document.head.append(meta);
    }

    if (policy !== null) {
        const meta = document.createElement('meta');
        meta.setAttribute('name', 'forma-culture-policy');

        for (const [name, value] of Object.entries(policy)) {
            meta.setAttribute(name, value);
        }

        document.head.append(meta);
    }
}

function readDocument() {
    return `${root.getAttribute('lang')}/${root.getAttribute('dir')}`;
}

// A stand-in for the Blazor object an initializer receives, raising the events a test names and recording each address
// it is asked to navigate to.
function fakeBlazor() {
    const listeners = new Map();
    const navigations = [];
    const navigationOptions = [];

    return {
        navigations,
        navigationOptions,
        addEventListener: (name, callback) => listeners.set(name, [...(listeners.get(name) ?? []), callback]),
        raise: name => (listeners.get(name) ?? []).forEach(callback => callback({})),
        navigateTo: (address, options) => {
            navigations.push(address);
            navigationOptions.push(options);
        },
    };
}

// The address policy as FormaPageHead publishes the map culture routing gives it.
function toPolicy(map) {
    return {
        content: 'address',
        'data-routes': JSON.stringify(map.routes),
        'data-default': map.defaultToken,
        'data-prefix': map.prefixesDefault ? 'always' : 'except-default',
        'data-switch': map.switchesInPlace ? 'in-place' : 'full-load',
        'data-root': map.root,
        'data-cookie-name': map.cookieName,
        'data-cookie-path': map.cookiePath,
    };
}

// An English route, the default without a token, and a Hebrew one, reached in place at the host's root.
function addressPolicy({ switchesInPlace = true } = {}) {
    return toPolicy({
        routes: [
            { token: 'en', aliases: [], culture: 'en', uiCulture: 'en', direction: 'ltr' },
            { token: 'he', aliases: ['iw'], culture: 'he', uiCulture: 'he', direction: 'rtl' },
        ],
        defaultToken: 'en',
        prefixesDefault: false,
        switchesInPlace,
        root: '/',
        cookieName: 'forma-culture-route',
        cookiePath: '/',
    });
}

function readRouteCookie() {
    return document.cookie.split('; ').find(cookie => cookie.startsWith('forma-culture-route='))?.split('=')[1] ?? null;
}

// An enhanced navigation whose response patches the document to the pair `served` and the marker it carries.
function navigate(blazor, served) {
    blazor.raise('enhancednavigationstart');
    setAttributes({ lang: served.lang, dir: served.dir });
    setHead({ culture: served.culture, uiCulture: served.uiCulture ?? served.culture });
    blazor.raise('enhancedload');
}

// A stand-in for a culture state's receiver, recording each pair delivered to it and answering at once, or holding the
// answer while `isHolding` is set.
function fakeReceiver() {
    const deliveries = [];
    const held = [];

    return {
        deliveries,
        held,
        isHolding: false,
        invokeMethodAsync(method, culture, uiCulture, documentKey, number) {
            deliveries.push({ method, culture, uiCulture, documentKey, number });

            return this.isHolding ? new Promise(resolve => held.push(resolve)) : Promise.resolve();
        },
    };
}

function describeDelivery({ culture, uiCulture, number }) {
    return `${culture}/${uiCulture} #${number}`;
}

// Lets the queued work the coordinator chains on promises run.
async function settle() {
    for (let i = 0; i < 10; i++) {
        await Promise.resolve();
    }
}

beforeEach(() => {
    setAttributes({ lang: 'en-US', dir: 'ltr' });
});

afterEach(() => {
    vi.restoreAllMocks();
    document.cookie.split('; ').filter(Boolean).forEach(cookie => document.cookie = `${cookie.split('=')[0]}=; max-age=0; path=/`);
});

describe('CultureCoordinator', () => {
    describe('initializing', () => {
        // What the marker holds, and what the coordinator reads it as: a missing element or attribute and a malformed name
        // are invalid, a present, empty name is the invariant culture, and the two names are read independently.
        it.each([
            ['no marker element', { marker: false }],
            ['no culture attribute', { omitted: ['data-culture'] }],
            ['no UI culture attribute', { omitted: ['data-ui-culture'] }],
            ['a malformed culture', { culture: 'not a culture', uiCulture: 'he' }],
            ['a malformed UI culture', { culture: 'de-DE', uiCulture: 'not a culture' }],
        ])('fails every registration on a page whose marker has %s', async (_, page) => {
            // Arrange
            vi.spyOn(console, 'error').mockImplementation(() => {});
            const coordinator = await load(page);

            // Act
            const registering = coordinator.register('a', fakeReceiver());

            // Assert
            await expect(registering).rejects.toThrow(/FormaPageHead/);
            expect(console.error).toHaveBeenCalledOnce();
        });

        it.each([
            ['the invariant culture by its empty name', { culture: '', uiCulture: '' }, '/ #0'],
            ['an invariant culture beside a named UI culture', { culture: '', uiCulture: 'he' }, '/he #0'],
            ['a named culture beside an invariant UI culture', { culture: 'de-DE', uiCulture: '' }, 'de-DE/ #0'],
            ['two named cultures', { culture: 'de-DE', uiCulture: 'he' }, 'de-DE/he #0'],
        ])('starts from a marker naming %s', async (_, page, adopted) => {
            // Arrange
            const coordinator = await load(page);
            const receiver = fakeReceiver();

            // Act
            const registration = await coordinator.register('a', receiver);
            await registration.request('fr-FR', 'fr-FR', 'ltr');

            // Assert
            expect(receiver.deliveries.map(describeDelivery)).toEqual([adopted, 'fr-FR/fr-FR #1']);
        });

        it('reads the marker once, as the document loads', async () => {
            // Arrange
            const coordinator = await load({ culture: 'de-DE', uiCulture: 'he' });
            setHead({ culture: 'fr-FR', uiCulture: 'fr-FR' });
            coordinator.initialize();
            const receiver = fakeReceiver();

            // Act
            await coordinator.register('a', receiver);

            // Assert
            expect(receiver.deliveries.map(describeDelivery)).toEqual(['de-DE/he #0']);
        });
    });

    describe('starting a standalone page', () => {
        it('starts from the pair its runtime supplies, publishing it in the head and setting the document to it', async () => {
            // Arrange
            const coordinator = await loadStandalone();
            const receiver = fakeReceiver();

            // Act
            const registration = await coordinator.register('a', receiver, 'webassembly', startOf());
            await registration.request('fr-FR', 'fr-FR', 'ltr');

            // Assert
            expect(receiver.deliveries.map(describeDelivery)).toEqual(['de-DE/he #0', 'fr-FR/fr-FR #1']);
            expect(document.head.querySelector('meta[name="forma-culture"]').getAttribute('data-ui-culture')).toBe('he');
            expect(document.head.querySelector('meta[name="forma-culture-policy"]').getAttribute('content')).toBe('state');
            expect(readDocument()).toBe('fr-FR/ltr');
        });

        it('sets the document to the pair it starts from', async () => {
            // Arrange
            const coordinator = await loadStandalone();

            // Act
            await coordinator.register('a', fakeReceiver(), 'webassembly', startOf());

            // Assert
            expect(readDocument()).toBe('he/rtl');
        });

        it('persists a switch through the cookie policy its runtime supplies', async () => {
            // Arrange
            const coordinator = await loadStandalone();
            const policy = { content: 'cookie', 'data-name': '.AspNetCore.Culture', 'data-path': '/', 'data-same-site': 'lax' };
            const registration = await coordinator.register('a', fakeReceiver(), 'webassembly', startOf({ policy }));

            // Act
            await registration.request('fr-FR', 'fr-FR', 'ltr');

            // Assert
            expect(document.cookie).toContain(`${encodeURIComponent('.AspNetCore.Culture')}=${encodeURIComponent('c=fr-FR|uic=fr-FR')}`);
        });

        it('refuses culture addresses, which need a server to give each culture its base', async () => {
            // Arrange
            vi.spyOn(console, 'error').mockImplementation(() => {});
            const coordinator = await loadStandalone();

            // Act
            const registering = coordinator.register('a', fakeReceiver(), 'webassembly', startOf({ policy: addressPolicy() }));

            // Assert
            await expect(registering).rejects.toThrow(/standalone app/);
            expect(console.error).toHaveBeenCalledOnce();
            expect(document.head.querySelector('meta[name="forma-culture"]')).toBeNull();
        });

        it('starts from the marker a server published, whatever a runtime supplies', async () => {
            // Arrange
            const coordinator = await load({ culture: 'en-US', uiCulture: 'en-US' });
            const receiver = fakeReceiver();

            // Act
            await coordinator.register('a', receiver, 'webassembly', startOf());

            // Assert
            expect(receiver.deliveries.map(describeDelivery)).toEqual(['en-US/en-US #0']);
            expect(document.head.querySelectorAll('meta[name="forma-culture"]')).toHaveLength(1);
            expect(readDocument()).toBe('en-US/ltr');
        });

        it('starts from the marker a page carries, though nothing initialized it as the document loaded', async () => {
            // Arrange
            const coordinator = await loadStandalone();
            setHead({ culture: 'en-US', uiCulture: 'en-US' });
            const receiver = fakeReceiver();

            // Act
            await coordinator.register('a', receiver, 'webassembly', startOf());

            // Assert
            expect(receiver.deliveries.map(describeDelivery)).toEqual(['en-US/en-US #0']);
        });
    });

    describe("a web view's own culture", () => {
        // A web view's page under a culture per web view: its start names the web view's pair and a policy of its own.
        function webViewStart({ culture = 'en', uiCulture = culture, direction = 'ltr', policy = addressPolicy() } = {}) {
            return startOf({ culture, uiCulture, direction, policy: { ...policy, 'data-web-view': '' } });
        }

        beforeEach(() => {
            window.happyDOM.setURL('http://localhost/about#part');
            window.Blazor = fakeBlazor();
        });

        afterEach(() => {
            delete window.Blazor;
        });

        it('starts from the pair its registration supplies, culture addresses included', async () => {
            // Arrange
            const coordinator = await loadStandalone();
            const receiver = fakeReceiver();

            // Act
            await coordinator.register('a', receiver, 'webview', webViewStart({ culture: 'he', direction: 'rtl' }));

            // Assert
            expect(receiver.deliveries.map(describeDelivery)).toEqual(['he/he #0']);
            expect(readDocument()).toBe('he/rtl');
        });

        it("accepts a landing at another culture's address as its own switch, writing no route cookie", async () => {
            // Arrange
            const coordinator = await loadStandalone();
            const receiver = fakeReceiver();
            const registration = await coordinator.register('a', receiver, 'webview', webViewStart());

            // Act
            await registration.reportLanding('he', 1);

            // Assert
            expect(receiver.deliveries.map(describeDelivery)).toEqual(['en/en #0', 'he/he #1']);
            expect(readDocument()).toBe('he/rtl');
            expect(readRouteCookie()).toBeNull();
        });

        it("switches by address through the web view's navigation, completing once its landing is accepted", async () => {
            // Arrange
            const coordinator = await loadStandalone();
            const receiver = fakeReceiver();
            const registration = await coordinator.register('a', receiver, 'webview', webViewStart());

            // Act
            const switching = registration.request('he', 'he', 'rtl');
            await vi.waitFor(() => expect(window.Blazor.navigations).toEqual(['http://localhost/he/about']));
            window.happyDOM.setURL('http://localhost/he/about');
            await registration.reportLanding('he', 1);

            // Assert
            await expect(switching).resolves.toBe('accepted');
            expect(receiver.deliveries.map(describeDelivery)).toEqual(['en/en #0', 'he/he #1']);
        });
    });

    describe("following an application's coordinator", () => {
        // A web view's page: its start names the application's pair and a policy that follows the application's coordinator.
        function followingStart({ culture = 'en', uiCulture = culture, direction = 'ltr', policy = addressPolicy() } = {}) {
            return startOf({ culture, uiCulture, direction, policy: { ...policy, 'data-follows': '', 'data-web-view': '' } });
        }

        beforeEach(() => {
            window.happyDOM.setURL('http://localhost/about#part');
            window.Blazor = fakeBlazor();
        });

        afterEach(() => {
            delete window.Blazor;
        });

        it('starts from the pair its registration supplies, culture addresses included', async () => {
            // Arrange
            const coordinator = await loadStandalone();
            const receiver = fakeReceiver();

            // Act
            await coordinator.register('a', receiver, 'webview', followingStart({ culture: 'he', direction: 'rtl' }));

            // Assert
            expect(receiver.deliveries.map(describeDelivery)).toEqual(['he/he #0']);
            expect(readDocument()).toBe('he/rtl');
        });

        it('follows a pair its application accepted, replacing the address in its route under the mark', async () => {
            // Arrange
            const coordinator = await loadStandalone();
            const receiver = fakeReceiver();
            const registration = await coordinator.register('a', receiver, 'webview', followingStart());

            // Act
            registration.follow('he', 'he', 'rtl');

            // Assert
            expect(readDocument()).toBe('he/rtl');
            expect(window.Blazor.navigations).toEqual(['http://localhost/he/about#part']);
            expect(window.Blazor.navigationOptions).toEqual([{ replaceHistoryEntry: true, historyEntryState: coordinator.FollowMark }]);
            expect(receiver.deliveries.map(describeDelivery)).toEqual(['en/en #0']);
        });

        it('follows a pair under the state alone without navigating', async () => {
            // Arrange
            const coordinator = await loadStandalone();
            const registration = await coordinator.register('a', fakeReceiver(), 'webview', followingStart({ policy: { content: 'state' } }));

            // Act
            registration.follow('he', 'he', 'rtl');

            // Assert
            expect(readDocument()).toBe('he/rtl');
            expect(window.Blazor.navigations).toEqual([]);
        });

        it('requests nothing for the landing its own replacement made', async () => {
            // Arrange
            const coordinator = await loadStandalone();
            const receiver = fakeReceiver();
            const registration = await coordinator.register('a', receiver, 'webview', followingStart());
            registration.follow('he', 'he', 'rtl');
            window.happyDOM.setURL('http://localhost/he/about#part');

            // Act
            await registration.reportLanding('he', 1);

            // Assert
            expect(receiver.deliveries.map(describeDelivery)).toEqual(['en/en #0']);
            expect(window.Blazor.navigations).toHaveLength(1);
        });

        it("takes the application's culture on its first landing, replacing a stale address", async () => {
            // Arrange
            const coordinator = await loadStandalone();
            const receiver = fakeReceiver();
            const registration = await coordinator.register('a', receiver, 'webview', followingStart({ culture: 'he', direction: 'rtl' }));

            // Act
            await registration.reportLanding(null, 1);

            // Assert
            expect(receiver.deliveries.map(describeDelivery)).toEqual(['he/he #0']);
            expect(window.Blazor.navigations).toEqual(['http://localhost/he/about#part']);
            expect(readDocument()).toBe('he/rtl');
        });

        it("takes the application's culture on a traversal, replacing the address it returned to", async () => {
            // Arrange
            const coordinator = await loadStandalone();
            const receiver = fakeReceiver();
            const registration = await coordinator.register('a', receiver, 'webview', followingStart());
            await registration.reportLanding(null, 1);
            registration.follow('he', 'he', 'rtl');
            window.happyDOM.setURL('http://localhost/he/about#part');
            await registration.reportLanding('he', 2);

            // Act
            window.happyDOM.setURL('http://localhost/about#part');
            window.dispatchEvent(new PopStateEvent('popstate'));
            await registration.reportLanding(null, 3);

            // Assert
            expect(receiver.deliveries.map(describeDelivery)).toEqual(['en/en #0']);
            expect(window.Blazor.navigations).toEqual(['http://localhost/he/about#part', 'http://localhost/he/about#part']);
            expect(readDocument()).toBe('he/rtl');
        });

        it("delivers a navigation the page made to another culture's address, writing no cookie", async () => {
            // Arrange
            const coordinator = await loadStandalone();
            const receiver = fakeReceiver();
            const registration = await coordinator.register('a', receiver, 'webview', followingStart());
            await registration.reportLanding(null, 1);

            // Act
            window.happyDOM.setURL('http://localhost/he/about');
            await registration.reportLanding('he', 2);

            // Assert
            expect(receiver.deliveries.map(describeDelivery)).toEqual(['en/en #0', 'he/he #1']);
            expect(window.Blazor.navigations).toEqual([]);
            expect(readDocument()).toBe('he/rtl');
            expect(readRouteCookie()).toBeNull();
        });
    });

    describe('registering', () => {
        it('delivers the accepted pair to the registering state before it completes', async () => {
            // Arrange
            const coordinator = await load({ culture: 'de-DE', uiCulture: 'he' });
            const receiver = fakeReceiver();

            // Act
            await coordinator.register('a', receiver);

            // Assert
            expect(receiver.deliveries).toEqual([{ method: 'ApplyAsync', culture: 'de-DE', uiCulture: 'he', documentKey: expect.any(String), number: 0 }]);
        });

        it('recovers the live registration under the same identity', async () => {
            // Arrange
            const coordinator = await load();
            const first = await coordinator.register('a', fakeReceiver());
            const receiver = fakeReceiver();

            // Act
            const second = await coordinator.register('a', receiver);
            await second.request('he', 'he', 'rtl');

            // Assert
            expect(second).toBe(first);
            expect(receiver.deliveries.map(describeDelivery)).toEqual(['en-US/en-US #0', 'he/he #1']);
        });

        it('adopts the pair the page accepted while it was released', async () => {
            // Arrange
            const coordinator = await load();
            const other = await coordinator.register('b', fakeReceiver());
            await other.request('he', 'he', 'rtl');
            const receiver = fakeReceiver();

            // Act
            await coordinator.register('a', receiver);

            // Assert
            expect(receiver.deliveries.map(describeDelivery)).toEqual(['he/he #1']);
        });
    });

    describe('switching', () => {
        it('accepts each switch as the next number in the document\'s sequence and delivers it to every registration', async () => {
            // Arrange
            const coordinator = await load();
            const requesterReceiver = fakeReceiver();
            const otherReceiver = fakeReceiver();
            const requester = await coordinator.register('a', requesterReceiver);
            await coordinator.register('b', otherReceiver);

            // Act
            await requester.request('de-DE', 'he', 'rtl');
            await requester.request('fr-FR', 'fr-FR', 'ltr');

            // Assert
            expect(requesterReceiver.deliveries.map(describeDelivery)).toEqual(['en-US/en-US #0', 'de-DE/he #1', 'fr-FR/fr-FR #2']);
            expect(otherReceiver.deliveries.map(describeDelivery)).toEqual(['en-US/en-US #0', 'de-DE/he #1', 'fr-FR/fr-FR #2']);
            expect(new Set([...requesterReceiver.deliveries, ...otherReceiver.deliveries].map(delivery => delivery.documentKey)).size).toBe(1);
        });

        it('sets the document to the accepted pair', async () => {
            // Arrange
            const coordinator = await load();
            const registration = await coordinator.register('a', fakeReceiver());

            // Act
            await registration.request('de-DE', 'he', 'rtl');

            // Assert
            expect(readDocument()).toBe('he/rtl');
        });

        it('accepts requests one at a time in the order they reach it', async () => {
            // Arrange — the first requester's state applies its switch slowly
            const coordinator = await load();
            const slowReceiver = fakeReceiver();
            const slow = await coordinator.register('a', slowReceiver);
            const fast = await coordinator.register('b', fakeReceiver());
            slowReceiver.isHolding = true;

            // Act
            const first = slow.request('de-DE', 'de-DE', 'ltr');
            const second = fast.request('he', 'he', 'rtl');
            await settle();
            const isSecondAcceptedFirst = readDocument() === 'he/rtl';
            slowReceiver.held.forEach(resolve => resolve());
            await Promise.all([first, second]);

            // Assert
            expect(isSecondAcceptedFirst).toBe(false);
            expect(readDocument()).toBe('he/rtl');
        });

        it('completes once the requesting state has applied the accepted pair', async () => {
            // Arrange
            const coordinator = await load();
            const receiver = fakeReceiver();
            const registration = await coordinator.register('a', receiver);
            receiver.isHolding = true;
            let isCompleted = false;

            // Act
            const switching = registration.request('he', 'he', 'rtl').then(() => isCompleted = true);
            await settle();
            const isCompletedBeforeApplied = isCompleted;
            receiver.held.forEach(resolve => resolve());
            await switching;

            // Assert
            expect(isCompletedBeforeApplied).toBe(false);
            expect(isCompleted).toBe(true);
        });

        it('rejects a switch from a released registration', async () => {
            // Arrange
            const coordinator = await load();
            const registration = await coordinator.register('a', fakeReceiver());
            registration.dispose();

            // Act
            const switching = registration.request('he', 'he', 'rtl');

            // Assert
            await expect(switching).rejects.toThrow(/no longer registered/);
            expect(readDocument()).toBe('en-US/ltr');
        });

        it('starts a new sequence in a new document', async () => {
            // Arrange
            const firstReceiver = fakeReceiver();
            const first = await load();
            await (await first.register('a', firstReceiver)).request('he', 'he', 'rtl');
            const secondReceiver = fakeReceiver();

            // Act
            const second = await load({ culture: 'he', lang: 'he', dir: 'rtl' });
            await second.register('a', secondReceiver);

            // Assert
            expect(secondReceiver.deliveries.map(describeDelivery)).toEqual(['he/he #0']);
            expect(secondReceiver.deliveries[0].documentKey).not.toBe(firstReceiver.deliveries[0].documentKey);
        });
    });

    describe('persisting', () => {
        const cookiePolicy = { content: 'cookie', 'data-name': '.AspNetCore.Culture', 'data-path': '/', 'data-max-age': '31536000', 'data-same-site': 'lax' };

        it('writes the cookie in request localization\'s serialization before it accepts the switch', async () => {
            // Arrange
            const coordinator = await load({ policy: cookiePolicy });
            const registration = await coordinator.register('a', fakeReceiver());

            // Act
            await registration.request('de-DE', 'he', 'rtl');

            // Assert
            expect(document.cookie).toContain(`${encodeURIComponent('.AspNetCore.Culture')}=${encodeURIComponent('c=de-DE|uic=he')}`);
        });

        it('fails the switch where it can\'t read the cookie back, changing nothing', async () => {
            // Arrange — a browser that refuses the cookie
            const coordinator = await load({ policy: cookiePolicy });
            const receiver = fakeReceiver();
            const registration = await coordinator.register('a', receiver);
            vi.spyOn(document, 'cookie', 'set').mockImplementation(() => {});

            // Act
            const switching = registration.request('he', 'he', 'rtl');

            // Assert
            await expect(switching).rejects.toThrow(/couldn't be read back/);
            expect(readDocument()).toBe('en-US/ltr');
            expect(receiver.deliveries.map(describeDelivery)).toEqual(['en-US/en-US #0']);
        });

        it('writes no cookie under the policy of the state alone', async () => {
            // Arrange
            const coordinator = await load();
            const registration = await coordinator.register('a', fakeReceiver());

            // Act
            await registration.request('he', 'he', 'rtl');

            // Assert
            expect(document.cookie).toBe('');
        });
    });

    describe('landing', () => {
        it('accepts an enhanced navigation\'s landing from its response\'s marker and delivers it to every registration', async () => {
            // Arrange
            const coordinator = await load();
            const blazor = fakeBlazor();
            coordinator.track(blazor);
            const receiver = fakeReceiver();
            await coordinator.register('a', receiver);

            // Act
            navigate(blazor, { lang: 'he', dir: 'rtl', culture: 'de-DE', uiCulture: 'he' });

            // Assert
            expect(receiver.deliveries.map(describeDelivery)).toEqual(['en-US/en-US #0', 'de-DE/he #1']);
            expect(readDocument()).toBe('he/rtl');
        });

        it('accepts no landing at the pair already accepted', async () => {
            // Arrange
            const coordinator = await load();
            const blazor = fakeBlazor();
            coordinator.track(blazor);
            const receiver = fakeReceiver();
            await coordinator.register('a', receiver);

            // Act
            navigate(blazor, { lang: 'en-US', dir: 'ltr', culture: 'en-US' });

            // Assert
            expect(receiver.deliveries.map(describeDelivery)).toEqual(['en-US/en-US #0']);
        });

        it('accepts a landing with no registration live, which a state registering later adopts', async () => {
            // Arrange
            const coordinator = await load();
            const blazor = fakeBlazor();
            coordinator.track(blazor);
            navigate(blazor, { lang: 'he', dir: 'rtl', culture: 'he' });
            const receiver = fakeReceiver();

            // Act
            await coordinator.register('a', receiver);

            // Assert
            expect(receiver.deliveries.map(describeDelivery)).toEqual(['he/he #1']);
        });

        it('changes nothing for a head update that follows no navigation', async () => {
            // Arrange
            const coordinator = await load();
            const blazor = fakeBlazor();
            coordinator.track(blazor);
            const receiver = fakeReceiver();
            await coordinator.register('a', receiver);

            // Act — a streaming update that patches the head's marker
            setHead({ culture: 'he', uiCulture: 'he' });
            blazor.raise('enhancedload');

            // Assert
            expect(receiver.deliveries.map(describeDelivery)).toEqual(['en-US/en-US #0']);
            expect(readDocument()).toBe('en-US/ltr');
        });

        it('sets the accepted pair again after a streaming update undoes it', async () => {
            // Arrange
            const coordinator = await load();
            const blazor = fakeBlazor();
            coordinator.track(blazor);
            const registration = await coordinator.register('a', fakeReceiver());
            await registration.request('he', 'he', 'rtl');

            // Act
            setAttributes({ lang: 'en-US', dir: 'ltr' });
            blazor.raise('enhancedload');

            // Assert
            expect(readDocument()).toBe('he/rtl');
        });

        it('holds a switch requested during a navigation until the navigation lands', async () => {
            // Arrange
            const coordinator = await load();
            const blazor = fakeBlazor();
            coordinator.track(blazor);
            const receiver = fakeReceiver();
            const registration = await coordinator.register('a', receiver);
            blazor.raise('enhancednavigationstart');

            // Act
            const switching = registration.request('fr-FR', 'fr-FR', 'ltr');
            await settle();
            const deliveredDuringNavigation = receiver.deliveries.map(describeDelivery);
            setAttributes({ lang: 'he', dir: 'rtl' });
            setHead({ culture: 'he', uiCulture: 'he' });
            blazor.raise('enhancedload');
            await switching;

            // Assert
            expect(deliveredDuringNavigation).toEqual(['en-US/en-US #0']);
            expect(receiver.deliveries.map(describeDelivery)).toEqual(['en-US/en-US #0', 'he/he #1', 'fr-FR/fr-FR #2']);
            expect(readDocument()).toBe('fr-FR/ltr');
        });

        it('ends the hold of a navigation that ends without landing, leaving the accepted pair', async () => {
            // Arrange
            const coordinator = await load();
            const blazor = fakeBlazor();
            coordinator.track(blazor);
            const registration = await coordinator.register('a', fakeReceiver());
            blazor.raise('enhancednavigationstart');
            const switching = registration.request('he', 'he', 'rtl');

            // Act
            blazor.raise('enhancednavigationend');
            await switching;

            // Assert
            expect(readDocument()).toBe('he/rtl');
        });
    });

    describe('releasing', () => {
        it('keeps the accepted pair once the last registration goes', async () => {
            // Arrange
            const coordinator = await load();
            const registration = await coordinator.register('a', fakeReceiver());
            await registration.request('he', 'he', 'rtl');

            // Act
            registration.dispose();

            // Assert
            expect(readDocument()).toBe('he/rtl');
        });

        it('releases the closed circuit\'s registrations, delivering nothing more to them', async () => {
            // Arrange
            const coordinator = await load();
            const blazor = fakeBlazor();
            coordinator.track(blazor);
            const receiver = fakeReceiver();
            await coordinator.register('a', receiver);

            // Act
            coordinator.releaseCircuit();
            navigate(blazor, { lang: 'he', dir: 'rtl', culture: 'he' });

            // Assert
            expect(receiver.deliveries.map(describeDelivery)).toEqual(['en-US/en-US #0']);
            expect(readDocument()).toBe('he/rtl');
        });
    });

    describe('mapping', () => {
        // The cases the server's router wrote, so the browser builds and reads addresses as the server does.
        it.each(routeCases.configurations.map(configuration => [configuration.map.root, configuration.map.prefixesDefault ? 'always' : 'except-default', configuration]))(
            'builds each destination and resolves each token as the server does, under the root %s with the prefix %s',
            async (_, __, configuration) => {
                // Arrange
                setHead({ culture: 'en', uiCulture: 'en', policy: toPolicy(configuration.map) });
                const coordinator = await import('#culture/CultureCoordinator.js');

                // Act
                const policy = coordinator.readPolicy(document.head);
                const destinations = configuration.destinations.map(({ href, token }) => coordinator.buildDestination(policy, policy.routes.find(route => route.token === token), href));
                const tokens = configuration.tokens.map(({ token }) => coordinator.findRouteByToken(policy, token, false)?.token ?? null);

                // Assert
                expect(destinations).toEqual(configuration.destinations.map(({ destination }) => destination));
                expect(tokens).toEqual(configuration.tokens.map(({ route }) => route));
            });
    });

    describe('addressing', () => {
        beforeEach(() => {
            window.happyDOM.setURL('http://localhost/about');
        });

        it('refuses a pair no route carries before anything changes', async () => {
            // Arrange
            const coordinator = await load({ culture: 'en', lang: 'en', policy: addressPolicy() });
            const blazor = fakeBlazor();
            coordinator.track(blazor);
            const receiver = fakeReceiver();
            const registration = await coordinator.register('a', receiver);

            // Act
            const switching = registration.request('fr', 'fr', 'ltr');

            // Assert
            await expect(switching).rejects.toThrow(/No configured culture route represents the culture fr with the UI culture fr/);
            expect(blazor.navigations).toEqual([]);
            expect(receiver.deliveries.map(describeDelivery)).toEqual(['en/en #0']);
            expect(readDocument()).toBe('en/ltr');
            expect(readRouteCookie()).toBeNull();
        });

        it('switches in place by navigating to the route\'s address, completing once the landing is accepted and applied', async () => {
            // Arrange
            const coordinator = await load({ culture: 'en', lang: 'en', policy: addressPolicy() });
            const blazor = fakeBlazor();
            coordinator.track(blazor);
            const receiver = fakeReceiver();
            const registration = await coordinator.register('a', receiver);

            // Act
            const switching = registration.request('he', 'he', 'rtl');
            await settle();
            await registration.reportLanding('he', 1);

            // Assert
            await expect(switching).resolves.toBe('accepted');
            expect(blazor.navigations).toEqual(['http://localhost/he/about']);
            expect(receiver.deliveries.map(describeDelivery)).toEqual(['en/en #0', 'he/he #1']);
            expect(readDocument()).toBe('he/rtl');
            expect(readRouteCookie()).toBe('he');
        });

        it('completes a switch whose navigation lands by an enhanced navigation, as the pages of islands do', async () => {
            // Arrange
            const coordinator = await load({ culture: 'en', lang: 'en', policy: addressPolicy() });
            const blazor = fakeBlazor();
            coordinator.track(blazor);
            const receiver = fakeReceiver();
            const registration = await coordinator.register('a', receiver);

            // Act
            const switching = registration.request('he', 'he', 'rtl');
            await settle();
            navigate(blazor, { lang: 'he', dir: 'rtl', culture: 'he' });

            // Assert
            await expect(switching).resolves.toBe('accepted');
            expect(receiver.deliveries.map(describeDelivery)).toEqual(['en/en #0', 'he/he #1']);
        });

        it('completes a switch as canceled where its navigation ends without landing, accepting nothing', async () => {
            // Arrange
            const coordinator = await load({ culture: 'en', lang: 'en', policy: addressPolicy() });
            const blazor = fakeBlazor();
            coordinator.track(blazor);
            const receiver = fakeReceiver();
            const registration = await coordinator.register('a', receiver);

            // Act
            const switching = registration.request('he', 'he', 'rtl');
            await settle();
            blazor.raise('enhancednavigationstart');
            blazor.raise('enhancednavigationend');

            // Assert
            await expect(switching).resolves.toBe('canceled');
            expect(receiver.deliveries.map(describeDelivery)).toEqual(['en/en #0']);
            expect(readDocument()).toBe('en/ltr');
        });

        it('completes a switch as canceled where a landing at another pair replaced its navigation', async () => {
            // Arrange
            const coordinator = await load({ culture: 'he', lang: 'he', dir: 'rtl', policy: addressPolicy() });
            const blazor = fakeBlazor();
            coordinator.track(blazor);
            const receiver = fakeReceiver();
            const registration = await coordinator.register('a', receiver);

            // Act
            const switching = registration.request('en', 'en', 'ltr');
            await settle();
            await registration.reportLanding('he', 1);

            // Assert
            await expect(switching).resolves.toBe('canceled');
            expect(receiver.deliveries.map(describeDelivery)).toEqual(['he/he #0']);
        });

        it('loads the route\'s address as a new document where the switch doesn\'t stay in place', async () => {
            // Arrange
            const coordinator = await load({ culture: 'en', lang: 'en', policy: addressPolicy({ switchesInPlace: false }) });
            const blazor = fakeBlazor();
            coordinator.track(blazor);
            const registration = await coordinator.register('a', fakeReceiver());
            const assign = vi.spyOn(window.location, 'assign').mockImplementation(() => {});

            // Act
            registration.request('he', 'he', 'rtl');
            await settle();

            // Assert
            expect(assign).toHaveBeenCalledWith('http://localhost/he/about');
            expect(blazor.navigations).toEqual([]);
        });

        it('accepts an interactive landing at the route its page reports, writing the route cookie and delivering it to every registration', async () => {
            // Arrange
            const coordinator = await load({ culture: 'en', lang: 'en', policy: addressPolicy() });
            const reporter = fakeReceiver();
            const other = fakeReceiver();
            const registration = await coordinator.register('a', reporter);
            await coordinator.register('b', other);

            // Act
            await registration.reportLanding('he', 1);

            // Assert
            expect(reporter.deliveries.map(describeDelivery)).toEqual(['en/en #0', 'he/he #1']);
            expect(other.deliveries.map(describeDelivery)).toEqual(['en/en #0', 'he/he #1']);
            expect(readRouteCookie()).toBe('he');
            expect(readDocument()).toBe('he/rtl');
        });

        it('takes the default route for a landing at a plain address, and accepts nothing at the pair already accepted', async () => {
            // Arrange
            const coordinator = await load({ culture: 'en', lang: 'en', policy: addressPolicy() });
            const receiver = fakeReceiver();
            const registration = await coordinator.register('a', receiver);

            // Act
            await registration.reportLanding(null, 1);

            // Assert
            expect(receiver.deliveries.map(describeDelivery)).toEqual(['en/en #0']);
            expect(readRouteCookie()).toBe('en');
        });

        it('accepts nothing from a report the runtime made before one already received', async () => {
            // Arrange
            const coordinator = await load({ culture: 'en', lang: 'en', policy: addressPolicy() });
            const receiver = fakeReceiver();
            const registration = await coordinator.register('a', receiver);
            await registration.reportLanding('he', 2);

            // Act
            await registration.reportLanding(null, 1);

            // Assert
            expect(receiver.deliveries.map(describeDelivery)).toEqual(['en/en #0', 'he/he #1']);
            expect(readRouteCookie()).toBe('he');
        });

        it('records nothing for a token no route names, or where culture routes don\'t decide the culture', async () => {
            // Arrange
            const routed = await load({ culture: 'en', lang: 'en', policy: addressPolicy() });
            const receiver = fakeReceiver();
            await (await routed.register('a', receiver)).reportLanding('xx', 1);
            const unrouted = await load({ culture: 'en', lang: 'en' });
            const other = fakeReceiver();

            // Act
            await (await unrouted.register('b', other)).reportLanding('he', 1);

            // Assert
            expect(receiver.deliveries.map(describeDelivery)).toEqual(['en/en #0']);
            expect(other.deliveries.map(describeDelivery)).toEqual(['en/en #0']);
            expect(readRouteCookie()).toBeNull();
        });
    });

    describe('initializer', () => {
        it('reads the page and releases the closed circuit\'s registrations', async () => {
            // Arrange
            setAttributes({ lang: 'en-US', dir: 'ltr' });
            setHead({ culture: 'en-US', uiCulture: 'en-US' });
            const initializer = await loadInitializer();
            const options = {};

            // Act
            initializer.beforeWebStart(options);

            // Assert
            expect(options.circuit.circuitHandlers).toHaveLength(1);
            expect(typeof options.circuit.circuitHandlers[0].onCircuitClosed).toBe('function');
        });
    });
});

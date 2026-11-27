import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';

const root = document.documentElement;

// The bootstrap runs once as it loads, reading the host page's stylesheets and its own element, so each test lays out the
// head a standalone app's host page links, sets the record, and loads the script afresh.
async function run({ record = null, themes = { forma: 'light dark', harbor: 'light dark', paper: 'light' }, unloaded = [], defaultTheme = 'harbor', defaultScheme = 'system' } = {}) {
    root.removeAttribute('data-forma-theme');
    root.removeAttribute('data-forma-scheme');
    document.head.innerHTML = '';

    for (const [name, schemes] of Object.entries(themes)) {
        const link = document.createElement('link');
        link.setAttribute('rel', 'stylesheet');
        link.setAttribute('data-forma-theme-stylesheet', name);
        link.setAttribute('data-forma-theme-schemes', schemes);
        Object.defineProperty(link, 'sheet', { value: unloaded.includes(name) ? null : {} });
        document.head.append(link);
    }

    const script = document.createElement('script');
    script.setAttribute('data-default-theme', defaultTheme);
    script.setAttribute('data-default-scheme', defaultScheme);
    Object.defineProperty(document, 'currentScript', { value: script, configurable: true });

    if (record === null) {
        localStorage.removeItem('forma-theme');
    } else {
        localStorage.setItem('forma-theme', record);
    }

    vi.resetModules();
    await import('#themes/ThemeBootstrap.js');
}

function readDocument() {
    return `${root.getAttribute('data-forma-theme')}/${root.getAttribute('data-forma-scheme')}`;
}

function readMarker() {
    const marker = document.head.querySelector('meta[name="forma-theme"]');

    return Object.fromEntries([...marker.attributes].map(attribute => [attribute.name, attribute.value]));
}

afterEach(() => {
    vi.restoreAllMocks();
    localStorage.clear();
});

describe('ThemeBootstrap', () => {
    it('draws the choice a record naming a loaded theme names, and publishes it as the marker', async () => {
        await run({ record: 'theme=forma&scheme=dark&revision=3' });

        expect(readDocument()).toBe('forma/dark');
        expect(readMarker()).toEqual({
            'name': 'forma-theme',
            'data-theme': 'forma',
            'data-scheme': 'dark',
            'data-revision': '3',
            'data-record': 'storage',
            'data-default-theme': 'harbor',
            'data-default-scheme': 'system',
        });
    });

    it('draws the default choice without a record', async () => {
        await run();

        expect(readDocument()).toBe('harbor/system');
        expect(readMarker()['data-revision']).toBe('0');
    });

    it('draws the default theme in the record\'s scheme for a record naming a theme the head doesn\'t link', async () => {
        await run({ record: 'theme=removed&scheme=light&revision=1' });

        expect(readDocument()).toBe('harbor/light');
    });

    it('draws the default theme in the record\'s scheme for a record naming a theme whose stylesheet failed', async () => {
        await run({ record: 'theme=forma&scheme=dark&revision=1', unloaded: ['forma'] });

        expect(readDocument()).toBe('harbor/dark');
    });

    it('draws a theme with a light scheme alone light, the record\'s preference kept in the marker', async () => {
        await run({ record: 'theme=paper&scheme=dark&revision=1' });

        expect(readDocument()).toBe('paper/light');
        expect(readMarker()['data-scheme']).toBe('dark');
    });

    it('draws the default choice for an unreadable record', async () => {
        await run({ record: 'theme=forma&scheme=dusk&revision=1' });

        expect(readDocument()).toBe('harbor/system');
    });

    it('draws the default choice where the browser refuses to open storage, as a private window can', async () => {
        vi.spyOn(localStorage, 'getItem').mockImplementation(() => {
            throw new DOMException('The storage is refused.', 'SecurityError');
        });

        await run({ record: 'theme=forma&scheme=dark&revision=1' });

        expect(readDocument()).toBe('harbor/system');
    });

    it('never writes the record', async () => {
        await run();
        const setItem = vi.spyOn(localStorage, 'setItem');
        await run();

        expect(setItem).not.toHaveBeenCalled();
    });
});

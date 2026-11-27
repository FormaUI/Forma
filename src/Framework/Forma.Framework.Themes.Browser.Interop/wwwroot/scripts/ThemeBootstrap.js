// Draws a standalone WebAssembly app's page in the reader's theme choice before any of its body is parsed: the choice the
// browser's record in local storage names, or the default the script's own element names. A classic script the host page
// loads in its head after its themes' stylesheets, which the browser has loaded or failed by the time it runs. It publishes
// the theme marker the page's theme coordinator starts from, and never writes the record, which the next switch replaces.
(() => {
    'use strict';

    const script = document.currentScript;
    const root = document.documentElement;
    const defaultTheme = script?.getAttribute('data-default-theme') ?? 'forma';
    const defaultScheme = readScheme(script?.getAttribute('data-default-scheme')) ?? 'system';
    const themes = new Map([...document.querySelectorAll('link[data-forma-theme-stylesheet]')].map(link => [
        link.getAttribute('data-forma-theme-stylesheet'),
        { schemes: (link.getAttribute('data-forma-theme-schemes') ?? 'light dark').split(' '), isLoaded: link.sheet !== null },
    ]));

    // A record naming a loaded theme is applied; one naming a theme the head doesn't link, or one whose stylesheet failed,
    // applies the default theme in the record's scheme preference; an empty or unreadable record, or storage the browser
    // refuses to open, applies the default choice.
    const record = readRecord();
    const choice = record === null
        ? { theme: defaultTheme, scheme: defaultScheme, revision: 0 }
        : { ...record, theme: themes.get(record.theme)?.isLoaded ? record.theme : defaultTheme };
    const schemes = themes.get(choice.theme)?.schemes ?? ['light', 'dark'];

    root.setAttribute('data-forma-theme', choice.theme);
    root.setAttribute('data-forma-scheme', schemes.includes('dark') ? choice.scheme : 'light');

    const marker = document.createElement('meta');
    marker.setAttribute('name', 'forma-theme');
    marker.setAttribute('data-theme', choice.theme);
    marker.setAttribute('data-scheme', choice.scheme);
    marker.setAttribute('data-revision', String(choice.revision));
    marker.setAttribute('data-record', 'storage');
    marker.setAttribute('data-default-theme', defaultTheme);
    marker.setAttribute('data-default-scheme', defaultScheme);
    document.head.append(marker);

    function readRecord() {
        let value;

        try {
            value = localStorage.getItem('forma-theme');
        } catch {
            return null;
        }

        if (!value) {
            return null;
        }

        const fields = new URLSearchParams(value);
        const theme = fields.get('theme');
        const scheme = readScheme(fields.get('scheme'));
        const revision = Number(fields.get('revision'));

        return theme && scheme !== null && Number.isSafeInteger(revision) && revision >= 0 ? { theme, scheme, revision } : null;
    }

    function readScheme(value) {
        return ['system', 'light', 'dark'].includes(value) ? value : null;
    }
})();

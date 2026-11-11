/**
 * Asset — scripts, modules, stylesheets and sprites loaded once per document
 */

const SpriteTimeout = 15_000;
const SpriteHostId = 'forma-sprite-definitions';
const SvgNamespace = 'http://www.w3.org/2000/svg';

const loads = new Map();
const sheets = new Map();

// The symbol ids the current host holds; a host swept away with the body starts it afresh.
let adoptedSymbolIds = new Set();

function getOrStart(map, key, start) {
    const existingLoad = map.get(key);

    if (existingLoad) {
        return existingLoad;
    }

    const pendingLoad = start().catch(error => {
        map.delete(key);
        throw error;
    });

    map.set(key, pendingLoad);

    return pendingLoad;
}

async function attempt(start) {
    try {
        await start();
        return true;
    } catch {
        return false;
    }
}

function loadElement(tag, url, configure) {
    return new Promise((resolve, reject) => {
        const element = document.createElement(tag);

        configure(element);

        element.addEventListener('load', resolve, { once: true });
        element.addEventListener('error', () => {
            element.remove();
            reject(new Error(url));
        }, { once: true });

        document.head.appendChild(element);
    });
}

function loadOnce(kind, url, start) {
    return attempt(() => getOrStart(loads, `${kind}:${url}`, start));
}

export function loadModule(url) {
    return loadOnce('module', url, () => import(url));
}

export function loadScript(url) {
    return loadOnce('script', url, () => loadElement('script', url, script => {
        script.src = url;
        script.async = true;
    }));
}

export function loadStyle(url) {
    return loadOnce('style', url, () => loadElement('link', url, link => {
        link.rel = 'stylesheet';
        link.href = url;
    }));
}

function getSpriteHost() {
    const existingHost = document.getElementById(SpriteHostId);

    if (existingHost?.isConnected) {
        return existingHost;
    }

    const host = document.createElementNS(SvgNamespace, 'svg');
    host.id = SpriteHostId;
    host.setAttribute('aria-hidden', 'true');
    host.setAttribute('focusable', 'false');
    host.style.cssText = 'position:absolute;inline-size:0;block-size:0;overflow:hidden';

    document.body.appendChild(host);
    adoptedSymbolIds = new Set();

    return host;
}

function fetchSheet(url, timeoutMs) {
    return getOrStart(sheets, url, async () => {
        const response = await fetch(url, { signal: AbortSignal.timeout(timeoutMs) });

        if (!response.ok) {
            throw new Error(`Could not load SVG sprite: ${url}`);
        }

        const parsedDocument = new DOMParser().parseFromString(await response.text(), 'image/svg+xml');

        if (parsedDocument.querySelector('parsererror')) {
            throw new Error(`Could not parse SVG sprite: ${url}`);
        }

        return [...parsedDocument.querySelectorAll('symbol')];
    });
}

/**
 * Adopts a sprite's symbols into the current document.
 *
 * The sheet is fetched and parsed once; its symbols are adopted into whichever sprite host
 * the document holds now.
 */
export function loadSprite(url, timeoutMs = SpriteTimeout) {
    return attempt(async () => {
        const symbols = await fetchSheet(url, timeoutMs);
        const host = getSpriteHost();

        for (const symbol of symbols) {
            if (!adoptedSymbolIds.has(symbol.id)) {
                host.appendChild(document.importNode(symbol, true));
                adoptedSymbolIds.add(symbol.id);
            }
        }
    });
}

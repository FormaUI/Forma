/**
 * Loads document-wide assets once, sharing concurrent requests and allowing retries after failure.
 */

const SPRITE_TIMEOUT_MS = 15_000;
const SPRITE_HOST_ID = 'forma-sprite-definitions';
const SVG_NAMESPACE = 'http://www.w3.org/2000/svg';

const loads = new Map();
const sheets = new Map();

// The symbol ids the current host holds; a host swept away with the body starts it afresh.
let adopted = new Set();

function cached(map, key, start) {
    const existing = map.get(key);

    if (existing) {
        return existing;
    }

    const pending = start().catch(error => {
        map.delete(key);
        throw error;
    });

    map.set(key, pending);

    return pending;
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
    return attempt(() => cached(loads, `${kind}:${url}`, start));
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
    const existing = document.getElementById(SPRITE_HOST_ID);

    if (existing?.isConnected) {
        return existing;
    }

    const host = document.createElementNS(SVG_NAMESPACE, 'svg');
    host.id = SPRITE_HOST_ID;
    host.setAttribute('aria-hidden', 'true');
    host.setAttribute('focusable', 'false');
    host.style.cssText = 'position:absolute;inline-size:0;block-size:0;overflow:hidden';

    document.body.appendChild(host);
    adopted = new Set();

    return host;
}

function fetchSheet(url, timeoutMs) {
    return cached(sheets, url, async () => {
        const response = await fetch(url, { signal: AbortSignal.timeout(timeoutMs) });

        if (!response.ok) {
            throw new Error(`Could not load SVG sprite: ${url}`);
        }

        const parsed = new DOMParser().parseFromString(await response.text(), 'image/svg+xml');

        if (parsed.querySelector('parsererror')) {
            throw new Error(`Could not parse SVG sprite: ${url}`);
        }

        return [...parsed.querySelectorAll('symbol')];
    });
}

/**
 * Adopts a sprite's symbols into the current document.
 *
 * The sheet is fetched and parsed once, while adoption is repeated whenever necessary
 * because navigation may replace the document-owned sprite host.
 */
export function loadSprite(url, timeoutMs = SPRITE_TIMEOUT_MS) {
    return attempt(async () => {
        const symbols = await fetchSheet(url, timeoutMs);
        const host = getSpriteHost();

        for (const symbol of symbols) {
            if (!adopted.has(symbol.id)) {
                host.appendChild(document.importNode(symbol, true));
                adopted.add(symbol.id);
            }
        }
    });
}

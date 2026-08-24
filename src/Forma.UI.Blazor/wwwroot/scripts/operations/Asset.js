/**
 * Asset — loading a script, module, or stylesheet once per page
 *
 * The promise is cached rather than the completion, so callers racing the first load
 * share it instead of each starting one. A failed load is forgotten so a later attempt
 * can retry, and nothing rejects: an asset that will not load reports false.
 */

const loads = new Map();

function once(kind, url, start) {
    const key = `${kind}:${url}`;

    if (loads.has(key)) {
        return loads.get(key);
    }

    const pending = start().then(() => true, () => {
        loads.delete(key);
        return false;
    });

    loads.set(key, pending);

    return pending;
}

export function loadModule(url) {
    return once('module', url, () => import(url));
}

export function loadScript(url) {
    return once('script', url, () => new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.src = url;
        script.async = true;
        script.addEventListener('load', resolve, { once: true });
        script.addEventListener('error', () => reject(new Error(url)), { once: true });
        document.head.appendChild(script);
    }));
}

export function loadStyle(url) {
    return once('style', url, () => new Promise((resolve, reject) => {
        const link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = url;
        link.addEventListener('load', resolve, { once: true });
        link.addEventListener('error', () => reject(new Error(url)), { once: true });
        document.head.appendChild(link);
    }));
}

const SPRITE_TIMEOUT_MS = 15000;
const SPRITE_HOST_ID = 'forma-sprite-definitions';
const SVG_NAMESPACE = 'http://www.w3.org/2000/svg';

function spriteHost() {
    const existing = document.getElementById(SPRITE_HOST_ID);

    if (existing) {
        return existing;
    }

    const host = document.createElementNS(SVG_NAMESPACE, 'svg');
    host.setAttribute('id', SPRITE_HOST_ID);
    host.setAttribute('aria-hidden', 'true');
    host.setAttribute('focusable', 'false');
    host.style.cssText = 'position:absolute;inline-size:0;block-size:0;overflow:hidden';
    document.body.appendChild(host);

    return host;
}

/**
 * Adopts an SVG sprite's symbols into the document so a local `<use href="#id">` resolves without
 * another request. A cross-document `<use>` fetches and parses the sprite itself, which lands after
 * the render that referenced it — this makes the symbols present before anything points at them.
 */
export function loadSprite(url, timeoutMs = SPRITE_TIMEOUT_MS) {
    return once('sprite', url, async () => {
        const response = await fetch(url, { signal: AbortSignal.timeout(timeoutMs) });

        if (!response.ok) {
            throw new Error(url);
        }

        const parsed = new DOMParser().parseFromString(await response.text(), 'image/svg+xml');

        if (parsed.querySelector('parsererror')) {
            throw new Error(url);
        }

        const host = spriteHost();

        for (const symbol of parsed.querySelectorAll('symbol')) {
            if (!document.getElementById(symbol.id)) {
                host.appendChild(document.importNode(symbol, true));
            }
        }
    });
}

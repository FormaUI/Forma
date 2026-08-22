/**
 * Assets — loading a script, module, or stylesheet once per page
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

/**
 * Replacements for the platform pieces a headless DOM either omits or reports as zero,
 * each handing back a control the test drives instead of waiting on the browser.
 */

/** Replaces the animation frame queue with one the test flushes by hand. */
export function stubAnimationFrames() {
    const original = { request: globalThis.requestAnimationFrame, cancel: globalThis.cancelAnimationFrame };
    const queued = new Map();
    let nextId = 1;

    globalThis.requestAnimationFrame = callback => {
        const id = nextId++;
        queued.set(id, callback);

        return id;
    };

    globalThis.cancelAnimationFrame = id => queued.delete(id);

    return {
        get pending() {
            return queued.size;
        },
        flush() {
            const callbacks = [...queued.values()];
            queued.clear();

            for (const callback of callbacks) {
                callback(0);
            }
        },
        restore() {
            globalThis.requestAnimationFrame = original.request;
            globalThis.cancelAnimationFrame = original.cancel;
        },
    };
}

function stubObserver(name) {
    const original = globalThis[name];
    const instances = [];

    globalThis[name] = class {
        constructor(callback, options) {
            this.callback = callback;
            this.options = options;
            this.targets = [];
            this.isConnected = true;
            instances.push(this);
        }

        observe(target) {
            this.targets.push(target);
        }

        unobserve(target) {
            this.targets = this.targets.filter(candidate => candidate !== target);
        }

        disconnect() {
            this.targets = [];
            this.isConnected = false;
        }

        /** Delivers observations exactly as the browser would, connected or not. */
        deliver(observations) {
            this.callback(observations, this);
        }
    };

    return {
        instances,
        latest: () => instances.at(-1),
        restore() {
            globalThis[name] = original;
        },
    };
}

export const stubIntersectionObserver = () => stubObserver('IntersectionObserver');
export const stubResizeObserver = () => stubObserver('ResizeObserver');
export const stubMutationObserver = () => stubObserver('MutationObserver');

/** Gives an element the scroll geometry a headless DOM reports as zero. */
export function sizeScroller(element, metrics) {
    for (const [name, value] of Object.entries(metrics)) {
        Object.defineProperty(element, name, { value, writable: true, configurable: true });
    }
}

/** Lays elements out as a column of equal rows, well clear of any scrolling edge. */
export function stackRows(elements, { height = 40, top = 200 } = {}) {
    elements.forEach((element, index) => {
        const rowTop = top + (index * height);
        const rect = { top: rowTop, bottom: rowTop + height, height, left: 0, right: 200, width: 200, x: 0, y: rowTop };

        element.getBoundingClientRect = () => rect;
    });
}

/** Gives an element pointer capture, which a headless DOM leaves unimplemented. */
export function stubPointerCapture(element) {
    const captured = new Set();

    element.setPointerCapture = pointerId => captured.add(pointerId);
    element.releasePointerCapture = pointerId => captured.delete(pointerId);
    element.hasPointerCapture = pointerId => captured.has(pointerId);

    return captured;
}

/**
 * A pointer event carrying the fields a gesture reads. Built on a plain Event because a
 * MouseEvent exposes its coordinates as prototype getters that cannot be assigned.
 */
export function pointerEvent(type, { pointerId = 1, pointerType = 'mouse', button = 0, clientX = 0, clientY = 0 } = {}) {
    const event = new Event(type, { bubbles: true, cancelable: true });

    return Object.assign(event, { pointerId, pointerType, button, clientX, clientY });
}

/** Keeps appended assets out of the document so only the test settles them. */
export function stubAssetLoading() {
    const appended = [];

    document.head.appendChild = element => {
        appended.push(element);

        return element;
    };

    return {
        appended,
        of: tag => appended.filter(element => element.tagName.toLowerCase() === tag),
        restore() {
            delete document.head.appendChild;
        },
    };
}

/** Replaces the object URL factory with one that records what it hands out and takes back. */
export function stubObjectUrls() {
    const original = { create: URL.createObjectURL, revoke: URL.revokeObjectURL };
    const created = [];
    const revoked = [];
    let nextId = 1;

    URL.createObjectURL = () => {
        const url = `blob:forma/${nextId++}`;
        created.push(url);

        return url;
    };

    URL.revokeObjectURL = url => revoked.push(url);

    return {
        created,
        revoked,
        get outstanding() {
            return created.filter(url => !revoked.includes(url));
        },
        restore() {
            URL.createObjectURL = original.create;
            URL.revokeObjectURL = original.revoke;
        },
    };
}

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { stubAssetLoading } from '#support/platform.js';

describe('Asset', () => {
    /** @type {typeof import('#common/operations/Asset.js')} */
    let assets;
    /** @type {ReturnType<typeof stubAssetLoading>} */
    let head;

    beforeEach(async () => {
        head = stubAssetLoading();
        // The load cache is module state, so each test gets a fresh module.
        vi.resetModules();
        assets = await import('#common/operations/Asset.js');
    });

    afterEach(() => head.restore());

    const settle = (tag, type, index = 0) => head.of(tag)[index].dispatchEvent(new Event(type));

    describe('loadScript', () => {
        it('appends the script asynchronously', () => {
            assets.loadScript('/lib/highlight.js');

            const [script] = head.of('script');
            expect(script.getAttribute('src')).toBe('/lib/highlight.js');
            expect(script.async).toBe(true);
        });

        it('reports success once the script has loaded', async () => {
            const pending = assets.loadScript('/lib/highlight.js');

            settle('script', 'load');

            await expect(pending).resolves.toBe(true);
        });

        // Nothing rejects; an asset that will not load reports false.
        it('reports failure rather than rejecting', async () => {
            const pending = assets.loadScript('/lib/missing.js');

            settle('script', 'error');

            await expect(pending).resolves.toBe(false);
        });

        it('appends the script once however many callers ask', async () => {
            const first = assets.loadScript('/lib/highlight.js');
            const second = assets.loadScript('/lib/highlight.js');

            settle('script', 'load');
            await Promise.all([first, second]);

            expect(head.of('script')).toHaveLength(1);
        });

        it('shares the first load with a caller that races it', async () => {
            const first = assets.loadScript('/lib/highlight.js');
            const second = assets.loadScript('/lib/highlight.js');

            settle('script', 'load');

            await expect(Promise.all([first, second])).resolves.toEqual([true, true]);
        });

        it('answers a later caller from the cache without loading again', async () => {
            const first = assets.loadScript('/lib/highlight.js');
            settle('script', 'load');
            await first;

            await expect(assets.loadScript('/lib/highlight.js')).resolves.toBe(true);
            expect(head.of('script')).toHaveLength(1);
        });

        it('removes a script that failed to load', async () => {
            const pending = assets.loadScript('/lib/missing.js');
            const [script] = head.of('script');

            settle('script', 'error');
            await pending;

            expect(script.isConnected).toBe(false);
        });

        // A failure is forgotten so a later attempt can retry.
        it('tries again after a failure', async () => {
            const first = assets.loadScript('/lib/flaky.js');
            settle('script', 'error');
            await first;

            const second = assets.loadScript('/lib/flaky.js');
            settle('script', 'load', 1);

            await expect(second).resolves.toBe(true);
        });
    });

    describe('loadStyle', () => {
        it('appends the stylesheet link', () => {
            assets.loadStyle('/lib/highlight.css');

            const [link] = head.of('link');
            expect(link.getAttribute('rel')).toBe('stylesheet');
            expect(link.getAttribute('href')).toBe('/lib/highlight.css');
        });

        it('reports success once the stylesheet has loaded', async () => {
            const pending = assets.loadStyle('/lib/highlight.css');

            settle('link', 'load');

            await expect(pending).resolves.toBe(true);
        });

        it('reports failure rather than rejecting', async () => {
            const pending = assets.loadStyle('/lib/missing.css');

            settle('link', 'error');

            await expect(pending).resolves.toBe(false);
        });

        it('appends the stylesheet once however many callers ask', async () => {
            const first = assets.loadStyle('/lib/highlight.css');
            const second = assets.loadStyle('/lib/highlight.css');

            settle('link', 'load');
            await Promise.all([first, second]);

            expect(head.of('link')).toHaveLength(1);
        });
    });

    // The cache is keyed by kind as well as url, so a script and a stylesheet at the
    // same path do not answer for one another.
    it('keeps a script and a stylesheet at one url apart', async () => {
        const script = assets.loadScript('/lib/asset');
        const style = assets.loadStyle('/lib/asset');

        settle('script', 'error');
        settle('link', 'load');

        await expect(Promise.all([script, style])).resolves.toEqual([false, true]);
    });

    describe('loadModule', () => {
        it('reports failure rather than rejecting', async () => {
            await expect(assets.loadModule('/lib/absent.js')).resolves.toBe(false);
        });
    });

    describe('loadSprite', () => {
        const sprite = '<svg xmlns="http://www.w3.org/2000/svg">'
            + '<symbol id="emoji-1f600" viewBox="0 0 36 36"><circle cx="18" cy="18" r="18" /></symbol>'
            + '<symbol id="emoji-1f603" viewBox="0 0 36 36"><circle cx="18" cy="18" r="18" /></symbol>'
            + '</svg>';

        const respondWith = markup => global.fetch.mockResolvedValue({ ok: true, text: () => Promise.resolve(markup) });

        beforeEach(() => {
            global.fetch = vi.fn();
            document.getElementById('forma-sprite-definitions')?.remove();
        });

        it('adopts the sprite symbols into the document', async () => {
            respondWith(sprite);

            await assets.loadSprite('/emojis/smileys.svg');

            expect(document.getElementById('emoji-1f600')).not.toBeNull();
            expect(document.getElementById('emoji-1f603')).not.toBeNull();
        });

        it('hides the definitions host from assistive technology and from layout', async () => {
            respondWith(sprite);

            await assets.loadSprite('/emojis/smileys.svg');

            const host = document.getElementById('forma-sprite-definitions');
            expect(host.getAttribute('aria-hidden')).toBe('true');
            expect(host.style.position).toBe('absolute');
        });

        it('keeps one host however many sprites arrive', async () => {
            respondWith(sprite);
            await assets.loadSprite('/emojis/smileys.svg');

            respondWith('<svg xmlns="http://www.w3.org/2000/svg"><symbol id="emoji-1f347" viewBox="0 0 36 36" /></svg>');
            await assets.loadSprite('/emojis/food.svg');

            expect(document.querySelectorAll('#forma-sprite-definitions')).toHaveLength(1);
            expect(document.getElementById('emoji-1f347')).not.toBeNull();
        });

        it('reports success once the symbols are in place', async () => {
            respondWith(sprite);

            await expect(assets.loadSprite('/emojis/smileys.svg')).resolves.toBe(true);
        });

        it('reports failure rather than rejecting when the response is not ok', async () => {
            global.fetch.mockResolvedValue({ ok: false });

            await expect(assets.loadSprite('/emojis/missing.svg')).resolves.toBe(false);
        });

        it('reports failure rather than rejecting when the request itself fails', async () => {
            global.fetch.mockRejectedValue(new TypeError('Network error'));

            await expect(assets.loadSprite('/emojis/missing.svg')).resolves.toBe(false);
        });

        it('reports failure rather than adopting a malformed sprite', async () => {
            respondWith('<svg><unclosed>');

            await expect(assets.loadSprite('/emojis/broken.svg')).resolves.toBe(false);
        });

        it('fetches once however many callers ask for the same url', async () => {
            respondWith(sprite);

            await Promise.all([assets.loadSprite('/emojis/smileys.svg'), assets.loadSprite('/emojis/smileys.svg')]);

            expect(global.fetch).toHaveBeenCalledTimes(1);
            expect(document.querySelectorAll('#emoji-1f600')).toHaveLength(1);
        });

        // A stalled connection would otherwise never settle, hanging every later caller on the cached promise.
        it('abandons a request that never settles', async () => {
            global.fetch.mockImplementation((_, options) => new Promise((_resolve, reject) => {
                // Without an abort signal there is nothing to cut this short, so the test times out.
                options.signal?.addEventListener('abort', () => reject(new DOMException('Aborted', 'TimeoutError')));
            }));

            await expect(assets.loadSprite('/emojis/stalled.svg', 20)).resolves.toBe(false);
        }, 1000);

        it('gives the request an abort signal', async () => {
            respondWith(sprite);

            await assets.loadSprite('/emojis/smileys.svg');

            expect(global.fetch.mock.calls[0][1].signal).toBeInstanceOf(AbortSignal);
        });

        // A failed load is forgotten, so a later attempt can retry rather than replaying the failure.
        it('retries after a failure', async () => {
            global.fetch.mockResolvedValueOnce({ ok: false });
            await expect(assets.loadSprite('/emojis/flaky.svg')).resolves.toBe(false);

            respondWith(sprite);
            await expect(assets.loadSprite('/emojis/flaky.svg')).resolves.toBe(true);
        });

        // An enhanced navigation replaces the body, host and symbols included, while the module lives on.
        it('adopts the symbols again after the host is swept from the document', async () => {
            respondWith(sprite);
            await assets.loadSprite('/emojis/smileys.svg');
            document.getElementById('forma-sprite-definitions').remove();

            await expect(assets.loadSprite('/emojis/smileys.svg')).resolves.toBe(true);

            expect(global.fetch).toHaveBeenCalledTimes(1);
            expect(document.getElementById('emoji-1f600')).not.toBeNull();
            expect(document.querySelectorAll('#forma-sprite-definitions')).toHaveLength(1);
        });
    });
});

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { stubAssetLoading } from '#support/platform.js';

describe('Asset', () => {
    /** @type {typeof import('#operations/Asset.js')} */
    let assets;
    /** @type {ReturnType<typeof stubAssetLoading>} */
    let head;

    beforeEach(async () => {
        head = stubAssetLoading();
        // The load cache is module state, so each test gets a fresh module.
        vi.resetModules();
        assets = await import('#operations/Asset.js');
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
});

/**
 * Embedding — third-party embed scripts and the rescan they need
 */

import { loadScript } from './Assets.js';

export async function activate(scriptUrl) {
    const isLoaded = await loadScript(scriptUrl);

    if (!isLoaded) {
        return false;
    }

    // Each service scans on load; markup rendered afterwards needs a second pass it only exposes this way.
    globalThis.instgrm?.Embeds?.process();
    globalThis.FB?.XFBML?.parse();
    globalThis.twttr?.widgets?.load();

    return true;
}

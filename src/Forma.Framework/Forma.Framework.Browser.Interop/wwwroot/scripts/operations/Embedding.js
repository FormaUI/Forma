/**
 * Embedding — third-party embed scripts and the rescan they need
 */

import { loadScript } from './Asset.js';

export async function activate(scriptUrl) {
    const isLoaded = await loadScript(scriptUrl);

    if (!isLoaded) {
        return false;
    }

    // Each service scans on load; these calls scan again for markup rendered since.
    globalThis.instgrm?.Embeds?.process();
    globalThis.FB?.XFBML?.parse();
    globalThis.twttr?.widgets?.load();

    return true;
}

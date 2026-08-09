// Loads a hosting service's embed script once per page, then asks it to process markup added since it loaded.
const loading = new Map();

function load(scriptUrl) {
    if (loading.has(scriptUrl)) {
        return loading.get(scriptUrl);
    }

    const pending = new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.src = scriptUrl;
        script.async = true;
        script.addEventListener('load', resolve, { once: true });
        script.addEventListener('error', () => reject(new Error(scriptUrl)), { once: true });
        document.head.appendChild(script);
    });

    // A failed load is forgotten so a later render can try the script again.
    pending.catch(() => loading.delete(scriptUrl));

    loading.set(scriptUrl, pending);
    return pending;
}

export async function activate(scriptUrl) {
    await load(scriptUrl);

    // Each service scans on load; markup rendered afterwards needs a second pass it only exposes this way.
    globalThis.instgrm?.Embeds?.process();
    globalThis.FB?.XFBML?.parse();
    globalThis.twttr?.widgets?.load();
}

// A browser reads the muted attribute only while parsing the element, so a rendered player has to be muted through the property.
export function mute(video) {
    video.muted = true;

    if (video.paused) {
        video.play().catch(() => { });
    }
}

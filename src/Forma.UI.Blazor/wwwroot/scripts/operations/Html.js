// Sanitizes untrusted markup before it reaches a live DOM, and strips the artefacts a word processor leaves behind.

const ALLOWED_TAGS = new Set([
    'p', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'blockquote', 'pre', 'code',
    'b', 'strong', 'i', 'em', 'u', 's', 'strike', 'sub', 'sup', 'mark', 'small',
    'ul', 'ol', 'li',
    'a', 'img', 'video', 'source', 'audio',
    'table', 'thead', 'tbody', 'tfoot', 'tr', 'th', 'td', 'caption', 'colgroup', 'col',
    'hr', 'br',
    'span', 'div',
    'details', 'summary', 'figure', 'figcaption',
]);

const DISCARDED_WITH_CONTENT = new Set([
    'script', 'style', 'iframe', 'object', 'svg', 'math', 'noscript', 'template', 'applet', 'frameset',
]);

const GLOBAL_ATTRIBUTES = new Set(['class', 'id', 'style', 'dir', 'title', 'lang']);

const TAG_ATTRIBUTES = {
    a: new Set(['href', 'target', 'rel', 'download', 'name']),
    img: new Set(['src', 'alt', 'width', 'height', 'loading']),
    video: new Set(['src', 'poster', 'controls', 'autoplay', 'loop', 'muted', 'preload', 'width', 'height']),
    audio: new Set(['src', 'controls', 'autoplay', 'loop', 'muted', 'preload']),
    source: new Set(['src', 'type']),
    th: new Set(['colspan', 'rowspan', 'scope', 'headers', 'align']),
    td: new Set(['colspan', 'rowspan', 'headers', 'align']),
    table: new Set(['border', 'cellpadding', 'cellspacing']),
    ol: new Set(['start', 'type', 'reversed']),
    li: new Set(['value']),
};

const URL_ATTRIBUTES = new Set(['href', 'src', 'poster']);
const SAFE_SCHEMES = new Set(['http:', 'https:', 'mailto:', 'tel:', 'data:']);

function isSafeUrl(value) {
    const trimmed = value.trim();

    // A fragment or a relative path carries no scheme to vet.
    if (trimmed === '' || trimmed.startsWith('#') || trimmed.startsWith('/')) {
        return true;
    }

    if (!/^[a-z][a-z0-9+.-]*:/i.test(trimmed)) {
        return true;
    }

    try {
        const scheme = new URL(trimmed, 'https://invalid.example').protocol;
        return SAFE_SCHEMES.has(scheme) && !(scheme === 'data:' && !/^data:image\//i.test(trimmed));
    } catch {
        return false;
    }
}

function cleanAttributes(element) {
    const tag = element.tagName.toLowerCase();
    const permitted = TAG_ATTRIBUTES[tag];

    for (const attribute of Array.from(element.attributes)) {
        const name = attribute.name.toLowerCase();
        const allowed = GLOBAL_ATTRIBUTES.has(name) || (permitted?.has(name) ?? false);

        if (!allowed || (URL_ATTRIBUTES.has(name) && !isSafeUrl(attribute.value))) {
            element.removeAttribute(attribute.name);
            continue;
        }

        if (name === 'style') {
            const kept = attribute.value
                .split(';')
                .filter(rule => rule.trim() !== '' && !/mso-|expression\s*\(|url\s*\(/i.test(rule));

            if (kept.length === 0) {
                element.removeAttribute('style');
            } else {
                element.setAttribute('style', `${kept.join(';').trim()};`);
            }
        }

        if (name === 'class') {
            const kept = attribute.value.split(/\s+/).filter(token => token !== '' && !/^Mso/i.test(token));

            if (kept.length === 0) {
                element.removeAttribute('class');
            } else {
                element.setAttribute('class', kept.join(' '));
            }
        }
    }
}

/** Returns the markup reduced to the elements, attributes and URL schemes a document may carry. */
export function sanitize(html) {
    const template = document.createElement('template');
    template.innerHTML = html ?? '';

    // Comments carry a word processor's conditional markup, which no reader needs.
    const comments = document.createTreeWalker(template.content, NodeFilter.SHOW_COMMENT);
    const stale = [];
    while (comments.nextNode()) {
        stale.push(comments.currentNode);
    }
    for (const comment of stale) {
        comment.remove();
    }

    for (const element of Array.from(template.content.querySelectorAll('*'))) {
        // A previous iteration may already have discarded an ancestor.
        if (!element.isConnected && !template.content.contains(element)) {
            continue;
        }

        const tag = element.tagName.toLowerCase();

        if (DISCARDED_WITH_CONTENT.has(tag) || tag.includes(':')) {
            element.remove();
            continue;
        }

        if (!ALLOWED_TAGS.has(tag)) {
            element.replaceWith(...element.childNodes);
            continue;
        }

        cleanAttributes(element);
    }

    return template.innerHTML;
}

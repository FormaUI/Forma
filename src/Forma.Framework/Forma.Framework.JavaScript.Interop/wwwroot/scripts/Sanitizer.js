/**
 * Sanitizer — untrusted markup reduced to what a document may carry, free of a word processor's artifacts
 */

const AllowedTags = new Set([
    'p', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'blockquote', 'pre', 'code',
    'b', 'strong', 'i', 'em', 'u', 's', 'strike', 'sub', 'sup', 'mark', 'small',
    'ul', 'ol', 'li',
    'a', 'img', 'video', 'source', 'audio',
    'table', 'thead', 'tbody', 'tfoot', 'tr', 'th', 'td', 'caption', 'colgroup', 'col',
    'hr', 'br',
    'span', 'div',
    'details', 'summary', 'figure', 'figcaption',
]);

const DiscardedWithContent = new Set([
    'script', 'style', 'iframe', 'object', 'svg', 'math', 'noscript', 'template', 'applet', 'frameset',
]);

const GlobalAttributes = new Set(['class', 'id', 'style', 'dir', 'title', 'lang']);

const TagAttributes = new Map([
    ['a', new Set(['href', 'target', 'rel', 'download', 'name'])],
    ['img', new Set(['src', 'alt', 'width', 'height', 'loading'])],
    ['video', new Set(['src', 'poster', 'controls', 'autoplay', 'loop', 'muted', 'preload', 'width', 'height'])],
    ['audio', new Set(['src', 'controls', 'autoplay', 'loop', 'muted', 'preload'])],
    ['source', new Set(['src', 'type'])],
    ['th', new Set(['colspan', 'rowspan', 'scope', 'headers', 'align'])],
    ['td', new Set(['colspan', 'rowspan', 'headers', 'align'])],
    ['table', new Set(['border', 'cellpadding', 'cellspacing'])],
    ['ol', new Set(['start', 'type', 'reversed'])],
    ['li', new Set(['value'])],
]);

const UrlAttributes = new Set(['href', 'src', 'poster']);
const SafeSchemes = new Set(['http:', 'https:', 'mailto:', 'tel:', 'data:']);

function isSafeUrl(value) {
    const trimmed = value.trim();

    if (trimmed === '' || trimmed.startsWith('#') || trimmed.startsWith('/')) {
        return true;
    }

    if (!/^[a-z][a-z0-9+.-]*:/i.test(trimmed)) {
        return true;
    }

    try {
        const scheme = new URL(trimmed, 'https://invalid.example').protocol;
        return SafeSchemes.has(scheme) && !(scheme === 'data:' && !/^data:image\//i.test(trimmed));
    } catch {
        return false;
    }
}

function cleanAttributes(element) {
    const tag = element.tagName.toLowerCase();
    const permitted = TagAttributes.get(tag);

    for (const attribute of Array.from(element.attributes)) {
        const name = attribute.name.toLowerCase();
        const allowed = GlobalAttributes.has(name) || (permitted?.has(name) ?? false);

        if (!allowed || (UrlAttributes.has(name) && !isSafeUrl(attribute.value))) {
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

    const comments = document.createTreeWalker(template.content, NodeFilter.SHOW_COMMENT);
    const stale = [];
    while (comments.nextNode()) {
        stale.push(comments.currentNode);
    }
    for (const comment of stale) {
        comment.remove();
    }

    for (const element of Array.from(template.content.querySelectorAll('*'))) {
        // An ancestor removed earlier in the loop takes this element with it.
        if (!template.content.contains(element)) {
            continue;
        }

        const tag = element.tagName.toLowerCase();

        if (DiscardedWithContent.has(tag) || tag.includes(':')) {
            element.remove();
            continue;
        }

        if (!AllowedTags.has(tag)) {
            element.replaceWith(...element.childNodes);
            continue;
        }

        cleanAttributes(element);
    }

    return template.innerHTML;
}

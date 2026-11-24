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

// Raster images alone, since an SVG document can carry script.
const ImageMediaTypes = new Set([
    'image/png', 'image/apng', 'image/jpeg', 'image/jpg', 'image/gif', 'image/webp', 'image/avif', 'image/bmp',
    'image/x-icon', 'image/vnd.microsoft.icon',
]);

const isOfType = (mediaType, type) => mediaType.length > type.length && mediaType.startsWith(type);

/** The schemes each kind of address allows beside a relative one, and the media a data address of it may carry. */
const AddressKinds = {
    link: { schemes: new Set(['http:', 'https:', 'mailto:', 'tel:']), allowsData: () => false },
    image: { schemes: new Set(['http:', 'https:', 'blob:']), allowsData: mediaType => ImageMediaTypes.has(mediaType) },
    video: { schemes: new Set(['http:', 'https:', 'blob:']), allowsData: mediaType => isOfType(mediaType, 'video/') },
    audio: { schemes: new Set(['http:', 'https:', 'blob:']), allowsData: mediaType => isOfType(mediaType, 'audio/') },
    media: { schemes: new Set(['http:', 'https:', 'blob:']), allowsData: mediaType => isOfType(mediaType, 'video/') || isOfType(mediaType, 'audio/') },
    frame: { schemes: new Set(['http:', 'https:', 'blob:']), allowsData: () => false },
};

/** What an attribute's address is to its element: a link, or the source of what the element fetches. */
function getAddressKind(tag, name) {
    if (name === 'href') {
        return AddressKinds.link;
    }

    if (name === 'poster' || tag === 'img') {
        return AddressKinds.image;
    }

    return { video: AddressKinds.video, audio: AddressKinds.audio, source: AddressKinds.media }[tag] ?? AddressKinds.frame;
}

/**
 * Whether the kind allows the address, read as the browser reads it: the URL parser drops the control characters and
 * spaces at either end and every tab and line break within, and a relative address resolves to the page's own scheme.
 */
function isAllowedAddress(value, kind) {
    let address;

    try {
        address = new URL(value, 'https://invalid.example');
    } catch {
        return false;
    }

    if (address.protocol === 'data:') {
        const comma = address.pathname.indexOf(',');

        return comma >= 0 && kind.allowsData(address.pathname.slice(0, comma).split(';')[0].trim().toLowerCase());
    }

    return kind.schemes.has(address.protocol);
}

function cleanAttributes(element) {
    const tag = element.tagName.toLowerCase();
    const permittedAttributes = TagAttributes.get(tag);

    for (const attribute of Array.from(element.attributes)) {
        const name = attribute.name.toLowerCase();
        const isAllowed = GlobalAttributes.has(name) || (permittedAttributes?.has(name) ?? false);

        if (!isAllowed || (UrlAttributes.has(name) && !isAllowedAddress(attribute.value, getAddressKind(tag, name)))) {
            element.removeAttribute(attribute.name);
            continue;
        }

        if (name === 'style') {
            const keptDeclarations = attribute.value
                .split(';')
                .filter(rule => rule.trim() !== '' && !/mso-|expression\s*\(|url\s*\(/i.test(rule));

            if (keptDeclarations.length === 0) {
                element.removeAttribute('style');
            } else {
                element.setAttribute('style', `${keptDeclarations.join(';').trim()};`);
            }
        }

        if (name === 'class') {
            const keptClasses = attribute.value.split(/\s+/).filter(token => token !== '' && !/^Mso/i.test(token));

            if (keptClasses.length === 0) {
                element.removeAttribute('class');
            } else {
                element.setAttribute('class', keptClasses.join(' '));
            }
        }
    }
}

/** Returns the markup reduced to the elements, attributes and URL schemes a document may carry. */
export function sanitize(html) {
    const template = document.createElement('template');
    template.innerHTML = html ?? '';

    const comments = document.createTreeWalker(template.content, NodeFilter.SHOW_COMMENT);
    const staleComments = [];
    while (comments.nextNode()) {
        staleComments.push(comments.currentNode);
    }
    for (const comment of staleComments) {
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

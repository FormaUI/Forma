/**
 * MediaMarkup — the markup image, resource, video, and embed requests build.
 */

import { Escape } from './Markup.mjs';

export class MediaMarkup {
    /** The markup an image request builds, or null when it names no source. */
    static image(data = {}) {
        const src = data.source ?? data.Source ?? '';
        const alt = data.altText ?? data.AltText ?? '';
        const caption = data.caption ?? data.Caption ?? '';

        if (!src) {
            return null;
        }

        return caption
            ? `<figure class="forma-composer-image"><img src="${Escape.attribute(src)}" alt="${Escape.attribute(alt)}" /><figcaption>${Escape.html(caption)}</figcaption></figure><p>&nbsp;</p>`
            : `<p><img class="forma-composer-image" src="${Escape.attribute(src)}" alt="${Escape.attribute(alt)}" /></p>`;
    }

    /** The markup a picked resource builds, or null when it names no source. */
    static resource(data = {}) {
        const src = data.source ?? data.Source ?? '';
        const name = data.name ?? data.Name ?? '';
        const kind = (data.kind ?? data.Kind ?? 'file').toLowerCase();
        const embed = data.embed ?? data.Embed ?? false;
        const link = data.link ?? data.Link ?? false;

        if (!src) {
            return null;
        }

        const address = Escape.attribute(src);
        const parts = [];

        if (embed && kind === 'image') {
            parts.push(`<img class="forma-composer-image" src="${address}" alt="${Escape.attribute(name)}" />`);
        } else if (embed && kind === 'video') {
            parts.push(`<video class="forma-composer-resource" src="${address}" controls></video>`);
        } else if (embed && kind === 'audio') {
            parts.push(`<audio class="forma-composer-resource" src="${address}" controls></audio>`);
        }

        // A file the browser cannot render, or one nothing above claimed, still has to arrive as something.
        if (link || parts.length === 0) {
            parts.push(`<a href="${address}" target="_blank" rel="noopener noreferrer">${Escape.html(name || src)}</a>`);
        }

        return `<p>${parts.join(' ')}</p>`;
    }

    /** The markup a video placeholder builds, or null when it names no address. */
    static video(data = {}) {
        const url = data.source ?? data.Source ?? data.url ?? data.Url ?? '';
        const title = data.title ?? data.Title ?? '';
        const autoplay = data.autoplay ?? data.Autoplay ?? false;

        if (!url) {
            return null;
        }

        const titleAttr = title ? ` data-video-title="${Escape.attribute(title)}"` : '';
        const autoplayAttr = autoplay ? ' data-autoplay="true"' : '';

        return `<div class="forma-composer-video" data-video-url="${Escape.attribute(url)}"${titleAttr}${autoplayAttr}><p><a href="${Escape.attribute(url)}" target="_blank" rel="noopener noreferrer">${Escape.html(title || url)}</a></p></div><p>&nbsp;</p>`;
    }

    /** The markup an embed placeholder builds, or null when it names neither a type nor an identifier. */
    static embed(data = {}) {
        const type = data.embedType ?? data.EmbedType ?? '';
        const id = data.identifier ?? data.Identifier ?? '';
        const settings = data.settings ?? data.Settings ?? '';

        if (!type && !id) {
            return null;
        }

        const settingsAttr = settings ? ` data-settings="${Escape.attribute(settings)}"` : '';

        return `<div class="forma-composer-embed" data-embed-type="${Escape.attribute(type)}" data-embed-id="${Escape.attribute(id)}"${settingsAttr}>[Embed: ${Escape.html(type || id)}]</div><p>&nbsp;</p>`;
    }

    /** What each kind is recognised by. A figure is tried before a bare picture, because closest()
        stops at the element itself and would otherwise lose the caption wrapped around it. */
    static Kinds = [
        { kind: 'image', selector: 'figure.forma-composer-image' },
        { kind: 'image', selector: 'img' },
        { kind: 'video', selector: '.forma-composer-video' },
        { kind: 'embed', selector: '.forma-composer-embed' },
        { kind: 'anchor', selector: 'a[name]' },
        { kind: 'link', selector: 'a[href]' },
    ];

    /** Reads back what a built element holds, so the dialog reopening on it starts where the author left off. */
    static read(kind, element) {
        switch (kind) {
            case 'image': {
                const picture = element.matches('img') ? element : element.querySelector('img');

                return {
                    source: picture?.getAttribute('src') ?? '',
                    altText: picture?.getAttribute('alt') ?? '',
                    caption: element.querySelector('figcaption')?.textContent ?? '',
                };
            }

            case 'video':
                return {
                    source: element.getAttribute('data-video-url') ?? '',
                    title: element.getAttribute('data-video-title') ?? '',
                    autoplay: element.getAttribute('data-autoplay') === 'true',
                };

            case 'embed':
                return {
                    embedType: element.getAttribute('data-embed-type') ?? '',
                    identifier: element.getAttribute('data-embed-id') ?? '',
                    settings: element.getAttribute('data-settings') ?? '',
                };

            default:
                return {};
        }
    }
}

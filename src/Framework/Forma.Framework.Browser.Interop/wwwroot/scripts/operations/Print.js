/**
 * Print — sending a document alone to the printer
 */

import { Transfer } from '../../../core/scripts/Transfer.js';

/** Prints the page as it stands. */
export function page() {
    window.print();
}

/** The markup by itself as a document of its own, titled and dressed in the page's own styles written into it, answered through the transfer. */
export function compose(html, title, className) {
    return Transfer.of(writeDocument(title, body => {
        const article = body.ownerDocument.createElement('article');
        article.className = className;
        article.innerHTML = html;
        body.append(article);
    }));
}

/** The page as it stands as a document of its own, titled and dressed in its own styles, its scripts left out, answered through the transfer. */
export function composePage(title) {
    return Transfer.of(writeDocument(title, body => {
        for (const node of document.body.childNodes) {
            body.append(body.ownerDocument.importNode(node, true));
        }

        for (const script of body.querySelectorAll('script')) {
            script.remove();
        }
    }));
}

function writeDocument(title, fill) {
    const writtenDocument = document.implementation.createHTMLDocument();
    writtenDocument.documentElement.lang = document.documentElement.lang;
    writtenDocument.documentElement.dir = document.documentElement.dir;

    const charset = writtenDocument.createElement('meta');
    charset.setAttribute('charset', 'utf-8');
    const heading = writtenDocument.createElement('title');
    heading.textContent = title;
    writtenDocument.head.append(charset, heading);

    const style = writtenDocument.createElement('style');
    style.textContent = [...document.styleSheets].map(readRules).join('\n');
    writtenDocument.head.append(style);

    fill(writtenDocument.body);

    return `<!DOCTYPE html>${writtenDocument.documentElement.outerHTML}`;
}

// A sheet another origin serves keeps its rules from script, so it is left out rather than failing the document.
function readRules(sheet) {
    try {
        return [...sheet.cssRules].map(rule => rule.cssText).join('\n');
    } catch {
        return '';
    }
}

/**
 * Prints markup by itself, dressed in the page's own stylesheets.
 * @param {string} html The document's markup.
 * @param {string} title The printed document's title.
 * @param {string} className The class the document's container carries.
 */
export function print(html, title, className) {
    const frame = document.createElement('iframe');
    frame.setAttribute('aria-hidden', 'true');
    frame.style.position = 'fixed';
    frame.style.inset = '0';
    frame.style.width = '0';
    frame.style.height = '0';
    frame.style.border = '0';
    frame.style.visibility = 'hidden';
    document.body.appendChild(frame);

    const printedDocument = frame.contentDocument;
    printedDocument.open();
    printedDocument.write(`<!DOCTYPE html><html lang="${document.documentElement.lang}" dir="${document.documentElement.dir}"><head><meta charset="utf-8"><title></title></head><body><article></article></body></html>`);
    printedDocument.close();

    printedDocument.title = title;
    printedDocument.body.firstElementChild.className = className;
    printedDocument.body.firstElementChild.innerHTML = html;

    for (const style of document.querySelectorAll('style')) {
        printedDocument.head.appendChild(printedDocument.importNode(style, true));
    }

    const sheets = [...document.styleSheets]
        .map(sheet => sheet.href)
        .filter(Boolean)
        .map(href => new Promise(resolve => {
            const link = printedDocument.createElement('link');
            link.rel = 'stylesheet';
            link.href = href;
            link.addEventListener('load', resolve, { once: true });
            link.addEventListener('error', resolve, { once: true });
            printedDocument.head.appendChild(link);
        }));

    frame.contentWindow.addEventListener('afterprint', () => frame.remove(), { once: true });

    return Promise.all(sheets).then(() => {
        frame.contentWindow.focus();
        frame.contentWindow.print();
    });
}

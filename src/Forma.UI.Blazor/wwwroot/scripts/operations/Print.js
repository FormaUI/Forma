/**
 * Print — sending a document alone to the printer
 */

/** Prints the page as it stands. */
export function page() {
    window.print();
}

/**
 * Prints markup by itself, dressed in the page's own stylesheets.
 * @param {string} html The document's markup.
 * @param {string} title The printed document's title.
 * @param {string} className The class the document's container carries, so the page's prose styles apply to it.
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

    const printed = frame.contentDocument;
    printed.open();
    printed.write(`<!DOCTYPE html><html lang="${document.documentElement.lang}" dir="${document.documentElement.dir}"><head><meta charset="utf-8"><title></title></head><body><article></article></body></html>`);
    printed.close();

    printed.title = title;
    printed.body.firstElementChild.className = className;
    printed.body.firstElementChild.innerHTML = html;

    const sheets = [...document.styleSheets]
        .map(sheet => sheet.href)
        .filter(Boolean)
        .map(href => new Promise(resolve => {
            const link = printed.createElement('link');
            link.rel = 'stylesheet';
            link.href = href;
            link.onload = resolve;
            link.onerror = resolve;
            printed.head.appendChild(link);
        }));

    frame.contentWindow.addEventListener('afterprint', () => frame.remove(), { once: true });

    return Promise.all(sheets).then(() => {
        frame.contentWindow.focus();
        frame.contentWindow.print();
    });
}

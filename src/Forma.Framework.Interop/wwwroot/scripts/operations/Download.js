/**
 * Download — handing the reader a file to save
 */

/** How long the file's address stays valid after the click that queues its download. */
const RevokeDelay = 40_000;

/**
 * Saves text as a file through the browser's download path.
 * @param {string} name The file name offered.
 * @param {string} content The file's text.
 * @param {string} type The file's media type.
 */
export function save(name, content, type) {
    const url = URL.createObjectURL(new Blob([content], { type }));
    const link = document.createElement('a');
    link.href = url;
    link.download = name;
    link.click();

    // A click only queues the download; the browser reads the address when it starts it.
    setTimeout(() => URL.revokeObjectURL(url), RevokeDelay);
}

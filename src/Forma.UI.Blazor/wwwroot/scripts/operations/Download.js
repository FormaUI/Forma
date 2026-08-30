/**
 * Download — handing the reader a file to save
 */

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

    // The address stays valid until the browser has started the download, which a click only queues.
    setTimeout(() => URL.revokeObjectURL(url));
}

/**
 * Share — handing a subject to the platform's own share surface
 */

/** Whether this browser offers a native share surface. */
export function isAvailable() {
    return typeof navigator.share === 'function';
}

/**
 * Offers the subject to the platform's share surface; declining it there is not a failure.
 * @param {string} title
 * @param {string} url
 */
export function share(title, url) {
    return navigator.share({ title, url }).then(() => true, () => false);
}

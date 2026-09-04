/**
 * Playback — playback state a rendered media element only exposes as a property
 */

/** @param {HTMLMediaElement} media */
export function mute(media) {
    // A browser reads the muted attribute only while parsing the element, so a rendered player has to be muted through the property.
    media.muted = true;

    if (media.paused) {
        media.play().catch(() => { });
    }
}

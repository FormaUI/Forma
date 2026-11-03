/**
 * Playback — playback state a media element holds as a DOM property
 */

/** @param {HTMLMediaElement} media */
export function mute(media) {
    // The muted attribute is read only when the element is parsed.
    media.muted = true;

    if (media.paused) {
        media.play().catch(() => { });
    }
}

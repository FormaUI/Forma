/**
 * Scroll — moving a container to its center
 *
 * A right-to-left container counts scrollLeft downward from zero, so its inline middle is mirrored.
 */

import { Direction } from '../../../core/scripts/Direction.js';

/** @param {HTMLElement} container */
export function toCenter(container) {
    const middle = (container.scrollWidth - container.clientWidth) / 2;

    container.scrollLeft = Direction.isRightToLeft(container) ? -middle : middle;
    container.scrollTop = (container.scrollHeight - container.clientHeight) / 2;
}

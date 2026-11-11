/**
 * Scroll — moving a container to its center
 */

import { Direction } from '../../../core/scripts/Direction.js';

/** @param {HTMLElement} container */
export function toCenter(container) {
    const middleOffset = (container.scrollWidth - container.clientWidth) / 2;

    container.scrollLeft = Direction.isRightToLeft(container) ? -middleOffset : middleOffset;
    container.scrollTop = (container.scrollHeight - container.clientHeight) / 2;
}

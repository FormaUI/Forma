/**
 * Scroll — moving a scroll container to a position it cannot express declaratively
 */

/** @param {HTMLElement} container */
export function toCenter(container) {
    container.scrollLeft = (container.scrollWidth - container.clientWidth) / 2;
    container.scrollTop = (container.scrollHeight - container.clientHeight) / 2;
}

/**
 * @param {HTMLElement} container
 * @param {number} direction -1 toward the start of the container, 1 toward its end.
 */
export function byPage(container, direction) {
    // A right-to-left container counts scrollLeft downwards from zero, so the step mirrors with it.
    const isRightToLeft = getComputedStyle(container).direction === 'rtl';
    const step = container.clientWidth * 0.8 * direction;

    container.scrollBy({ left: isRightToLeft ? -step : step, behavior: 'smooth' });
}

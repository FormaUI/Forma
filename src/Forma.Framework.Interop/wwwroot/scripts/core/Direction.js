/**
 * Direction — the reading direction an element is laid out in
 */

export class Direction {
    /** Whether the element lays out right to left. */
    static isRightToLeft(element) {
        return getComputedStyle(element).direction === 'rtl';
    }
}

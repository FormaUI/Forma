/**
 * Scrolling — which elements scroll
 */

export class Scrolling {
    /** Whether the element holds more content than its height shows. */
    static scrolls(element) {
        return element.scrollHeight > element.clientHeight;
    }

    /**
     * Whether the element scrolls vertically on its own: its overflow lets it, and its content asks for it.
     * @param {Element} element
     * @param {CSSStyleDeclaration} style The element's computed style, when the caller already holds it.
     */
    static isScroller(element, style = getComputedStyle(element)) {
        const { overflowY } = style;

        return (overflowY === 'auto' || overflowY === 'scroll' || overflowY === 'overlay') && Scrolling.scrolls(element);
    }

    /**
     * The nearest of the element and its ancestors that scrolls vertically, or null when none does before the walk
     * reaches `until`, which is not itself considered.
     * @param {Element | null} from
     * @param {Element | null} until
     */
    static nearestScroller(from, until = null) {
        for (let node = from; node && node !== until; node = node.parentElement) {
            if (Scrolling.isScroller(node)) {
                return node;
            }
        }

        return null;
    }
}

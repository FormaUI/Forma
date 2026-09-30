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
     * @param {CSSStyleDeclaration} style The element's computed style.
     */
    static isScroller(element, style = getComputedStyle(element)) {
        const { overflowY } = style;

        return (overflowY === 'auto' || overflowY === 'scroll' || overflowY === 'overlay') && Scrolling.scrolls(element);
    }

    /**
     * The nearest of `from` and its ancestors below `until` that scrolls vertically.
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

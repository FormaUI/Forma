/**
 * Markup — text made safe for markup, and color spellings normalized for it.
 */

/** Escapes text for the markup context it lands in. */
export class Escape {
    /** Escapes text for an attribute value. */
    static attribute(str) {
        return (str ?? '')
            .replaceAll('&', '&amp;')
            .replaceAll('"', '&quot;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;');
    }

    /** Escapes text for element content. */
    static html(str) {
        return (str ?? '')
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;');
    }
}

/** Color value spellings. */
export class CssColor {
    /** Converts an rgb() or rgba() color to #rrggbb, leaving any other notation alone. */
    static toHex(value) {
        const parts = /^rgba?\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)/.exec(value);

        return parts
            ? `#${parts.slice(1, 4).map(part => Number(part).toString(16).padStart(2, '0')).join('')}`
            : value;
    }
}

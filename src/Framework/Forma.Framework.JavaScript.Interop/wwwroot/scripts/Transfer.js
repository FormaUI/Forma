/**
 * Transfer — a value crossing to .NET
 *
 * A value a reader can make large travels inline while the call carrying it fits the budget, and as a stream of its
 * bytes beyond it, which Blazor sends in chunks under any hub limit.
 */

/** The encoded size a value travels inline within: half Blazor's default 32 KB hub limit, the rest left to the call's other arguments and the message's framing. */
const Budget = 16 * 1024;

const encoder = new TextEncoder();

/** @typedef {{ value: unknown } | { stream: unknown, text: boolean }} Transferred */

export class Transfer {
    /** The encoded size a value travels inline within. */
    static get budget() {
        return Budget;
    }

    /**
     * Wraps a value for .NET: inline where its encoding fits the budget, and otherwise a stream of its bytes, a text's
     * own and any other value's JSON.
     * @param {unknown} value
     * @returns {Transferred}
     */
    static of(value) {
        const json = JSON.stringify(value ?? null);

        // The hub carries a call's arguments as a JSON string inside a JSON message of its own, so a value is escaped
        // twice on its way, and the budget is measured on that.
        if (Transfer.#encodedLength(JSON.stringify(json)) <= Budget) {
            return { value: value ?? null };
        }

        const isText = typeof value === 'string';
        const bytes = encoder.encode(isText ? value : json);

        return { stream: globalThis.DotNet.createJSStreamReference(bytes), text: isText };
    }

    // A UTF-16 unit encodes to three UTF-8 bytes at most, so a text that short fits without being encoded.
    static #encodedLength(text) {
        return text.length * 3 <= Budget ? text.length : encoder.encode(text).length;
    }
}

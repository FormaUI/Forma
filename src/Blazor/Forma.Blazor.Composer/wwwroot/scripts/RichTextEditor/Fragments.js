/**
 * Fragments — what a document fragment holds.
 */

export class Fragments {
    /** Whether the fragment holds any text or any element. */
    static hasContent(fragment) {
        return fragment.textContent.length > 0 || fragment.querySelector('*') !== null;
    }
}

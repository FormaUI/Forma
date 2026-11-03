/**
 * DocumentOrder — the order elements stand in within the document
 */

import { Transfer } from '../../../core/scripts/Transfer.js';

/**
 * The places of `elements` in the order they stand in the document, those no longer in it last, in the order given,
 * answered through the transfer.
 * @param {Element[]} elements
 */
export function sort(elements) {
    return Transfer.of(elements
        .map((element, index) => ({ element, index }))
        .sort((first, second) => {
            if (!first.element?.isConnected || !second.element?.isConnected) {
                return Number(!first.element?.isConnected) - Number(!second.element?.isConnected) || first.index - second.index;
            }

            return first.element.compareDocumentPosition(second.element) & Node.DOCUMENT_POSITION_FOLLOWING ? -1 : 1;
        })
        .map(({ index }) => index));
}

/**
 * ElementLabels — the labels elements are read by
 */

import { Transfer } from '../../../core/scripts/Transfer.js';

/**
 * The label each of `elements` is read by, in the order given: its own `aria-label`, or its text without what is hidden
 * from assistive technology, its white space collapsed, and empty for one no longer in the document; answered through
 * the transfer.
 * @param {Element[]} elements
 */
export function read(elements) {
    return Transfer.of(elements.map(element => element?.isConnected ? labelOf(element) : ''));
}

function labelOf(element) {
    const label = element.getAttribute('aria-label')?.trim();

    return label || textOf(element).replace(/\s+/g, ' ').trim();
}

function textOf(node) {
    if (node.nodeType === Node.TEXT_NODE) {
        return node.textContent;
    }

    if (node.nodeType !== Node.ELEMENT_NODE || node.getAttribute('aria-hidden') === 'true') {
        return '';
    }

    return Array.from(node.childNodes, textOf).join('');
}

/**
 * Focus — what the keyboard can reach
 */

/** What Tab moves between. */
const FocusableSelector = 'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

/**
 * The controls inside a root that Tab can reach, in document order, leaving out any that are not displayed.
 * @param {ParentNode} root
 * @returns {HTMLElement[]}
 */
export function focusablesIn(root) {
    return [...root.querySelectorAll(FocusableSelector)].filter(isDisplayed);
}

/** Whether an element and every ancestor of it is displayed. */
function isDisplayed(element) {
    for (let node = element; node; node = node.parentElement) {
        const style = getComputedStyle(node);
        if (style.display === 'none' || style.visibility === 'hidden') {
            return false;
        }
    }

    return true;
}

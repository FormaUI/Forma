/**
 * Controls — what a native control claims for itself, which a list around it leaves alone
 */

/** The selectors and keys native controls answer themselves. */
export class Controls {
    /** Elements Tab can reach. */
    static Focusable = 'button:not([disabled]):not([tabindex="-1"]), [href]:not([tabindex="-1"]), input:not([disabled]):not([tabindex="-1"]), select:not([disabled]):not([tabindex="-1"]), textarea:not([disabled]):not([tabindex="-1"]), [tabindex]:not([tabindex="-1"])';

    /** Elements that own their own press and keys. */
    static Interactive = 'a[href], button, input, select, textarea, label, [role="button"], [role="link"], [contenteditable="true"]';

    /** Fields where the arrow, Home and End keys move a caret rather than the focused item. */
    static TextEntry = 'select, textarea, [contenteditable="true"], input:not([type="checkbox"]):not([type="radio"]):not([type="button"]):not([type="submit"])';

    /** The keys a list owns, whose default would scroll the page. */
    static NavigationKeys = new Set(['ArrowUp', 'ArrowDown', 'Home', 'End']);
}

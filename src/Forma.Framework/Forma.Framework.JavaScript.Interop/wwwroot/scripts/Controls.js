/**
 * Controls — what a native control claims for itself, which a list around it leaves alone
 *
 * Which keys a control consumes is the input router's native table; here are the selectors alone.
 */

/** The selectors of what native controls answer themselves. */
export class Controls {
    /** Elements Tab can reach. */
    static Focusable = 'button:not([disabled]):not([tabindex="-1"]), [href]:not([tabindex="-1"]), input:not([disabled]):not([tabindex="-1"]), select:not([disabled]):not([tabindex="-1"]), textarea:not([disabled]):not([tabindex="-1"]), [tabindex]:not([tabindex="-1"])';

    /** Elements that own their own press and keys. */
    static Interactive = 'a[href], button, input, select, textarea, label, [role="button"], [role="link"], [contenteditable="true"]';
}

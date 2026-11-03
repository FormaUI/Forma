/**
 * Controls — the selectors of native controls
 */

/** The selectors of native controls. */
export class Controls {
    /** Elements Tab can reach. */
    static Focusable = 'button:not([disabled]):not([tabindex="-1"]), [href]:not([tabindex="-1"]), input:not([disabled]):not([tabindex="-1"]), select:not([disabled]):not([tabindex="-1"]), textarea:not([disabled]):not([tabindex="-1"]), [tabindex]:not([tabindex="-1"])';

    /** Elements that own their own press and keys. */
    static Interactive = 'a[href], button, input, select, textarea, label, [role="button"], [role="link"], [contenteditable="true"]';
}

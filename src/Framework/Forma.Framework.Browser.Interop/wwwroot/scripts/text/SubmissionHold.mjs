/**
 * SubmissionHold — a form's submission held until the edits of the fields in it have reached their owners
 */

/**
 * @typedef {object} HeldEdits
 * @property {Element} element The field the edits are of.
 * @property {boolean} isDelivering Whether an edit is still on its way to the owner.
 * @property {Promise<unknown>} delivered Settles once every edit sent so far has reached the owner.
 */

/** @type {Set<HeldEdits>} */
const trackedEdits = new Set();

/** @type {AbortController | null} */
let controller = null;

export class SubmissionHold {
    /**
     * Holds the submission of the form a field's edits are in while an edit is on its way, until the handle is disposed.
     * @param {HeldEdits} edits
     */
    static track(edits) {
        trackedEdits.add(edits);

        // Heard on the window as the submission is captured, ahead of every listener on the document, Blazor's among them.
        if (controller === null) {
            controller = new AbortController();
            globalThis.addEventListener('submit', event => SubmissionHold.#hold(event), { capture: true, signal: controller.signal });
        }

        return {
            dispose() {
                trackedEdits.delete(edits);

                if (trackedEdits.size === 0 && controller !== null) {
                    controller.abort();
                    controller = null;
                }
            },
        };
    }

    // A submission made while a field's edit is on its way is made again, by the same submitter, once the edit has arrived, so the form
    // is validated and submitted with what the reader typed.
    static #hold(event) {
        const form = event.target;

        if (!(form instanceof HTMLFormElement)) {
            return;
        }

        const deliveringEdits = [...trackedEdits].filter(edits => edits.isDelivering && SubmissionHold.#isWithin(edits.element, form));

        if (deliveringEdits.length === 0) {
            return;
        }

        event.preventDefault();
        event.stopImmediatePropagation();

        const submitter = event.submitter ?? null;

        Promise.allSettled(deliveringEdits.map(edits => edits.deliveryCompletion)).then(() => {
            if (form.isConnected) {
                form.requestSubmit(submitter?.isConnected && submitter.form === form ? submitter : null);
            }
        });
    }

    static #isWithin(element, form) {
        return element.form === form || form.contains(element);
    }
}

/**
 * AutofillObserver — whether the browser filled a field without the reader typing
 *
 * The field stylesheet starts one zero-duration animation as the browser fills a field and another as the fill clears,
 * and each one's start is the signal; a fill made before the observation began is read from the field's autofill state.
 */

/** @typedef {{ isFilled: boolean }} AutofillState */

const FilledAnimation = 'forma-autofill-start';
const ClearedAnimation = 'forma-autofill-cancel';

export class AutofillObserver {
    #field;
    #publish;
    #abort = new AbortController();
    #isFilled;
    /** @type {AutofillState | null} */
    #last = null;

    /**
     * @param {HTMLInputElement} field The field observed.
     * @param {object} options The observation's options.
     * @param {(state: AutofillState) => void} publish Publishes each changed state.
     */
    constructor(field, options, publish) {
        this.#field = field;
        this.#publish = publish;
        this.#isFilled = readAutofill(field) ?? false;

        field.addEventListener('animationstart', event => {
            if (event.animationName === FilledAnimation) {
                this.#take(true);
            } else if (event.animationName === ClearedAnimation) {
                this.#take(false);
            }
        }, { signal: this.#abort.signal });
    }

    /** Reads whether the field is filled now. */
    read() {
        this.#last = { isFilled: this.#isFilled };

        return this.#last;
    }

    /** Publishes whether the field is filled again, read afresh where the browser can say. */
    refresh() {
        this.#last = null;
        this.#take(readAutofill(this.#field) ?? this.#isFilled);
    }

    dispose() {
        this.#abort.abort();
    }

    #take(isFilled) {
        this.#isFilled = isFilled;

        if (this.#abort.signal.aborted || this.#last?.isFilled === isFilled) {
            return;
        }

        this.#last = { isFilled };
        this.#publish(this.#last);
    }
}

// The field's autofill state where the browser matches it by a selector, or null where it matches neither form.
function readAutofill(field) {
    for (const selector of [':autofill', ':-webkit-autofill']) {
        try {
            return field.matches(selector);
        } catch {
            // An unknown pseudo-class throws, and the next form is tried.
        }
    }

    return null;
}

/**
 * TextEdits — a text field's edits, sent to its owner as they happen
 */

import { Transfer } from '../../../core/scripts/Transfer.js';
import { CharacterCount } from './CharacterCount.mjs';
import { SubmissionHold } from './SubmissionHold.mjs';
import { TextSources } from './TextSources.mjs';

/** @typedef {(method: string, ...args: unknown[]) => Promise<unknown>} Report */
/** @typedef {{ name: string, report: Report, predecessor: TextEdits | null }} Owner */

export class TextEdits {
    /** The commands the owner runs on the field's edits. */
    static commands = new Set(['write', 'resend', 'flush']);

    #element;
    /** @type {import('./TextSources.mjs').TextSource} */
    #source;
    #sendsOnInput;
    #name;
    #report;
    #text;
    #revision;
    /** Whether the field has committed every edit it sent. */
    #isCommitted = true;
    /** @type {CharacterCount | null} */
    #count = null;
    /** The sends the owner has yet to take. */
    #sending = new Set();
    #hold;
    #controller = new AbortController();

    /**
     * @param {HTMLElement} field The field edited, or an element whose script provides the text its edits read and write.
     * @param {{ sendsOnInput: boolean, count?: { output: string, maxLength?: number | null } | null, text: string, revision: number }} options
     * Whether the field sends each edit as it is typed, the running count of its characters it shows, and the text its owner holds with
     * the revision it stands at.
     * @param {Owner} owner The registration as messages name it, how the edits reach the owner, and the edits the field's previous
     * registration sent.
     */
    constructor(field, { sendsOnInput, count = null, text, revision }, { name, report, predecessor }) {
        this.#element = field;
        this.#source = TextSources.of(field);
        this.#sendsOnInput = sendsOnInput;
        this.#name = name;
        this.#report = report;

        // The edits go on from where the previous registration left them, and an element drawn in place of its predecessor's takes the
        // text the reader left there. A field holding other than its owner's text holds what the reader typed before the edits attached.
        if (predecessor) {
            this.#text = predecessor.#text;
            this.#revision = predecessor.#revision;

            if (predecessor.#element !== field) {
                this.#source.write(predecessor.#source.read());
            }
        } else {
            this.#text = text;
            this.#revision = revision;
        }

        if (count) {
            this.#count = new CharacterCount(field, count);
        }

        // A submission of the form the field is in waits while an edit of it is on its way.
        this.#hold = SubmissionHold.track(this);

        this.#source.listen(() => this.#sendsOnInput && this.#send(false), () => this.#send(true), this.#controller.signal);

        // What the reader typed before the edits listened is sent at once, as a commit where the reader has left the field, since its
        // change went unheard; a field still holding the focus updating on change commits as it is left.
        if (this.#source.read() !== this.#text) {
            const isLeft = !this.#source.holdsFocus();

            if (isLeft || this.#sendsOnInput) {
                this.#send(isLeft);
            }
        }
    }

    /**
     * The edit that turns `before` into `after`: where it starts, the length it removes and the text it inserts, never parting a surrogate
     * pair, which the inserted text could not otherwise carry.
     * @param {string} before
     * @param {string} after
     */
    static diff(before, after) {
        const shortest = Math.min(before.length, after.length);
        let start = 0;

        while (start < shortest && before.charCodeAt(start) === after.charCodeAt(start)) {
            start++;
        }

        if (start > 0 && isHighSurrogate(before.charCodeAt(start - 1))) {
            start--;
        }

        let end = 0;

        while (end < shortest - start && before.charCodeAt(before.length - 1 - end) === after.charCodeAt(after.length - 1 - end)) {
            end++;
        }

        if (end > 0 && isLowSurrogate(before.charCodeAt(before.length - end))) {
            end--;
        }

        return { start, removed: before.length - start - end, inserted: after.slice(start, after.length - end) };
    }

    /** The field the edits are of. */
    get element() {
        return this.#element;
    }

    /** Whether an edit is still on its way to the owner. */
    get isDelivering() {
        return this.#sending.size > 0;
    }

    /** Settles once every edit sent so far has reached the owner, or failed on its way. */
    get delivered() {
        return Promise.allSettled([...this.#sending]);
    }

    /**
     * Writes a text of the owner's own into the field, returning the revision it stands at.
     * @param {string} text
     */
    write(text) {
        this.#source.write(text);
        this.#text = text;
        this.#count?.show();

        return ++this.#revision;
    }

    /**
     * Sends the owner the field's whole text at the revision it stands at, in place of the edits that led to it, where the owner's text
     * stands at another revision; returns whether it sent it.
     * @param {number} revision The revision the owner's text stands at.
     */
    resend(revision) {
        if (revision === this.#revision) {
            return false;
        }

        this.#deliver(this.#report('ReplaceAsync', this.#revision, Transfer.of(this.#text)));

        return true;
    }

    /** Commits what the field holds where it holds an edit not yet committed, returning the revision it then stands at. */
    flush() {
        if (this.#source.read() !== this.#text || !this.#isCommitted) {
            this.#send(true);
        }

        return this.#revision;
    }

    dispose() {
        this.#controller.abort();
        this.#count?.dispose();
        this.#hold.dispose();
    }

    #send(isCommit) {
        const text = this.#source.read();

        if (text === this.#text && (!isCommit || !this.#source.commitsUnchanged)) {
            return;
        }

        const { start, removed, inserted } = TextEdits.diff(this.#text, text);
        this.#text = text;
        this.#isCommitted = isCommit;

        this.#deliver(this.#report('EditAsync', ++this.#revision, start, removed, Transfer.of(inserted), isCommit));
    }

    // A send is on its way until the owner has taken it or it failed, which is reported.
    #deliver(sending) {
        const delivery = sending.catch(error => this.#fail(error));
        this.#sending.add(delivery);
        delivery.finally(() => this.#sending.delete(delivery));
    }

    #fail(error) {
        console.error(`Forma: an edit of ${this.#name} could not be delivered; its owner has the field send its whole text once the connection is back.`, error);
    }
}

function isHighSurrogate(code) {
    return code >= 0xD800 && code <= 0xDBFF;
}

function isLowSurrogate(code) {
    return code >= 0xDC00 && code <= 0xDFFF;
}

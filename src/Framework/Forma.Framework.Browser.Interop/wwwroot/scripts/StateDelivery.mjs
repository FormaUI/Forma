/**
 * StateDelivery — one observation's state on its way to its owner
 */

export class StateDelivery {
    #send;
    #report;
    #deliversEveryState;
    /** The samples waiting to be sent, in the order they were measured. */
    #waiting = [];
    /** The sample measured last, which a failed send leaves waiting. */
    #latest = null;
    #isInFlight = false;
    #isOpen = false;
    #isSuspended = false;
    #isClosed = false;
    #opening = null;
    #sending = 0;
    /** The resumes so far; a send made before the last of them is given up on. */
    #resumes = 0;

    /**
     * @param {(state: unknown) => Promise<unknown>} send Sends a sample to the owner.
     * @param {(error: unknown) => void} report Reports a send that failed.
     * @param {{ deliversEveryState?: boolean }} options Whether every sample is sent as it is measured, ahead of anything the
     *   reader does next, rather than one in flight with only the latest waiting behind it.
     */
    constructor(send, report, { deliversEveryState = false } = {}) {
        this.#send = send;
        this.#report = report;
        this.#deliversEveryState = deliversEveryState;
    }

    /** Whether delivery is suspended. */
    get isSuspended() {
        return this.#isSuspended;
    }

    /** Starts delivery. */
    open() {
        if (this.#isOpen || this.#opening !== null || this.#isClosed) {
            return;
        }

        this.#opening = setTimeout(() => {
            this.#opening = null;
            this.#isOpen = true;
            this.#flush();
        }, 0);
    }

    /** Takes the latest sample, behind those waiting where every sample is sent, and in their place otherwise or while suspended. */
    offer(state) {
        if (this.#isClosed) {
            return;
        }

        this.#latest = state;

        if (this.#deliversEveryState && !this.#isSuspended) {
            this.#waiting.push(state);
        } else {
            this.#waiting = [state];
        }

        this.#flush();
    }

    /** Resumes delivery. */
    resume() {
        this.#isSuspended = false;
        this.#isInFlight = false;
        this.#sending++;
        this.#resumes++;
        this.#flush();
    }

    /** Stops delivery. */
    close() {
        this.#isClosed = true;
        this.#waiting = [];

        if (this.#opening !== null) {
            clearTimeout(this.#opening);
            this.#opening = null;
        }
    }

    #flush() {
        while (this.#isOpen && !this.#isSuspended && !this.#isClosed && this.#waiting.length > 0 && (this.#deliversEveryState || !this.#isInFlight)) {
            this.#sendNext();
        }
    }

    #sendNext() {
        const state = this.#waiting.shift();
        const sending = ++this.#sending;
        const resumes = this.#resumes;
        this.#isInFlight = true;

        this.#send(state).then(
            () => {
                if (sending !== this.#sending) {
                    return;
                }

                this.#isInFlight = false;
                this.#flush();
            },
            error => {
                if (this.#isClosed || resumes !== this.#resumes) {
                    this.#report(error);
                    return;
                }

                // The latest sample waits for the resume, the one that failed or one measured after it.
                this.#isInFlight = false;
                this.#waiting = [this.#latest];
                this.#isSuspended = true;
                this.#report(error);
            });
    }
}

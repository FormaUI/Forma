/**
 * StateDelivery — one observation's state on its way to its owner
 *
 * One sample is in flight at a time and the latest measured since waits behind it, so a slow owner
 * holds at most two. The first sample is held until a task after the observation's attach returns,
 * so the attach result reaches the owner first. A send that fails is reported and suspends delivery,
 * keeping the latest sample, until the owner resumes it. Resuming also gives up on a send still
 * outstanding, since a call made while the circuit was down is never answered, and an answer that
 * arrives for a send given up on changes nothing.
 */

export class StateDelivery {
    #send;
    #report;
    #pending = null;
    #hasPending = false;
    #isInFlight = false;
    #isOpen = false;
    #isSuspended = false;
    #isClosed = false;
    #opening = null;
    #sending = 0;

    /**
     * @param {(state: unknown) => Promise<unknown>} send Sends a sample to the owner.
     * @param {(error: unknown) => void} report Reports a send that failed.
     */
    constructor(send, report) {
        this.#send = send;
        this.#report = report;
    }

    /** Whether delivery is suspended after a failed send. */
    get isSuspended() {
        return this.#isSuspended;
    }

    /** Starts sending a task from now, once the attach that made the observation has returned. */
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

    /** Takes a newly measured sample, replacing any still waiting. */
    offer(state) {
        if (this.#isClosed) {
            return;
        }

        this.#pending = state;
        this.#hasPending = true;
        this.#flush();
    }

    /** Resumes delivery after a failed send or one never answered, starting with the latest sample. */
    resume() {
        this.#isSuspended = false;
        this.#isInFlight = false;
        this.#sending++;
        this.#flush();
    }

    /** Stops delivery, dropping the sample waiting. */
    close() {
        this.#isClosed = true;
        this.#pending = null;
        this.#hasPending = false;

        if (this.#opening !== null) {
            clearTimeout(this.#opening);
            this.#opening = null;
        }
    }

    #flush() {
        if (!this.#isOpen || this.#isInFlight || this.#isSuspended || this.#isClosed || !this.#hasPending) {
            return;
        }

        const state = this.#pending;
        const sending = ++this.#sending;
        this.#pending = null;
        this.#hasPending = false;
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
                if (sending !== this.#sending || this.#isClosed) {
                    this.#report(error);
                    return;
                }

                this.#isInFlight = false;

                // The sample that failed is the latest the owner has not received, unless a newer one arrived meanwhile.
                if (!this.#hasPending) {
                    this.#pending = state;
                    this.#hasPending = true;
                }

                this.#isSuspended = true;
                this.#report(error);
            });
    }
}

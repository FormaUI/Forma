/**
 * StateDelivery — one observation's state on its way to its owner
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

    /** Takes the latest sample, replacing any still waiting. */
    offer(state) {
        if (this.#isClosed) {
            return;
        }

        this.#pending = state;
        this.#hasPending = true;
        this.#flush();
    }

    /** Resumes delivery. */
    resume() {
        this.#isSuspended = false;
        this.#isInFlight = false;
        this.#sending++;
        this.#flush();
    }

    /** Stops delivery. */
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

                // A failed sample is kept unless a newer one waits.
                if (!this.#hasPending) {
                    this.#pending = state;
                    this.#hasPending = true;
                }

                this.#isSuspended = true;
                this.#report(error);
            });
    }
}

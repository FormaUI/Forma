/**
 * FrameScheduler — one piece of work held to the next animation frame
 *
 * Events arrive faster than frames are painted; work requested any number of times before the
 * frame runs once, in that frame.
 */

export class FrameScheduler {
    #handle = null;

    /** Whether work is waiting for the next frame. */
    get isPending() {
        return this.#handle !== null;
    }

    /** Schedules the work for the next frame, unless work is already waiting for it. */
    request(work) {
        if (this.#handle !== null) {
            return;
        }

        this.#handle = requestAnimationFrame(() => {
            this.#handle = null;
            work();
        });
    }

    /** Drops the work waiting for the next frame, if any. */
    cancel() {
        if (this.#handle === null) {
            return;
        }

        cancelAnimationFrame(this.#handle);
        this.#handle = null;
    }
}

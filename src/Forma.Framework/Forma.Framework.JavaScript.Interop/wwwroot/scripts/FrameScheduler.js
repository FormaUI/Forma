/**
 * FrameScheduler — one piece of work held to the next animation frame
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

    /** Drops the work waiting for the next frame. */
    cancel() {
        if (this.#handle === null) {
            return;
        }

        cancelAnimationFrame(this.#handle);
        this.#handle = null;
    }
}

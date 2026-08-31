/**
 * ScrollBoundaryWatcher — reporting how much of a scrolling track is still out of view
 *
 * How much remains is a measurement only the browser holds, so it is reported back rather
 * than derived in C#. Offsets are read through Math.abs because a right-to-left track
 * counts scrollLeft downwards from zero.
 */

/**
 * @param {HTMLElement} target
 * @param {{ owner: { invokeMethodAsync: (name: string, ...args: unknown[]) => Promise<unknown> } }} options
 */
export function attach(target, options) {
    return new ScrollBoundaryWatcher(target, options);
}

function measure(track) {
    const offset = Math.abs(track.scrollLeft);
    const slack = track.scrollWidth - track.clientWidth;

    // A sub-pixel track width leaves a fractional remainder that would otherwise read as scrollable.
    return {
        canScrollBack: slack > 1 && offset > 1,
        canScrollForward: slack > 1 && offset < slack - 1,
    };
}

class ScrollBoundaryWatcher {
    #track;
    #owner;
    #controller = new AbortController();
    #resize;
    #mutations;
    #watched = [];
    #last = null;
    #isQueued = false;
    #frame = null;
    #isDisposed = false;

    constructor(track, { owner }) {
        this.#track = track;
        this.#owner = owner;

        track.addEventListener('scroll', () => this.#schedule(), { signal: this.#controller.signal, passive: true });

        this.#resize = new ResizeObserver(() => this.#schedule());
        this.#resize.observe(track);
        this.#watchChildren();

        // Commands can be added or removed without the track's own box changing, which no ResizeObserver reports.
        this.#mutations = new MutationObserver(() => {
            this.#watchChildren();
            this.#schedule();
        });

        this.#mutations.observe(track, { childList: true });
        this.#schedule();
    }

    dispose() {
        this.#isDisposed = true;

        if (this.#frame !== null) {
            cancelAnimationFrame(this.#frame);
            this.#frame = null;
        }

        this.#controller.abort();
        this.#resize.disconnect();
        this.#mutations.disconnect();
    }

    // A command resizing changes what fits, so each is watched alongside the track.
    #watchChildren() {
        for (const child of this.#watched) {
            this.#resize.unobserve(child);
        }

        this.#watched = [...this.#track.children];

        for (const child of this.#watched) {
            this.#resize.observe(child);
        }
    }

    // Scrolling fires far faster than the circuit can answer, so reports are coalesced to one a frame.
    #schedule() {
        if (this.#isQueued || this.#isDisposed) {
            return;
        }

        this.#isQueued = true;
        this.#frame = requestAnimationFrame(() => this.#report());
    }

    #report() {
        this.#isQueued = false;
        this.#frame = null;

        if (this.#isDisposed) {
            return;
        }

        const next = measure(this.#track);

        if (this.#last
            && this.#last.canScrollBack === next.canScrollBack
            && this.#last.canScrollForward === next.canScrollForward) {
            return;
        }

        this.#last = next;
        this.#owner.invokeMethodAsync('HandleOverflowAsync', next.canScrollBack, next.canScrollForward);
    }
}

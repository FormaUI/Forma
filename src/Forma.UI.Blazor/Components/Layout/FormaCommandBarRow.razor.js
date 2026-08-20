/**
 * FormaCommandBarRow — overflow scrolling
 *
 * How much of a row is still out of view is a measurement only the browser holds, so the
 * arrows' enabled state is reported back from here rather than derived in C#. Scroll
 * offsets are read through Math.abs because a right-to-left row counts scrollLeft
 * downwards from zero.
 */

const observed = new WeakMap();

/** @param {HTMLElement} track */
function measure(track) {
    const offset = Math.abs(track.scrollLeft);
    const slack = track.scrollWidth - track.clientWidth;

    // A sub-pixel track width leaves a fractional remainder that would otherwise read as scrollable.
    return {
        canScrollBack: slack > 1 && offset > 1,
        canScrollForward: slack > 1 && offset < slack - 1
    };
}

/**
 * @param {HTMLElement} track
 * @param {{ invokeMethodAsync: (name: string, ...args: unknown[]) => Promise<unknown> }} owner
 */
export function observe(track, owner) {
    unobserve(track);

    const controller = new AbortController();
    let last = null;
    let queued = false;

    const report = () => {
        queued = false;

        const next = measure(track);

        if (last && last.canScrollBack === next.canScrollBack && last.canScrollForward === next.canScrollForward) {
            return;
        }

        last = next;
        owner.invokeMethodAsync('ReportOverflowAsync', next.canScrollBack, next.canScrollForward);
    };

    // Scrolling fires far faster than the circuit can answer, so reports are coalesced to one a frame.
    const schedule = () => {
        if (!queued) {
            queued = true;
            requestAnimationFrame(report);
        }
    };

    track.addEventListener('scroll', schedule, { signal: controller.signal, passive: true });

    const resize = new ResizeObserver(schedule);
    resize.observe(track);

    // A command resizing changes what fits, so each is watched alongside the track.
    let watched = [];
    const watchChildren = () => {
        for (const child of watched) {
            resize.unobserve(child);
        }

        watched = [...track.children];

        for (const child of watched) {
            resize.observe(child);
        }
    };

    watchChildren();

    // Commands can be added or removed without the track's own box changing, which no ResizeObserver reports.
    const mutations = new MutationObserver(() => {
        watchChildren();
        schedule();
    });

    mutations.observe(track, { childList: true });

    observed.set(track, { controller, resize, mutations });
    schedule();
}

/** @param {HTMLElement} track */
export function unobserve(track) {
    const entry = observed.get(track);

    if (entry) {
        entry.controller.abort();
        entry.resize.disconnect();
        entry.mutations.disconnect();
        observed.delete(track);
    }
}

/**
 * @param {HTMLElement} track
 * @param {number} direction -1 toward the start of the row, 1 toward its end.
 */
export function scrollByPage(track, direction) {
    const isRightToLeft = getComputedStyle(track).direction === 'rtl';
    const step = track.clientWidth * 0.8 * direction;

    track.scrollBy({ left: isRightToLeft ? -step : step, behavior: 'smooth' });
}

/**
 * FormaDataGrid — row selection interop
 *
 * QuickGrid's OnRowClick carries the row item but no MouseEventArgs, so modifier keys
 * (ctrl/meta for toggle, shift for range) can only be read here, from the DOM event
 * that a row's own click handling never surfaces to Blazor. Each row is identified by
 * a data-row-key attribute already rendered by the component, so no DOM index or sort
 * order needs to be reconstructed on this side.
 */

/** Elements that own their own clicks and keys, so a row never consumes them. */
const INTERACTIVE = 'a[href], button, input, select, textarea, label, [role="button"], [role="link"], [contenteditable="true"]';

/** Fields where the arrow, home and end keys move a caret rather than the focused row. */
const TEXT_ENTRY = 'select, textarea, [contenteditable="true"], input:not([type="checkbox"]):not([type="radio"]):not([type="button"]):not([type="submit"])';

/**
 * @param {HTMLElement} scroller
 * @param {DotNet.DotNetObject} dotNetRef
 */
export function attachRowSelection(scroller, dotNetRef) {
    let anchor = null;

    function markers() {
        return Array.from(scroller.querySelectorAll('[data-row-key]'));
    }

    function tokenOf(marker) {
        return Number(marker.dataset.rowKey);
    }

    function rangeTokens(from, to) {
        const all = markers();
        const fromIndex = all.indexOf(from);
        const toIndex = all.indexOf(to);
        if (fromIndex === -1 || toIndex === -1) {
            return [tokenOf(to)];
        }

        const [start, end] = fromIndex <= toIndex ? [fromIndex, toIndex] : [toIndex, fromIndex];
        return all.slice(start, end + 1).map(tokenOf);
    }

    function focusMarker(marker) {
        for (const candidate of markers()) {
            candidate.tabIndex = candidate === marker ? 0 : -1;
        }

        marker.focus();
    }

    // Shift-click extends a native text selection on mousedown, before the click handler
    // below runs; its guards are mirrored so only a row-selecting click loses its default.
    function handleMouseDown(event) {
        if (event.button !== 0 || !event.shiftKey || event.altKey) {
            return;
        }

        if (event.target.closest(INTERACTIVE)) {
            return;
        }

        if (!event.target.closest('tr')?.querySelector('[data-row-key]')) {
            return;
        }

        event.preventDefault();
    }

    function handleClick(event) {
        if (event.altKey) {
            return;
        }

        // The row checkbox, an action icon or a link inside a cell is the click's real
        // target; consuming it here would leave that element dead.
        if (event.target.closest(INTERACTIVE)) {
            return;
        }

        const row = event.target.closest('tr');
        const marker = row?.querySelector('[data-row-key]');
        if (!marker) {
            return;
        }

        event.stopPropagation();

        const toggle = event.ctrlKey || event.metaKey;
        const range = event.shiftKey;

        if (range && anchor) {
            void dotNetRef.invokeMethodAsync('HandleRowRangeSelectAsync', rangeTokens(anchor, marker), toggle);
            return;
        }

        anchor = marker;
        void dotNetRef.invokeMethodAsync('HandleRowSelectAsync', tokenOf(marker), toggle);
    }

    function handleKeydown(event) {
        if (!['ArrowUp', 'ArrowDown', 'Home', 'End'].includes(event.key)) {
            return;
        }

        if (event.target.closest(TEXT_ENTRY)) {
            return;
        }

        const all = markers();
        if (all.length === 0) {
            return;
        }

        // Focus is the source of truth for "which row", not the roving tabindex: tabbing
        // straight onto a row sets the former and leaves the latter wherever it was.
        const focused = document.activeElement?.closest('[data-row-key]');
        const currentIndex = focused ? all.indexOf(focused) : -1;
        let nextIndex;

        if (event.key === 'Home') {
            nextIndex = 0;
        } else if (event.key === 'End') {
            nextIndex = all.length - 1;
        } else if (currentIndex === -1) {
            nextIndex = event.key === 'ArrowDown' ? 0 : all.length - 1;
        } else if (event.key === 'ArrowUp') {
            nextIndex = Math.max(0, currentIndex - 1);
        } else {
            nextIndex = Math.min(all.length - 1, currentIndex + 1);
        }

        const next = all[nextIndex];
        if (nextIndex === currentIndex && event.key !== 'Home' && event.key !== 'End') {
            return;
        }

        event.preventDefault();
        focusMarker(next);

        if (event.ctrlKey || event.metaKey) {
            return;
        }

        if (event.shiftKey && anchor) {
            void dotNetRef.invokeMethodAsync('HandleRowRangeSelectAsync', rangeTokens(anchor, next), false);
            return;
        }

        anchor = next;
        void dotNetRef.invokeMethodAsync('HandleRowSelectAsync', tokenOf(next), false);
    }

    scroller.addEventListener('mousedown', handleMouseDown, true);
    scroller.addEventListener('click', handleClick, true);
    scroller.addEventListener('keydown', handleKeydown, true);

    scroller.formaDetachRowSelection = () => {
        scroller.removeEventListener('mousedown', handleMouseDown, true);
        scroller.removeEventListener('click', handleClick, true);
        scroller.removeEventListener('keydown', handleKeydown, true);
        delete scroller.formaDetachRowSelection;
    };
}

/**
 * @param {HTMLElement} scroller
 */
export function detachRowSelection(scroller) {
    scroller.formaDetachRowSelection?.();
}

/**
 * FormaDataGrid — page height reservation
 *
 * A page holding fewer rows than the page size leaves the grid shorter than a full one,
 * so the page navigation jumps up under the pointer between clicks. How tall a row stands
 * follows from whatever the host puts in its cells, which is only known once laid out,
 * so the height to hold is measured here rather than computed from a declared row size.
 *
 * Fractional box heights throughout: a row rounded to the whole pixel carries that error
 * once per row of the page, which is enough to shift the footer on its own.
 *
 * @param {HTMLElement} scroller
 * @param {number} pageSize
 */
export function reservePageHeight(scroller, pageSize) {
    const table = scroller.querySelector('table');
    const body = table?.tBodies[0];
    if (!body) {
        return;
    }

    // QuickGrid already pads a short page out to the page size, but with empty rows that
    // carry none of the content a row draws its height from, so they are no sample.
    const filled = [...body.rows].filter(row => row.textContent.trim() !== '');
    if (!filled.length) {
        return;
    }

    // A full page states the height outright, and is worth keeping: rows of unequal
    // height make any projection from a short page an approximation of it.
    if (filled.length >= pageSize) {
        scroller.formaReservedHeight = table.getBoundingClientRect().height;
    } else if (scroller.formaReservedHeight === undefined) {
        const header = table.tHead?.getBoundingClientRect().height ?? 0;
        const tallest = Math.max(...filled.map(row => row.getBoundingClientRect().height));
        scroller.formaReservedHeight = header + tallest * pageSize;
    }

    scroller.style.minBlockSize = `${scroller.formaReservedHeight}px`;
}


/**
 * FormaDataGrid — select-all checkbox interop
 *
 * "Some but not all rows selected" is an IDL-only property on the element, with no
 * content attribute Blazor could render, so the tri-state header checkbox has to be
 * set from script.
 */

/**
 * @param {HTMLInputElement} checkbox
 * @param {boolean} indeterminate
 */
export function setIndeterminate(checkbox, indeterminate) {
    checkbox.indeterminate = indeterminate;
}

/**
 * FormaDataGrid — row selection interop
 *
 * QuickGrid's OnRowClick carries the row item but no MouseEventArgs, so modifier keys
 * (ctrl/meta for toggle, shift for range) can only be read here, from the DOM event
 * that a row's own click handling never surfaces to Blazor. Each row is identified by
 * a data-row-key attribute already rendered by the component, so no DOM index or sort
 * order needs to be reconstructed on this side.
 */

/** Controls that own their own clicks and keys, so a row never consumes them. */
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

    function handleClick(event) {
        if (event.altKey) {
            return;
        }

        // The row checkbox, an action icon or a link inside a cell is the click's real
        // target; consuming it here would leave that control dead.
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

    scroller.addEventListener('click', handleClick, true);
    scroller.addEventListener('keydown', handleKeydown, true);

    scroller.formaDetachRowSelection = () => {
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


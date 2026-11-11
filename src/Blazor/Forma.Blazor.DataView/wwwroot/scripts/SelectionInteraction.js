/**
 * SelectionInteraction — click and keyboard selection across a list of keyed items
 *
 * The modifier keys are read from the DOM event: Ctrl or Meta toggles, Shift extends a range.
 * Each item is identified by the key attribute the component renders on its marker. The keys
 * are routes on the input router, declared on the list, so a key inside a nested scope, a
 * select open in a cell, is that scope's, and a key in a field inside an item is the field's.
 *
 * The arrows move by where the items stand: down and up to the item below or above in the same
 * column, right and left to the neighbor in the same row, so one behavior serves rows down a
 * table, names flowing on into further columns, and cards packed across the width. Items laid
 * out nowhere yet, or all in one place, stand in one column in their document order.
 *
 * Home and End reach a row's ends where the item has neighbors across, and the list's ends where it
 * stands alone or the primary modifier is held. Where the owner holds more items than the page draws,
 * a move to the list's ends is asked of the owner, which scrolls the item into range and names its
 * position once it is drawn; the focus lands on it then, unless the reader moved on meanwhile. Each
 * move keeps its rules on the way: plain selects the item reached, the primary modifier moves the
 * focus alone, and Shift extends the range from the anchor, by position where the owner seeks, so an
 * anchor scrolled out of the page still counts. Where the list moves without selecting, its markers
 * being boxes the reader checks with Space, every move takes the focus alone.
 *
 * Wherever the keys move, the markers stand as one tab stop: the marker last focused, or the first
 * drawn until one is, so Tab enters the list once and leaves it at once. A render that draws the
 * focused item's element afresh drops the focus to nothing; with neither a press nor Tab to explain it, the focus goes back to the item's
 * marker, so Tab still leaves the list, and the page, from its last item. A press or a Tab that a host
 * handles while keeping the focus where it was explains nothing once it has ended.
 *
 * Where the list gathers its items' controls, Tab reaches the controls of the active items alone: the
 * item holding a collection's tab stop, one per collection the list holds, a tree beside a grid, and
 * the item holding the focus, every other item's controls standing out of the Tab sequence until it is
 * active, so Tab enters a collection once and leaves it after one item's controls. A press on another
 * item's control makes that item active. Shift+F10 and the Menu key on an item, its marker or itself,
 * press the control the item names as the opener of its actions, and the focus comes back to where the
 * key was pressed once the actions hand it back to their opener.
 *
 * Enter on an item's marker activates the item, a route like the moves, so the form a host wraps
 * the list in never takes it as a submission. The presses are routes too: where the list selects,
 * a press selects, a shift-press stops the browser extending its own text selection across the
 * items, and a double press activates; where it does not, a press activates, or checks the item's
 * box where the list checks on a press, as a selection mode does, the owner activating instead an
 * item that cannot be selected, so a reader can still open it. A press on a native control
 * inside an item is the control's, as is one on a part of the item a caller names as its own
 * controls, a box's cell or a card's actions.
 *
 * An item that draws no marker of its own can be its own marker, so a list with no selection still
 * stands as one tab stop the keys move through. Where the marker holding the focus stops being one
 * while its item stays, as an item does when the box drawn inside it takes over, the focus moves to
 * the marker the item draws now.
 */

import { Controls } from '../../forma/core/scripts/Controls.js';
import { tell } from '../../forma/core/scripts/Owner.js';
import { Transfer } from '../../forma/core/scripts/Transfer.js';
import { register } from '../../forma/input/scripts/InputRouter.js';

/** Where each key sends the focused item: a step in a direction, or to either end of its row or the list. */
const Moves = [
    { action: 'selection.next', key: 'ArrowDown', target: (layout, index) => layout.below(index) },
    { action: 'selection.previous', key: 'ArrowUp', target: (layout, index) => layout.above(index) },
    { action: 'selection.right', key: 'ArrowRight', target: (layout, index) => layout.beside(index, 1) },
    { action: 'selection.left', key: 'ArrowLeft', target: (layout, index) => layout.beside(index, -1) },
    { action: 'selection.first', key: 'Home', edge: -1, target: (layout, index, whole) => layout.end(index, -1, whole) },
    { action: 'selection.last', key: 'End', edge: 1, target: (layout, index, whole) => layout.end(index, 1, whole) },
];

/** How long a move asked of the owner waits for its item to be drawn before it gives up the focus it would have taken. */
const SeekPatience = 3000;

/**
 * @param {HTMLElement} target
 * @param {{
 *   owner: import('../../forma/core/scripts/Owner.js').Owner,
 *   keyAttribute?: string,
 *   itemSelector?: string,
 *   itemKeyAttribute?: string | null,
 *   controlSelector?: string | null,
 *   selects?: boolean,
 *   moves?: boolean,
 *   checksOnPress?: boolean,
 *   positionAttribute?: string | null,
 *   seeks?: boolean,
 *   itemRootSelector?: string | null,
 *   actionsSelector?: string | null,
 *   gathersControls?: boolean
 * }} options
 */
export function attach(target, options) {
    return new SelectionInteraction(target, options);
}

/** Where the items stand, read once per key press: each item's box, and the neighbors each direction reaches. */
class Layout {
    #boxes;
    #stacked;
    #rightToLeft;

    constructor(items, rightToLeft) {
        this.#boxes = items.map(item => item.getBoundingClientRect());
        this.#rightToLeft = rightToLeft;

        // Boxes that are all one box carry no layout, so the items stand in one column in their order.
        const first = this.#boxes[0];
        this.#stacked = this.#boxes.every(box => box.top === first.top && box.left === first.left);
    }

    get count() {
        return this.#boxes.length;
    }

    below(index) {
        return index < 0 ? 0 : this.#stacked ? Math.min(this.count - 1, index + 1) : this.#nearest(index, box => this.#sameColumn(index, box) && box.top > this.#boxes[index].top, box => box.top);
    }

    above(index) {
        return index < 0 ? this.count - 1 : this.#stacked ? Math.max(0, index - 1) : this.#nearest(index, box => this.#sameColumn(index, box) && box.top < this.#boxes[index].top, box => -box.top);
    }

    beside(index, direction) {
        if (index < 0 || this.#stacked) {
            return index < 0 ? 0 : index;
        }

        const left = this.#boxes[index].left;

        return this.#nearest(index, box => this.#sameRow(index, box) && Math.sign(box.left - left) === direction, box => direction * box.left);
    }

    /** Whether the item has no neighbors across, so its row's ends are the list's. */
    standsAlone(index) {
        return this.#rowmates(index).length === 0;
    }

    // A row's own end where the item has neighbors in its row, else the list's; the list's whenever the whole list is asked for.
    end(index, direction, whole = false) {
        const rowmates = whole ? [] : this.#rowmates(index);

        if (rowmates.length === 0) {
            return direction < 0 ? 0 : this.count - 1;
        }

        const row = [index, ...rowmates];
        const toStart = direction < 0 ? !this.#rightToLeft : this.#rightToLeft;

        return row.reduce((best, candidate) => (toStart ? this.#boxes[candidate].left < this.#boxes[best].left : this.#boxes[candidate].left > this.#boxes[best].left) ? candidate : best, index);
    }

    #rowmates(index) {
        return index < 0 || this.#stacked ? [] : this.#boxes.map((_, candidate) => candidate).filter(candidate => candidate !== index && this.#sameRow(index, this.#boxes[candidate]));
    }

    #sameColumn(index, box) {
        return Math.abs(box.left - this.#boxes[index].left) < Math.max(1, this.#boxes[index].width / 2);
    }

    #sameRow(index, box) {
        return Math.abs(box.top - this.#boxes[index].top) < Math.max(1, this.#boxes[index].height / 2);
    }

    // The item nearest along the measure among those the direction admits, or the item itself where none does.
    #nearest(index, admits, measure) {
        let best = index;
        let bestMeasure = Infinity;

        this.#boxes.forEach((box, candidate) => {
            if (candidate === index || !admits(box)) {
                return;
            }

            const value = measure(box);

            if (value < bestMeasure) {
                bestMeasure = value;
                best = candidate;
            }
        });

        return best;
    }
}

class SelectionInteraction {
    #target;
    #owner;
    #keyAttribute;
    #markerSelector;
    #itemSelector;
    #itemKeyAttribute;
    #controlSelector;
    #positionAttribute;
    #selects;
    #checksOnPress;
    #seeks;
    #registration;
    #tabStop = null;
    #anchor = null;
    #anchorPosition = null;
    #seeking = null;
    #lastRequest = 0;
    #waiting = null;
    #listening = new AbortController();
    #held = null;
    #readerMoved = false;
    #moveStart = null;
    #itemRootSelector;
    #actionsSelector;
    #gathersControls;
    #gathered = new Map();
    #indexed = new Map();
    #actionsReturn = null;
    #suppressesContextMenu = false;

    constructor(target, { owner, keyAttribute = 'data-row-key', itemSelector = 'tr', itemKeyAttribute = null, controlSelector = null, selects = true, moves = selects, checksOnPress = false, positionAttribute = null, seeks = false, itemRootSelector = null, actionsSelector = null, gathersControls = false }) {
        this.#target = target;
        this.#owner = owner;
        this.#keyAttribute = keyAttribute;
        this.#markerSelector = `[${keyAttribute}]`;
        this.#itemSelector = itemSelector;
        this.#itemKeyAttribute = itemKeyAttribute;
        this.#controlSelector = controlSelector;
        this.#positionAttribute = positionAttribute;
        this.#selects = selects;
        this.#checksOnPress = checksOnPress && !selects;
        this.#seeks = seeks && positionAttribute !== null;
        this.#itemRootSelector = itemRootSelector;
        this.#actionsSelector = itemRootSelector === null ? null : actionsSelector;
        this.#gathersControls = gathersControls && itemRootSelector !== null;

        // Each key moves plain, with Shift extending the range, or with the primary modifier moving alone; one that
        // would move nothing matches nothing, so a press at the end of the list keeps its default.
        const routes = moves
            ? Moves.map(move => ({
                action: move.action,
                bindings: [
                    { type: 'keydown', key: move.key, when: event => this.#planMove(event, move) !== null },
                    { type: 'keydown', key: move.key, modifiers: { primary: true }, when: event => this.#planMove(event, move) !== null },
                ],
                handle: event => this.#move(event, move),
            }))
            : [];

        // The shift-press is taken at press time and loses its default there, before the browser extends its own text
        // selection across the items; the click that selects keeps its default.
        const selection = selects
            ? [{
                action: 'selection.select',
                bindings: [
                    { type: 'click', button: 0, when: event => this.#pressedMarker(event) !== null },
                    { type: 'pointerdown', button: 0, modifiers: { shift: true }, when: event => this.#pressedMarker(event) !== null },
                ],
                handle: event => {
                    if (event.type === 'click') {
                        this.#select(event);
                    } else {
                        event.preventDefault();
                    }
                },
            }]
            : [];

        const activation = {
            action: 'selection.activate',
            bindings: [
                { type: 'keydown', key: 'Enter', when: event => this.#focusedMarker(event) !== null },
                { type: selects ? 'dblclick' : 'click', button: 0, when: event => this.#pressedItem(event) !== null },
            ],
            handle: event => event.type === 'click' && this.#checksOnPress ? this.#check(event) : this.#activate(event),
        };

        // The menu keys on an item press the opener of its actions; a key pressed in a control inside the item is the control's own.
        const actions = this.#actionsSelector
            ? [{
                action: 'selection.actions',
                bindings: [
                    { type: 'keydown', key: 'F10', modifiers: { shift: true }, when: event => this.#actionsOpener(event) !== null },
                    { type: 'keydown', key: 'ContextMenu', when: event => this.#actionsOpener(event) !== null },
                ],
                handle: event => this.#openActions(event),
            }]
            : [];

        this.#registration = register(target, { routes: [...routes, ...selection, activation, ...actions] });

        // A render may draw the focused item's element afresh, which drops the focus to nothing; with neither a press nor Tab to
        // explain the loss, the focus goes back to the item's marker once the render has drawn it again. Both are observed rather
        // than routed, so whichever scope owns them still does, a surface keeping Tab inside itself above all. Each ends where
        // its focus has finished moving: a Tab at its release, a press at its click, where a touch tap's late compatibility press
        // has moved the focus, or at its release where it raises no click, and a press also at its cancellation.
        const signal = this.#listening.signal;
        const ownerDocument = target.ownerDocument;
        target.addEventListener('focusin', event => this.#hold(this.#focusedMarker(event)), { signal });

        // Wherever the keys move, a marker taking the focus becomes the tab stop, and every marker a render draws joins the rest.
        if (moves) {
            target.addEventListener('focusin', event => {
                const marker = this.#focusedMarker(event);

                if (marker) {
                    this.#tabStop = marker;
                    this.#settleTabStop();
                }
            }, { signal });

            const drawing = new MutationObserver(() => {
                this.#followFocus();
                this.#settleTabStop();
            });
            drawing.observe(target, { childList: true, subtree: true });
            signal.addEventListener('abort', () => drawing.disconnect());
            this.#settleTabStop();
        }

        ownerDocument.addEventListener('pointerdown', () => this.#beginMove(), { capture: true, signal });
        ownerDocument.addEventListener('pointerup', event => {
            if (event.button !== 0) {
                this.#endMove();
            }
        }, { capture: true, signal });
        ownerDocument.addEventListener('pointercancel', () => this.#endMove(), { capture: true, signal });
        ownerDocument.addEventListener('click', event => {
            if (event.detail > 0) {
                this.#endMove();
            }
        }, { capture: true, signal });
        ownerDocument.addEventListener('keydown', event => {
            if (event.key === 'Tab') {
                this.#beginMove();
            }
        }, { capture: true, signal });
        ownerDocument.addEventListener('keyup', event => {
            if (event.key === 'Tab') {
                this.#endMove();
            }
        }, { capture: true, signal });
        target.addEventListener('focusout', event => {
            const held = this.#held;

            if (event.relatedTarget === null && held && !this.#readerMoved) {
                requestAnimationFrame(() => this.#restore(held));
            }
        }, { signal });

        if (this.#actionsSelector) {
            target.addEventListener('focusin', event => this.#returnFromActions(event), { signal });

            // The browser raises its own menu for the keys as well, which the actions opened in its place stand for; a press
            // or another key is the reader's next move, whose own menu is the browser's again.
            ownerDocument.addEventListener('contextmenu', event => {
                if (this.#suppressesContextMenu) {
                    this.#suppressesContextMenu = false;
                    event.preventDefault();
                    event.stopPropagation();
                }
            }, { capture: true, signal });
            ownerDocument.addEventListener('pointerdown', () => this.#suppressesContextMenu = false, { capture: true, signal });
            ownerDocument.addEventListener('keydown', event => {
                if (event.key !== 'F10' && event.key !== 'ContextMenu') {
                    this.#suppressesContextMenu = false;
                }
            }, { capture: true, signal });
        }

        // Every render may draw items, move the tab stop or enable a control, so the controls Tab reaches are settled again after each,
        // and as the focus moves; the tab indexes the gathering itself wrote settle nothing new.
        if (this.#gathersControls) {
            target.addEventListener('focusin', event => this.#focusItemOf(event), { signal });

            const gathering = new MutationObserver(records => {
                if (!records.every(record => record.type === 'attributes' && this.#gathered.has(record.target))) {
                    this.#gatherControls();
                }
            });
            gathering.observe(target, { childList: true, subtree: true, attributes: true, attributeFilter: ['tabindex', 'disabled'] });
            signal.addEventListener('abort', () => gathering.disconnect());
            this.#gatherControls();
        }
    }

    dispose() {
        this.#cancelSeek();
        this.#listening.abort();
        this.#registration.dispose();
        this.#releaseControls();
        this.#releaseMarkers([]);
    }

    /**
     * Lands the focus on the item a move asked of the owner reached, once the item at the position is drawn;
     * a request the reader has since moved on from, or one a newer request replaced, is let go.
     * @param {number} request The request the owner answers.
     * @param {number} position The position of the item reached.
     */
    focusItem(request, position) {
        if (this.#seeking?.request !== request) {
            return;
        }

        const { mode } = this.#seeking;
        this.#seeking = null;
        this.#whenDrawn(() => this.#markerAt(position), marker => {
            this.#focusMarker(marker, this.#markers());

            if (mode === 'select') {
                this.#setAnchor(marker);
            }
        });
    }

    /** Lets go of a move asked of the owner, which the owner can no longer answer. */
    cancelSeek() {
        this.#cancelSeek();
    }

    /**
     * Has a press check the box of the item it lands on, or activate the item again, keeping the focus and the tab stop as they are.
     * @param {boolean} checks Whether a press checks.
     */
    setChecksOnPress(checks) {
        this.#checksOnPress = checks && !this.#selects;
    }

    #markers() {
        return [...this.#target.querySelectorAll(this.#markerSelector)];
    }

    #tokenOf(marker) {
        return Number(marker.getAttribute(this.#keyAttribute));
    }

    /** The marker the key was pressed on, when it is one of this list's; a key in a control inside an item is the control's. */
    #focusedMarker(event) {
        const { target } = event;

        return target instanceof Element && target.matches(this.#markerSelector) && this.#target.contains(target) ? target : null;
    }

    /** The marker of the item a selecting press lands on; a press on a native control or with Alt held selects nothing. */
    #pressedMarker(event) {
        const { target } = event;

        if (event.altKey || !(target instanceof Element) || target.closest(Controls.Interactive)) {
            return null;
        }

        return this.#markerIn(target);
    }

    /** The item an activating press lands on, when it carries a token; a press on a native control or on the item's own controls activates nothing. */
    #pressedItem(event) {
        const { target } = event;

        if (!(target instanceof Element) || target.closest(Controls.Interactive) || (this.#controlSelector && target.closest(this.#controlSelector))) {
            return null;
        }

        const item = target.closest(this.#itemSelector);

        return item && this.#target.contains(item) && this.#tokenOfItem(item) !== null ? item : null;
    }

    // An item's token comes from its own attribute where the list names one, else from its marker.
    #tokenOfItem(item) {
        const value = this.#itemKeyAttribute
            ? item.getAttribute(this.#itemKeyAttribute)
            : this.#ownMarker(item)?.getAttribute(this.#keyAttribute);

        return value == null || value === '' ? null : Number(value);
    }

    #activate(event) {
        const token = event.type === 'keydown' ? this.#tokenOf(this.#focusedMarker(event)) : this.#tokenOfItem(this.#pressedItem(event));
        tell(this.#owner, 'HandleActivateAsync', token);
    }

    // A press checks or unchecks the box the item it lands on draws, which the owner does where the item can be selected and activates
    // it otherwise, the focus landing on the box so the keys carry on from the item the pointer chose; an item that draws no box is
    // activated.
    #check(event) {
        const item = this.#pressedItem(event);
        const box = this.#markerIn(item);

        if (!box || box === item) {
            this.#activate(event);
            return;
        }

        this.#focusMarker(box, this.#markers());
        tell(this.#owner, 'HandleCheckAsync', this.#tokenOfItem(item));
    }

    // The marker holding the focus can stop being one while its item stays; the focus moves to the marker the item draws now.
    #followFocus() {
        const held = this.#tabStop;

        if (!held?.isConnected || held.matches(this.#markerSelector) || held !== this.#target.ownerDocument.activeElement) {
            return;
        }

        const marker = this.#markerIn(held);

        if (marker) {
            this.#focusMarker(marker, this.#markers());
        }
    }

    #markerIn(node) {
        const item = node?.closest(this.#itemSelector);

        return item ? this.#ownMarker(item) : null;
    }

    // An item's marker is the one drawn inside it, or the item itself where it is its own.
    #ownMarker(item) {
        return item.matches(this.#markerSelector) ? item : item.querySelector(this.#markerSelector);
    }

    #itemOf(marker) {
        return marker.closest(this.#itemSelector) ?? marker;
    }

    #rangeTokens(from, to) {
        const all = this.#markers();
        const fromIndex = all.indexOf(from);
        const toIndex = all.indexOf(to);

        if (fromIndex === -1 || toIndex === -1) {
            return [this.#tokenOf(to)];
        }

        // From the anchor to the item reached, which a selection of one item at most chooses alone.
        const tokens = all.slice(Math.min(fromIndex, toIndex), Math.max(fromIndex, toIndex) + 1).map(marker => this.#tokenOf(marker));

        return fromIndex <= toIndex ? tokens : tokens.reverse();
    }

    #focusMarker(marker, all) {
        this.#tabStop = marker;
        this.#releaseMarkers(all);

        for (const candidate of all) {
            this.#setMarkerTabIndex(candidate, candidate === marker ? 0 : -1);
        }

        marker.focus();
    }

    // The marker last focused stays the one tab stop while it is drawn, the marker its item draws now where it stopped being one, and
    // the first drawn marker stands in for it otherwise.
    #settleTabStop() {
        const all = this.#markers();
        const last = this.#tabStop?.isConnected && this.#target.contains(this.#tabStop) ? this.#tabStop : null;
        const stop = (last && (all.includes(last) ? last : this.#markerIn(last))) ?? all[0];
        this.#releaseMarkers(all);

        for (const marker of all) {
            this.#setMarkerTabIndex(marker, marker === stop ? 0 : -1);
        }
    }

    // A marker's place in the Tab sequence, written on it so an element that takes no focus of its own takes a press's, the tab index it
    // had kept to return to once it stops being a marker.
    #setMarkerTabIndex(marker, tabIndex) {
        if (!this.#indexed.has(marker)) {
            this.#indexed.set(marker, marker.getAttribute('tabindex'));
        }

        if (marker.getAttribute('tabindex') !== `${tabIndex}`) {
            marker.tabIndex = tabIndex;
        }
    }

    // An element no longer among the markers, an item that drew a box in its place above all, takes back the tab index it had, so it
    // leaves the Tab sequence it stood in as a marker.
    #releaseMarkers(all) {
        const markers = new Set(all);

        for (const [element, tabIndex] of this.#indexed) {
            if (!element.isConnected) {
                this.#indexed.delete(element);
            } else if (!markers.has(element)) {
                this.#indexed.delete(element);
                this.#restoreTabIndex(element, tabIndex);
            }
        }
    }

    /** The opener of the actions of the item the key was pressed on, its marker or itself; a key in a control inside the item is the control's. */
    #actionsOpener(event) {
        const { target } = event;

        if (!(target instanceof Element) || !this.#target.contains(target)) {
            return null;
        }

        const root = target.closest(this.#itemRootSelector);

        if (!root || !this.#target.contains(root) || (target !== root && !target.matches(this.#markerSelector))) {
            return null;
        }

        const opener = root.querySelector(this.#actionsSelector);

        return opener && !opener.matches(':disabled, [aria-disabled="true"]') ? opener : null;
    }

    #openActions(event) {
        const opener = this.#actionsOpener(event);

        if (!opener) {
            return;
        }

        event.preventDefault();
        this.#actionsReturn = { opener, origin: event.target };
        this.#suppressesContextMenu = true;
        opener.click();
    }

    // The actions hand the focus back to their opener as they close, which passes it on to where the key was pressed; the focus
    // reaching a marker or another item first means the reader went elsewhere.
    #returnFromActions(event) {
        const pending = this.#actionsReturn;
        const { target } = event;

        if (!pending || !(target instanceof Element)) {
            return;
        }

        if (target === pending.opener) {
            this.#actionsReturn = null;

            if (pending.origin.isConnected) {
                pending.origin.focus();
            }

            return;
        }

        const root = target.closest(this.#itemRootSelector);

        if (target.matches(this.#markerSelector) || (root && root !== pending.opener.closest(this.#itemRootSelector))) {
            this.#actionsReturn = null;
        }
    }

    // A control of another item taking the focus makes that item the one Tab reaches the controls of, its marker the tab stop.
    #focusItemOf(event) {
        const { target } = event;
        const root = target instanceof Element ? target.closest(this.#itemRootSelector) : null;
        const marker = root && this.#target.contains(root) ? this.#ownMarker(root) : null;

        if (marker && marker !== this.#tabStop && !target.matches(this.#markerSelector)) {
            this.#tabStop = marker;
            this.#settleTabStop();
        }

        this.#gatherControls();
    }

    /** The items whose controls Tab reaches: each holding a collection's tab stop, by its marker or itself, and the one holding the focus. */
    #activeRoots(roots) {
        const active = new Set(roots.filter(root => root.getAttribute('tabindex') === '0' || root.querySelector(`${this.#markerSelector}[tabindex="0"]`) !== null));
        const focused = this.#target.ownerDocument.activeElement;
        const focusedRoot = focused instanceof Element && this.#target.contains(focused) ? focused.closest(this.#itemRootSelector) : null;

        if (focusedRoot) {
            active.add(focusedRoot);
        }

        return active;
    }

    // Every item's controls but the active ones' leave the Tab sequence, each keeping the tab index it had to return to; while no item
    // holds a tab stop, Tab has no item to enter by, so every item's controls stay in the sequence.
    #gatherControls() {
        const roots = [...this.#target.querySelectorAll(this.#itemRootSelector)];
        const active = this.#activeRoots(roots);

        for (const [control, tabIndex] of this.#gathered) {
            const root = control.isConnected ? control.closest(this.#itemRootSelector) : null;

            if (!control.isConnected) {
                this.#gathered.delete(control);
            } else if (active.size === 0 || root === null || active.has(root)) {
                this.#gathered.delete(control);
                this.#restoreTabIndex(control, tabIndex);
            }
        }

        if (active.size === 0) {
            return;
        }

        for (const root of roots) {
            if (active.has(root)) {
                continue;
            }

            for (const control of root.querySelectorAll(Controls.Focusable)) {
                if (control !== root && !control.matches(this.#markerSelector) && control.closest(this.#itemRootSelector) === root && !this.#gathered.has(control)) {
                    this.#gathered.set(control, control.getAttribute('tabindex'));
                    control.tabIndex = -1;
                }
            }
        }
    }

    #releaseControls() {
        for (const [control, tabIndex] of this.#gathered) {
            if (control.isConnected) {
                this.#restoreTabIndex(control, tabIndex);
            }
        }

        this.#gathered.clear();
    }

    #restoreTabIndex(control, tabIndex) {
        if (tabIndex === null) {
            control.removeAttribute('tabindex');
        } else {
            control.setAttribute('tabindex', tabIndex);
        }
    }

    // The position an item carries in the owner's order, where the owner seeks.
    #positionOf(marker) {
        const value = this.#seeks ? this.#itemOf(marker).getAttribute(this.#positionAttribute) : null;

        return value == null || value === '' ? null : Number(value);
    }

    #setAnchor(marker) {
        this.#anchor = marker;
        this.#anchorPosition = this.#positionOf(marker);
    }

    // A range runs by position where the owner seeks, so an anchor scrolled out of the page still counts; by the markers drawn otherwise.
    #extendTo(marker, additive) {
        const position = this.#positionOf(marker);

        if (this.#anchorPosition !== null && position !== null) {
            tell(this.#owner, 'HandleRangeSelectByPositionAsync', this.#anchorPosition, position, additive);
            return;
        }

        // A range can span every item drawn, so its tokens cross through the transfer.
        tell(this.#owner, 'HandleRangeSelectAsync', Transfer.of(this.#rangeTokens(this.#anchor, marker)), additive);
    }

    #select(event) {
        const marker = this.#pressedMarker(event);
        this.#cancelSeek();

        // The click lands focus on the item's marker, so the keys carry on from the item the pointer chose.
        this.#focusMarker(marker, this.#markers());

        const toggle = event.ctrlKey || event.metaKey;

        if (event.shiftKey && this.#anchor) {
            this.#extendTo(marker, toggle);
            return;
        }

        this.#setAnchor(marker);
        tell(this.#owner, 'HandleSelectAsync', this.#tokenOf(marker), toggle);
    }

    // Asks the owner for the list's first or last item, a request for the same end the same way still pending joining it rather
    // than asking again; one that selects otherwise replaces it.
    #seek(edge, mode) {
        const effective = mode === 'extend' && this.#anchorPosition === null ? 'select' : mode;

        if (this.#seeking?.edge === edge && this.#seeking.mode === effective) {
            return;
        }

        this.#cancelSeek();
        const request = ++this.#lastRequest;
        this.#seeking = { request, edge, mode: effective };
        tell(this.#owner, 'HandleSeekAsync', request, edge > 0, effective, effective === 'extend' ? this.#anchorPosition : null);
    }

    #hold(marker) {
        this.#held = marker ? { token: this.#tokenOf(marker), position: this.#positionOf(marker), item: this.#itemOf(marker) } : null;
        this.#readerMoved = false;
        this.#moveStart = null;
    }

    // A press or a Tab may move the focus anywhere while it is handled, so a loss of focus meanwhile is the reader's.
    #beginMove() {
        this.#readerMoved = true;
        this.#moveStart = this.#target.ownerDocument.activeElement;
    }

    // An input whose handling left the focus on the element it began on moved nothing, so a later loss is not the reader's.
    #endMove() {
        if (this.#readerMoved && this.#moveStart !== null && this.#target.ownerDocument.activeElement === this.#moveStart) {
            this.#readerMoved = false;
        }

        this.#moveStart = null;
    }

    // Whether the focus has fallen to nothing rather than gone to something else.
    #isFocusLost() {
        const active = this.#target.ownerDocument.activeElement;

        return !active || active === this.#target.ownerDocument.body;
    }

    // Only a focus that fell to nothing is taken back, onto the held item's marker once the render has drawn it again, by token or by position.
    #restore(held) {
        if (this.#held !== held || this.#readerMoved || !this.#isFocusLost() || this.#waiting) {
            return;
        }

        const find = () => this.#target.querySelector(`[${this.#keyAttribute}="${held.token}"]`)
            ?? (held.position === null ? null : this.#markerAt(held.position))
            ?? this.#nearestMarker(held.item);

        this.#whenDrawn(find, marker => {
            if (this.#held === held && !this.#readerMoved && this.#isFocusLost()) {
                this.#focusMarker(marker, this.#markers());
            }
        });
    }

    /** The marker nearest an item still drawn that no longer carries one, the next before the previous. */
    #nearestMarker(item) {
        if (!item.isConnected || !this.#target.contains(item) || this.#markerIn(item)) {
            return null;
        }

        const all = this.#markers();

        return all.find(marker => item.compareDocumentPosition(marker) & Node.DOCUMENT_POSITION_FOLLOWING)
            ?? all.findLast(marker => item.compareDocumentPosition(marker) & Node.DOCUMENT_POSITION_PRECEDING)
            ?? null;
    }

    #markerAt(position) {
        return this.#markerIn(this.#target.querySelector(`[${this.#positionAttribute}="${position}"]`));
    }

    #cancelSeek() {
        this.#seeking = null;
        this.#waiting?.();
        this.#waiting = null;
    }

    // Hands over the marker the finder names once it is drawn, giving up after a while or when the reader moves on; a newer wait
    // replaces this one, and this one's own end stops it alone.
    #whenDrawn(find, land) {
        this.#waiting?.();
        this.#waiting = null;
        const marker = find();

        if (marker) {
            land(marker);
            return;
        }

        const observer = new MutationObserver(() => {
            const drawn = find();

            if (drawn) {
                stop();
                land(drawn);
            }
        });
        const timer = setTimeout(() => stop(), SeekPatience);
        const stop = () => {
            observer.disconnect();
            clearTimeout(timer);

            if (this.#waiting === stop) {
                this.#waiting = null;
            }
        };

        observer.observe(this.#target, this.#positionAttribute
            ? { childList: true, subtree: true, attributes: true, attributeFilter: [this.#positionAttribute] }
            : { childList: true, subtree: true });
        this.#waiting = stop;
    }

    /** The items and the place the key would move the focused one to, the list's end to ask the owner for, or null where there is nowhere to go. */
    #planMove(event, { key, edge, target }) {
        // A key pressed on the list itself or within one of its items is the list's; one pressed in another
        // control inside the list, a header's button or a sidebar's row, is that control's own.
        if (event.target !== this.#target && !event.target.closest(this.#itemSelector)) {
            return null;
        }

        const all = this.#markers();

        if (all.length === 0) {
            return null;
        }

        // The focused marker names the current item; tabbing onto one leaves the roving tab index where it was.
        const focused = document.activeElement?.closest(this.#markerSelector);
        const currentIndex = focused ? all.indexOf(focused) : -1;
        // The reading direction is the list's own, which the page's may differ from.
        const rightToLeft = getComputedStyle(this.#target).direction === 'rtl';
        const layout = new Layout(all.map(marker => this.#itemOf(marker)), rightToLeft);
        // Home and End reach the whole list where the item stands alone in its row or the primary modifier asks for it.
        const whole = edge !== undefined && (event.ctrlKey || event.metaKey || layout.standsAlone(currentIndex));

        if (whole && this.#seeks) {
            return { edge, whole };
        }

        const nextIndex = target(layout, currentIndex, whole);
        const isEnd = key === 'Home' || key === 'End';

        return nextIndex === currentIndex && !isEnd ? null : { all, next: all[nextIndex], whole };
    }

    // The move is planned again here, since a host's remap of the action replaces the binding and its predicate with it.
    #move(event, move) {
        const plan = this.#planMove(event, move);

        if (!plan) {
            return;
        }

        // Plain selects the item reached and the primary modifier moves the focus alone; Shift extends the range, with the
        // primary modifier too where the move reached the whole list's end. A list that does not select moves the focus alone.
        const primary = event.ctrlKey || event.metaKey;
        const mode = !this.#selects ? 'focus' : event.shiftKey && (!primary || plan.whole) ? 'extend' : primary ? 'focus' : 'select';

        if (plan.edge !== undefined && plan.all === undefined) {
            this.#seek(plan.edge, mode);
            return;
        }

        this.#cancelSeek();
        const { all, next } = plan;
        this.#focusMarker(next, all);

        if (mode === 'focus') {
            return;
        }

        if (mode === 'extend' && this.#anchor) {
            this.#extendTo(next, false);
            return;
        }

        this.#setAnchor(next);
        tell(this.#owner, 'HandleSelectAsync', this.#tokenOf(next), false);
    }
}

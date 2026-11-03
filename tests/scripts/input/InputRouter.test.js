import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { register, registerHost, resolve, requestDismiss, owns, ownerOf, describe as describeScope, stack, configure } from '#input/InputRouter.js';
import { fakeOwner } from '#support/owner.js';
import { sizeScroller } from '#support/platform.js';

const handles = [];

function scope(element, profile, owner = null) {
    const handle = register(element, profile, owner);
    handles.push(handle);

    return handle;
}

function host(profile, owner = null) {
    const handle = registerHost(profile, owner);
    handles.push(handle);

    return handle;
}

function key(target, key, init = {}) {
    const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true, ...init });
    target.dispatchEvent(event);

    return event;
}

function press(target, init = {}) {
    const event = new PointerEvent('pointerdown', { bubbles: true, cancelable: true, pointerId: 1, pointerType: 'mouse', ...init });
    target.dispatchEvent(event);

    return event;
}

// A click from a pointer counts one; a click raised from the keyboard counts none.
function click(target, init = {}) {
    const event = new MouseEvent('click', { bubbles: true, cancelable: true, detail: 1, ...init });
    target.dispatchEvent(event);

    return event;
}

function release(target, init = {}) {
    const event = new PointerEvent('pointerup', { bubbles: true, cancelable: true, pointerId: 1, pointerType: 'mouse', ...init });
    target.dispatchEvent(event);

    return event;
}

// A wheel event with its modifier keys defined on the event, which happy-dom drops from a WheelEvent's init.
function wheel(target, { shiftKey = false, ctrlKey = false, altKey = false, metaKey = false, ...init } = {}) {
    const event = new WheelEvent('wheel', { bubbles: true, cancelable: true, deltaY: 40, ...init });
    Object.defineProperties(event, { shiftKey: { value: shiftKey }, ctrlKey: { value: ctrlKey }, altKey: { value: altKey }, metaKey: { value: metaKey } });
    target.dispatchEvent(event);

    return event;
}

function drop(target, types) {
    const event = new Event('drop', { bubbles: true, cancelable: true });
    Object.defineProperty(event, 'dataTransfer', { value: { types } });
    target.dispatchEvent(event);

    return event;
}

const keyBinding = (key, modifiers = {}) => ({ type: 'keydown', key, modifiers });
const route = (action, handle, ...bindings) => ({ action, bindings, handle });
const arrowsTo = handle => [route('test.next', handle, keyBinding('ArrowDown')), route('test.previous', handle, keyBinding('ArrowUp'))];

beforeEach(() => {
    document.body.innerHTML = `
        <div id="grid">
            <div id="row"><span id="cell">Cell</span></div>
            <div id="popup">
                <div id="list"><button id="option">Option</button></div>
                <input id="field">
            </div>
        </div>
        <button id="trigger">Open</button>
        <button id="elsewhere">Elsewhere</button>`;
    configure({ escape: 'dismiss', pressAway: 'dismiss' });
});

afterEach(() => {
    while (handles.length > 0) {
        handles.pop().dispose();
    }

    configure({ bindings: {}, closeRequest: null });
});

const grid = () => document.querySelector('#grid');
const popup = () => document.querySelector('#popup');
const list = () => document.querySelector('#list');
const option = () => document.querySelector('#option');
const field = () => document.querySelector('#field');
const cell = () => document.querySelector('#cell');
const trigger = () => document.querySelector('#trigger');
const elsewhere = () => document.querySelector('#elsewhere');

function dialogWith(html = '') {
    const dialog = document.createElement('dialog');
    dialog.innerHTML = html;
    document.body.append(dialog);

    return dialog;
}

describe('InputRouter structural routing', () => {
    it('routes a key to the innermost scope that routes it', () => {
        const gridMoves = vi.fn();
        const listMoves = vi.fn();
        scope(grid(), { routes: arrowsTo(gridMoves) });
        scope(list(), { routes: arrowsTo(listMoves) });

        const event = key(option(), 'ArrowDown');

        expect(listMoves).toHaveBeenCalledWith(event, expect.objectContaining({ action: 'test.next', owner: list() }));
        expect(gridMoves).not.toHaveBeenCalled();
        expect(event.defaultPrevented).toBe(true);
    });

    it('lets an outer scope route a key an inner scope does not', () => {
        const gridMoves = vi.fn();
        scope(grid(), { routes: [...arrowsTo(gridMoves), route('test.first', gridMoves, keyBinding('Home'))] });
        scope(list(), { routes: arrowsTo(vi.fn()) });

        key(option(), 'Home');

        expect(gridMoves).toHaveBeenCalledOnce();
    });

    it('stops at a scope declared as a boundary for the class, naming it as owner', () => {
        const gridMoves = vi.fn();
        scope(grid(), { routes: [route('test.first', gridMoves, keyBinding('Home'))] });
        scope(list(), { boundary: ['keydown'], routes: arrowsTo(vi.fn()) });

        const event = key(option(), 'Home');

        expect(gridMoves).not.toHaveBeenCalled();
        expect(event.defaultPrevented).toBe(false);
        expect(resolve(event)).toMatchObject({ owner: list(), action: null, reason: 'boundary for keydown' });
    });

    it('never routes past an active scope', () => {
        const gridMoves = vi.fn();
        scope(grid(), { routes: arrowsTo(gridMoves) });
        scope(popup(), {}).activate();

        const event = key(option(), 'ArrowDown');

        expect(gridMoves).not.toHaveBeenCalled();
        expect(resolve(event)).toMatchObject({ owner: popup(), action: null, reason: 'active boundary' });
        expect(event.defaultPrevented).toBe(false);
    });

    it('answers owns and ownerOf from the same resolution', () => {
        scope(grid(), { routes: arrowsTo(vi.fn()) });
        scope(list(), { routes: arrowsTo(vi.fn()) });
        const event = new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true });
        Object.defineProperty(event, 'composedPath', { value: () => [option(), list(), popup(), grid(), document.body, document] });

        expect(ownerOf(event)).toBe(list());
        expect(owns(list(), event)).toBe(true);
        expect(owns(grid(), event)).toBe(false);
    });

    it('sends an action with no script handler to the .NET owner as its family describes it', () => {
        const owner = fakeOwner();
        scope(list(), { routes: [{ action: 'test.next', bindings: [keyBinding('ArrowDown')] }] }, owner);

        key(option(), 'ArrowDown');

        expect(owner.invokeMethodAsync).toHaveBeenCalledWith('HandleInputActionAsync', 'test.next', expect.objectContaining({ family: 'keyboard', key: 'ArrowDown', modifiers: expect.objectContaining({ control: false }) }));
    });

    it('listens on the scope itself for wheel routes and prevents the default there', () => {
        const scrolled = vi.fn();
        scope(grid(), { routes: [route('test.scroll-track', scrolled, { type: 'wheel' })] });

        const event = wheel(cell());

        expect(scrolled).toHaveBeenCalledOnce();
        expect(event.defaultPrevented).toBe(true);
    });

    it('listens on the scope itself for a drag passing over and prevents the default there', () => {
        const over = vi.fn();
        scope(grid(), { routes: [route('test.over', over, { type: 'dragover' })] });
        const event = new Event('dragover', { bubbles: true, cancelable: true });
        Object.defineProperty(event, 'dataTransfer', { value: { types: ['Files'] } });

        cell().dispatchEvent(event);

        expect(over).toHaveBeenCalledOnce();
        expect(event.defaultPrevented).toBe(true);
    });

    it('runs the owning handler once when nested scopes both listen for wheel', () => {
        const outer = vi.fn();
        const inner = vi.fn();
        scope(grid(), { routes: [route('test.scroll-track', outer, { type: 'wheel' })] });
        scope(list(), { routes: [route('test.scroll-list', inner, { type: 'wheel' })] });

        wheel(option());

        expect(inner).toHaveBeenCalledOnce();
        expect(outer).not.toHaveBeenCalled();
    });

    it('lets a routed press reach its target by default', () => {
        const pressed = vi.fn();
        scope(grid(), { routes: [route('test.press', pressed, { type: 'pointerdown' })] });
        const reached = vi.fn();
        cell().addEventListener('pointerdown', reached);

        const event = press(cell());

        expect(pressed).toHaveBeenCalledOnce();
        expect(event.defaultPrevented).toBe(false);
        expect(reached).toHaveBeenCalledOnce();
    });

    it('does not route a key while composing', () => {
        const typed = vi.fn();
        scope(grid(), { routes: [route('test.typed', typed, keyBinding('a'))] });

        const event = key(cell(), 'a', { isComposing: true });

        expect(typed).not.toHaveBeenCalled();
        expect(resolve(event).reason).toBe('composition');
    });
});

describe('InputRouter binding matching', () => {
    it('matches Control, Meta and Alt on a key only when stated, and Shift either way unless it is', () => {
        const bold = vi.fn();
        const next = vi.fn();
        scope(list(), { routes: [route('test.bold', bold, keyBinding('b', { control: true })), route('test.next', next, keyBinding('ArrowDown'))] });

        key(option(), 'b');
        key(option(), 'b', { ctrlKey: true });
        key(option(), 'b', { metaKey: true });
        key(option(), 'ArrowDown', { shiftKey: true });
        key(option(), 'ArrowDown', { ctrlKey: true });
        key(option(), 'ArrowDown', { altKey: true });

        expect(bold).toHaveBeenCalledOnce();
        expect(next).toHaveBeenCalledOnce();
    });

    it('matches any printable character but a space with a typing binding, and never a named key or a shortcut', () => {
        const find = vi.fn();
        scope(list(), { routes: [route('test.find', find, { type: 'keydown', typing: true })] });

        key(option(), 'p');
        key(option(), 'P', { shiftKey: true });
        key(option(), 'ArrowDown');
        key(option(), 'p', { ctrlKey: true });
        const space = key(option(), ' ');

        expect(find).toHaveBeenCalledTimes(2);
        expect(space.defaultPrevented).toBe(false);
    });

    it('prefers a named key over a typing binding for the same character', () => {
        const find = vi.fn();
        const pick = vi.fn();
        scope(list(), { routes: [route('test.find', find, { type: 'keydown', typing: true }), route('test.pick', pick, keyBinding('p'))] });

        key(option(), 'p');

        expect(pick).toHaveBeenCalledOnce();
        expect(find).not.toHaveBeenCalled();
    });

    it('prefers the more specific binding whatever its order', () => {
        const extend = vi.fn();
        const move = vi.fn();
        scope(list(), { routes: [route('test.move', move, keyBinding('ArrowDown')), route('test.extend', extend, keyBinding('ArrowDown', { shift: true }))] });

        const event = key(option(), 'ArrowDown', { shiftKey: true });
        key(option(), 'ArrowDown');

        expect(extend).toHaveBeenCalledOnce();
        expect(move).toHaveBeenCalledOnce();
        expect(resolve(event)).toMatchObject({ tie: 'specificity', reason: 'route test.extend, chosen by specificity' });
    });

    it('breaks an equally specific tie by priority, then by registration order, and says which', () => {
        const low = vi.fn();
        const high = vi.fn();
        const first = vi.fn();
        scope(list(), { routes: [{ action: 'test.low', bindings: [keyBinding('x')], handle: low }, { action: 'test.high', bindings: [keyBinding('x')], handle: high, priority: 1 }] });
        scope(grid(), { routes: [route('test.first', first, keyBinding('y')), route('test.second', vi.fn(), keyBinding('y'))] });

        const byPriority = key(option(), 'x');
        const byOrder = key(cell(), 'y');

        expect(high).toHaveBeenCalledOnce();
        expect(low).not.toHaveBeenCalled();
        expect(first).toHaveBeenCalledOnce();
        expect(resolve(byPriority).tie).toBe('priority');
        expect(resolve(byOrder).tie).toBe('registration-order');
    });

    it('reads primary as Control on this platform', () => {
        const save = vi.fn();
        scope(list(), { routes: [route('test.save', save, keyBinding('s', { primary: true }))] });

        key(option(), 's', { ctrlKey: true });
        key(option(), 's', { metaKey: true });

        expect(save).toHaveBeenCalledOnce();
    });

    it('matches a wheel turn by axis and by the modifiers it states', () => {
        const pageNext = vi.fn();
        const scroll = vi.fn();
        scope(grid(), { routes: [
            route('test.page-next', pageNext, { type: 'wheel', axis: 'horizontal' }, { type: 'wheel', modifiers: { shift: true } }),
            route('test.scroll', scroll, { type: 'wheel', axis: 'vertical' }),
        ] });

        wheel(cell(), { deltaX: 50, deltaY: 5 });
        wheel(cell(), { shiftKey: true });
        wheel(cell(), { ctrlKey: true });

        expect(pageNext).toHaveBeenCalledTimes(2);
        expect(scroll).toHaveBeenCalledOnce();
    });

    it('matches a press by button and by pointer type', () => {
        const context = vi.fn();
        const touch = vi.fn();
        scope(grid(), { routes: [route('test.context', context, { type: 'pointerdown', button: 2 }), route('test.touch', touch, { type: 'pointerdown', pointerType: 'touch' })] });

        press(cell(), { button: 2 });
        press(cell(), { pointerType: 'touch' });
        press(cell());

        expect(context).toHaveBeenCalledOnce();
        expect(touch).toHaveBeenCalledOnce();
    });

    it('matches a drop by the data it carries', () => {
        const files = vi.fn();
        scope(grid(), { routes: [route('test.files', files, { type: 'drop', data: ['Files'] })] });

        drop(cell(), ['text/plain']);
        drop(cell(), ['Files']);

        expect(files).toHaveBeenCalledOnce();
    });

    it('lets a binding add a predicate the vocabulary cannot express', () => {
        const handled = vi.fn();
        scope(grid(), { routes: [route('test.edge', handled, { type: 'wheel', when: event => event.deltaY > 100 })] });

        wheel(cell(), { deltaY: 10 });
        wheel(cell(), { deltaY: 200 });

        expect(handled).toHaveBeenCalledOnce();
    });

    it('reports a route with the class defaults of each of its bindings', () => {
        scope(list(), { routes: [route('test.scroll', vi.fn(), { type: 'wheel' }), { action: 'test.press', bindings: [{ type: 'pointerdown' }], handle: vi.fn(), propagation: 'stop' }] });

        expect(describeScope(list()).routes).toMatchObject([
            { action: 'test.scroll', priority: 0, source: 'default', bindingsSuppression: [{ type: 'wheel', preventDefault: true, propagation: 'stop' }] },
            { action: 'test.press', bindingsSuppression: [{ type: 'pointerdown', preventDefault: false, propagation: 'stop' }] },
        ]);
    });

    it('takes the class default from the binding that matched when a route mixes classes', () => {
        const activated = vi.fn();
        scope(list(), { routes: [route('test.activate', activated, keyBinding('Enter'), { type: 'pointerdown' })] });

        const pressed = press(option());
        const keyed = key(list(), 'Enter');

        expect(activated).toHaveBeenCalledTimes(2);
        expect(pressed.defaultPrevented).toBe(false);
        expect(keyed.defaultPrevented).toBe(true);
    });
});

describe('InputRouter clicks', () => {
    const clickBinding = (init = {}) => ({ type: 'click', button: 0, ...init });

    it('routes a click to the innermost scope that routes it, by button and only the modifiers it states', () => {
        const chosen = vi.fn();
        scope(grid(), { routes: [route('test.choose', chosen, clickBinding())] });

        click(cell());
        click(cell(), { shiftKey: true });
        click(cell(), { button: 2 });

        expect(chosen).toHaveBeenCalledTimes(2);
    });

    it('routes a double click by its own binding', () => {
        const opened = vi.fn();
        scope(grid(), { routes: [route('test.open', opened, { type: 'dblclick', button: 0 })] });

        click(cell());
        cell().dispatchEvent(new MouseEvent('dblclick', { bubbles: true, cancelable: true, detail: 2 }));

        expect(opened).toHaveBeenCalledOnce();
    });

    it('leaves a click on a native control to the control', () => {
        const chosen = vi.fn();
        scope(grid(), { routes: [route('test.choose', chosen, clickBinding())] });

        click(option());

        expect(chosen).not.toHaveBeenCalled();
        expect(resolve(new MouseEvent('click', { bubbles: true })).action).toBeNull();
    });

    it('routes a click raised from the keyboard, with no press before it', () => {
        const chosen = vi.fn();
        scope(grid(), { routes: [route('test.choose', chosen, clickBinding())] });

        click(cell(), { detail: 0 });

        expect(chosen).toHaveBeenCalledOnce();
    });

    it('swallows the click a claimed gesture ends in, on what captured it as on what was pressed, and routes the next', () => {
        const chosen = vi.fn();
        const gesture = scope(grid(), { routes: [route('test.choose', chosen, clickBinding())] });

        press(cell());
        gesture.claimClick(1, cell());
        release(cell());
        const swallowed = click(grid());
        press(cell());
        click(cell());

        expect(swallowed.defaultPrevented).toBe(true);
        expect(chosen).toHaveBeenCalledOnce();
    });

    it('never swallows a click raised from the keyboard, and keeps the claim for the pointer\'s own click', () => {
        const chosen = vi.fn();
        const gesture = scope(grid(), { routes: [route('test.choose', chosen, clickBinding())] });

        press(cell());
        gesture.claimClick(1, cell());
        click(cell(), { detail: 0 });
        const swallowed = click(cell());

        expect(chosen).toHaveBeenCalledOnce();
        expect(swallowed.defaultPrevented).toBe(true);
    });

    it('counts the period from the release rather than the press', () => {
        const chosen = vi.fn();
        const gesture = scope(grid(), { routes: [route('test.choose', chosen, clickBinding())] });
        const now = vi.spyOn(performance, 'now');

        now.mockReturnValue(1000);
        press(cell());
        gesture.claimClick(1, cell());
        now.mockReturnValue(5000);
        release(cell());
        now.mockReturnValue(5200);
        click(cell());

        expect(chosen).not.toHaveBeenCalled();
        now.mockRestore();
    });

    it('drops a click whose press stood in a scope that has since gone, rather than letting it fall to the scope beneath', () => {
        const outer = vi.fn();
        const inner = vi.fn();
        scope(grid(), { routes: [route('test.outer', outer, clickBinding())] });
        const row = register(document.querySelector('#row'), { routes: [route('test.inner', inner, clickBinding())] });

        press(cell());
        row.dispose();
        click(cell());

        expect(inner).not.toHaveBeenCalled();
        expect(outer).not.toHaveBeenCalled();
    });

    it('drops a click whose press stood in an active scope that has since turned inactive', () => {
        const outer = vi.fn();
        const inner = vi.fn();
        scope(grid(), { routes: [route('test.outer', outer, clickBinding())] });
        const row = scope(document.querySelector('#row'), { routes: [route('test.inner', inner, clickBinding())] });
        row.activate();

        press(cell());
        row.release();
        click(cell());

        expect(inner).not.toHaveBeenCalled();
        expect(outer).not.toHaveBeenCalled();
    });

    it('describes a click to a .NET owner as a pointer input', () => {
        const owner = { invokeMethodAsync: vi.fn().mockResolvedValue(undefined) };
        scope(grid(), { routes: [{ action: 'test.choose', bindings: [clickBinding()] }] }, owner);

        click(cell());

        expect(owner.invokeMethodAsync).toHaveBeenCalledWith('HandleInputActionAsync', 'test.choose', expect.objectContaining({ family: 'pointer', type: 'click' }));
    });
});

describe('InputRouter host bindings', () => {
    it('replaces an action\'s default bindings with the host\'s', () => {
        const bold = vi.fn();
        scope(list(), { routes: [route('composer.bold', bold, keyBinding('b', { primary: true }))] });
        configure({ bindings: { 'composer.bold': [keyBinding('b', { alt: true })] } });

        key(option(), 'b', { ctrlKey: true });
        key(option(), 'b', { altKey: true });

        expect(bold).toHaveBeenCalledOnce();
        expect(describeScope(list()).routes[0]).toMatchObject({ source: 'host', bindings: [keyBinding('b', { alt: true })], defaultBindings: [keyBinding('b', { primary: true })] });
    });

    it('extends and trims an action\'s defaults', () => {
        const next = vi.fn();
        scope(list(), { routes: [route('test.next', next, keyBinding('ArrowDown'), keyBinding('j'))] });
        configure({ bindings: { 'test.next': { add: [keyBinding('n', { primary: true })], remove: [keyBinding('j')] } } });

        key(option(), 'ArrowDown');
        key(option(), 'j');
        key(option(), 'n', { ctrlKey: true });

        expect(next).toHaveBeenCalledTimes(2);
    });

    it('unbinds an action the host removes', () => {
        const bold = vi.fn();
        scope(list(), { routes: [route('composer.bold', bold, keyBinding('b', { primary: true }))] });
        configure({ bindings: { 'composer.bold': null } });

        key(option(), 'b', { ctrlKey: true });

        expect(bold).not.toHaveBeenCalled();
        expect(describeScope(list()).routes[0].bindings).toEqual([]);
    });

    it('applies an override declared before the route registers', () => {
        configure({ bindings: { 'composer.bold': { replace: [keyBinding('b', { alt: true })] } } });
        const bold = vi.fn();
        scope(list(), { routes: [route('composer.bold', bold, keyBinding('b', { primary: true }))] });

        key(option(), 'b', { altKey: true });

        expect(bold).toHaveBeenCalledOnce();
    });
});

describe('InputRouter host routes', () => {
    it('routes to the host only after every component scope has declined', () => {
        const handled = vi.fn();
        const gridMoves = vi.fn();
        host({ routes: [route('test.palette', handled, keyBinding('k', { primary: true })), route('test.host-next', handled, keyBinding('ArrowDown'))] });
        scope(grid(), { routes: arrowsTo(gridMoves) });

        key(cell(), 'k', { ctrlKey: true });
        key(cell(), 'ArrowDown');
        key(elsewhere(), 'ArrowDown');

        expect(handled).toHaveBeenCalledTimes(2);
        expect(gridMoves).toHaveBeenCalledOnce();
    });

    it('does not reach the host through a boundary or an active scope', () => {
        const handled = vi.fn();
        host({ routes: [route('test.palette', handled, keyBinding('k', { primary: true }))] });
        scope(popup(), {}).activate();
        scope(elsewhere(), { boundary: ['keydown'] });

        key(option(), 'k', { ctrlKey: true });
        key(elsewhere(), 'k', { ctrlKey: true });

        expect(handled).not.toHaveBeenCalled();
    });

    it('answers a preemptive route before any component scope, native control or boundary', () => {
        const handled = vi.fn();
        const gridMoves = vi.fn();
        host({ preemptiveRoutes: [route('test.palette', handled, keyBinding('k', { primary: true }))] });
        scope(grid(), { routes: [route('test.link', gridMoves, keyBinding('k', { primary: true }))] });
        scope(popup(), {}).activate();

        const event = key(option(), 'k', { ctrlKey: true });
        key(field(), 'k', { ctrlKey: true });

        expect(handled).toHaveBeenCalledTimes(2);
        expect(gridMoves).not.toHaveBeenCalled();
        expect(resolve(event)).toMatchObject({ precedence: 'preemptive', source: 'default' });
    });

    it('listens for a preemptive route of a class that rides no document listener', () => {
        const handled = vi.fn();
        host({ preemptiveRoutes: [route('test.zoom', handled, { type: 'wheel', modifiers: { control: true } })] });

        wheel(cell(), { ctrlKey: true });

        expect(handled).toHaveBeenCalledOnce();
    });

    it('sends host actions to the .NET owner', () => {
        const owner = fakeOwner();
        host({ routes: [{ action: 'test.palette', bindings: [keyBinding('k', { primary: true })] }] }, owner);

        key(cell(), 'k', { ctrlKey: true });

        expect(owner.invokeMethodAsync).toHaveBeenCalledWith('HandleInputActionAsync', 'test.palette', expect.objectContaining({ family: 'keyboard', key: 'k', modifiers: expect.objectContaining({ primary: true }) }));
    });
});

describe('InputRouter pending edits', () => {
    // A field typed into, which has not yet reported its change.
    function edit(target, value) {
        target.focus();
        target.value = value;
        target.dispatchEvent(new Event('input', { bubbles: true }));
    }

    it("commits the focused field's edit before it routes an action", () => {
        const order = [];
        host({ preemptiveRoutes: [route('test.save', () => order.push(`action ${field().value}`), keyBinding('s', { primary: true }))] });
        field().addEventListener('change', () => order.push(`change ${field().value}`));
        edit(field(), 'typed');

        key(field(), 's', { ctrlKey: true });

        expect(order).toEqual(['change typed', 'action typed']);
    });

    it("commits the focused field's edit before it dismisses", () => {
        const order = [];
        scope(popup(), { onDismiss: () => order.push('dismiss') }).activate();
        field().addEventListener('change', () => order.push('change'));
        edit(field(), 'typed');

        key(field(), 'Escape');

        expect(order).toEqual(['change', 'dismiss']);
    });

    it('commits nothing for an edit the field already reported', () => {
        const handled = vi.fn();
        const changes = vi.fn();
        host({ preemptiveRoutes: [route('test.save', handled, keyBinding('s', { primary: true }))] });
        edit(field(), 'typed');
        field().dispatchEvent(new Event('change', { bubbles: true }));
        field().addEventListener('change', changes);

        key(field(), 's', { ctrlKey: true });

        expect(handled).toHaveBeenCalledOnce();
        expect(changes).not.toHaveBeenCalled();
    });

    it('commits nothing for a key a native field keeps', () => {
        const changes = vi.fn();
        host({ preemptiveRoutes: [route('test.save', vi.fn(), keyBinding('s', { primary: true }))] });
        field().addEventListener('change', changes);
        edit(field(), 'typed');

        key(field(), 'ArrowLeft');

        expect(changes).not.toHaveBeenCalled();
    });
});

describe('InputRouter registrations', () => {
    it('lets several registrations share one scope and disposes each on its own', () => {
        const moves = vi.fn();
        const scrolls = vi.fn();
        scope(grid(), { routes: arrowsTo(moves) });
        const wheelRegistration = scope(grid(), { routes: [route('test.scroll', scrolls, { type: 'wheel' })] });

        wheel(cell());
        wheelRegistration.dispose();
        wheel(cell());
        key(cell(), 'ArrowDown');

        expect(scrolls).toHaveBeenCalledOnce();
        expect(moves).toHaveBeenCalledOnce();
        expect(describeScope(grid()).registrations).toBe(1);
    });

    it('keeps the scope active until every registration releases it', () => {
        const first = scope(popup(), {});
        const second = scope(popup(), {});
        first.activate();
        second.activate();
        first.release();

        expect(describeScope(popup()).active).toBe(true);

        second.release();

        expect(describeScope(popup()).active).toBe(false);
    });

    it('registers nothing on an element reference that resolved to nothing', () => {
        expect(register(null, { modal: true })).toBeNull();
        expect(stack()).toEqual([]);
    });

    it('releases what a disposed registration held', () => {
        const registration = scope(popup(), {});
        registration.activate();
        registration.dispose();
        handles.pop();

        expect(stack()).toEqual([]);
        expect(describeScope(popup())).toBeNull();
    });

    it('treats the null a .NET profile sends for an unset field as not declared', () => {
        const moves = vi.fn();
        const dismissed = vi.fn();
        scope(grid(), {});
        scope(popup(), {
            modal: false, escape: null, pressAway: null, parent: null, boundary: [], within: [],
            routes: [{ action: 'test.next', bindings: [{ type: 'keydown', key: 'ArrowDown', modifiers: { control: null, alt: null, shift: null, meta: null, primary: false } }], handle: moves, priority: 0, preventDefault: null, propagation: null }],
        }).activate();
        scope(popup(), { escape: 'cancel', onDismiss: dismissed }).activate();

        const event = key(option(), 'ArrowDown');
        key(option(), 'Escape');

        expect(moves).toHaveBeenCalledOnce();
        expect(resolve(event).reason).toBe('route test.next');
        expect(dismissed).toHaveBeenCalledWith('cancel', 'escape');
        expect(describeScope(popup()).entries[0].escape).toEqual({ value: 'dismiss', source: 'host' });
        expect(describeScope(popup()).parent).toBe(grid());
    });

    it('routes through a registration whose fields arrive as null', () => {
        const moves = vi.fn();
        scope(popup(), { routes: [{ action: 'test.next', bindings: [{ type: 'keydown', key: 'ArrowDown', modifiers: { control: null, alt: null, shift: null, meta: null, primary: false } }], handle: moves, preventDefault: null, propagation: null }] });

        const event = key(option(), 'ArrowDown');

        expect(moves).toHaveBeenCalledOnce();
        expect(event.defaultPrevented).toBe(true);
    });

    it('ignores a second dispose', () => {
        const moves = vi.fn();
        const first = scope(grid(), { routes: [] });
        scope(grid(), { routes: arrowsTo(moves) });
        first.dispose();
        first.dispose();
        handles.splice(handles.indexOf(first), 1);

        key(cell(), 'ArrowDown');

        expect(moves).toHaveBeenCalledOnce();
        expect(describeScope(grid()).registrations).toBe(1);
    });

    it('applies an updated profile at once', () => {
        const moves = vi.fn();
        const registration = scope(grid(), { routes: [] });

        registration.update({ routes: arrowsTo(moves) });
        key(cell(), 'ArrowDown');

        expect(moves).toHaveBeenCalledOnce();
    });
});

describe('InputRouter native controls', () => {
    it('leaves the wheel to a panel that scrolls along the turn\'s axis', () => {
        const scrolled = vi.fn();
        const panel = document.createElement('div');
        panel.style.overflowY = 'auto';
        panel.innerHTML = '<p>long</p>';
        sizeScroller(panel, { scrollHeight: 400, clientHeight: 100 });
        grid().append(panel);
        scope(grid(), { routes: [route('test.scroll', scrolled, { type: 'wheel' })] });

        const event = wheel(panel.querySelector('p'));

        expect(scrolled).not.toHaveBeenCalled();
        expect(resolve(event)).toMatchObject({ owner: panel, precedence: 'native', action: null });
    });

    it('routes the wheel over a panel that scrolls only the other axis', () => {
        const scrolled = vi.fn();
        const panel = document.createElement('div');
        panel.style.overflowX = 'auto';
        panel.innerHTML = '<p>wide</p>';
        sizeScroller(panel, { scrollWidth: 400, clientWidth: 100 });
        grid().append(panel);
        scope(grid(), { routes: [route('test.scroll', scrolled, { type: 'wheel' })] });

        wheel(panel.querySelector('p'));

        expect(scrolled).toHaveBeenCalledOnce();
    });

    it.each(['video', 'audio'])('leaves the arrows, Home, End, Enter and Space to a %s with the browser\'s controls', tag => {
        const steps = vi.fn();
        const media = document.createElement(tag);
        media.setAttribute('controls', '');
        grid().append(media);
        scope(grid(), { routes: [route('test.next', steps, keyBinding('ArrowRight')), route('test.first', steps, keyBinding('Home')), route('test.play', steps, keyBinding(' '))] });

        const seek = key(media, 'ArrowRight');
        key(media, 'Home');
        key(media, ' ');

        expect(steps).not.toHaveBeenCalled();
        expect(resolve(seek)).toMatchObject({ owner: media, precedence: 'native', action: null });
    });

    it('routes the arrows over a media element without controls', () => {
        const steps = vi.fn();
        const media = document.createElement('video');
        grid().append(media);
        scope(grid(), { routes: [route('test.next', steps, keyBinding('ArrowRight'))] });

        key(media, 'ArrowRight');

        expect(steps).toHaveBeenCalledOnce();
    });

    it('leaves the keys a text field consumes to the field', () => {
        const gridMoves = vi.fn();
        scope(grid(), { routes: [...arrowsTo(gridMoves), route('test.first', gridMoves, keyBinding('Home')), route('test.select-all', gridMoves, keyBinding('a', { control: true }))] });

        const home = key(field(), 'Home');
        key(field(), 'a', { ctrlKey: true });
        key(field(), 'x');

        expect(gridMoves).not.toHaveBeenCalled();
        expect(resolve(home)).toMatchObject({ owner: field(), precedence: 'native', action: null });
        expect(home.defaultPrevented).toBe(false);
    });

    it('routes the keys a text field does not consume', () => {
        const gridMoves = vi.fn();
        scope(grid(), { routes: arrowsTo(gridMoves) });

        key(field(), 'ArrowDown');

        expect(gridMoves).toHaveBeenCalledOnce();
    });

    it('treats a character typed through AltGr as typing', () => {
        const shortcut = vi.fn();
        scope(grid(), { routes: [route('test.shortcut', shortcut, keyBinding('ó', { control: true, alt: true }))] });
        const event = new KeyboardEvent('keydown', { key: 'ó', ctrlKey: true, altKey: true, bubbles: true, cancelable: true, modifierAltGraph: true });

        field().dispatchEvent(event);

        expect(shortcut).not.toHaveBeenCalled();
        expect(resolve(event).precedence).toBe('native');
    });

    it.each([
        ['number', 'ArrowDown'],
        ['date', 'ArrowUp'],
        ['range', 'ArrowDown'],
        ['radio', 'ArrowUp'],
    ])('leaves the arrows to a %s input', (type, pressed) => {
        const gridMoves = vi.fn();
        scope(grid(), { routes: arrowsTo(gridMoves) });
        field().type = type;

        key(field(), pressed);

        expect(gridMoves).not.toHaveBeenCalled();
    });

    it('leaves Enter and Space to a button and Space to a checkbox', () => {
        const activated = vi.fn();
        scope(grid(), { routes: [route('test.open', activated, keyBinding('Enter')), route('test.toggle', activated, keyBinding(' '))] });
        field().type = 'checkbox';

        key(option(), 'Enter');
        key(option(), ' ');
        key(field(), ' ');
        key(field(), 'Enter');

        expect(activated).toHaveBeenCalledOnce();
    });

    it('leaves paging to a text area and to a select', () => {
        const paged = vi.fn();
        scope(grid(), { routes: [route('test.page', paged, keyBinding('PageDown'))] });
        const area = document.createElement('textarea');
        const select = document.createElement('select');
        grid().append(area, select);

        key(area, 'PageDown');
        key(select, 'PageDown');

        expect(paged).not.toHaveBeenCalled();
    });

    it('leaves a shortcut pressed inside an active nested scope to that scope, never to the routes around it', () => {
        const bold = vi.fn();
        scope(grid(), { routes: [route('composer.bold', bold, keyBinding('b', { primary: true }))] });
        scope(popup(), {}).activate();

        const inPopup = key(field(), 'b', { ctrlKey: true });
        const outside = key(cell(), 'b', { ctrlKey: true });

        expect(resolve(inPopup)).toMatchObject({ owner: popup(), action: null, reason: 'active boundary' });
        expect(bold).toHaveBeenCalledOnce();
        expect(bold).toHaveBeenCalledWith(outside, expect.objectContaining({ action: 'composer.bold' }));
    });

    it('lets a scope registered on an editable route a shortcut the editable would otherwise consume', () => {
        const editor = document.createElement('div');
        editor.setAttribute('contenteditable', 'true');
        editor.innerHTML = '<p id="text">Text</p>';
        grid().append(editor);
        const bold = vi.fn();
        scope(editor, { routes: [route('test.bold', bold, keyBinding('b', { control: true }))] });

        key(document.querySelector('#text'), 'b', { ctrlKey: true });
        const italic = key(document.querySelector('#text'), 'i', { ctrlKey: true });
        const typed = key(document.querySelector('#text'), 'x');

        expect(bold).toHaveBeenCalledOnce();
        expect(resolve(italic).precedence).toBe('native');
        expect(resolve(typed).precedence).toBe('native');
    });
});

describe('InputRouter active routing', () => {
    it('sends Escape to the foremost active registration only', () => {
        const outer = vi.fn();
        const inner = vi.fn();
        scope(grid(), { onDismiss: outer }).activate();
        scope(popup(), { onDismiss: inner }).activate();

        const event = key(option(), 'Escape');

        expect(inner).toHaveBeenCalledWith('dismiss', 'escape');
        expect(outer).not.toHaveBeenCalled();
        expect(event.defaultPrevented).toBe(true);
    });

    it('sends one Escape to one registration when several on the same element are active', () => {
        const first = vi.fn();
        const second = vi.fn();
        scope(popup(), { onDismiss: first }).activate();
        scope(popup(), { escape: 'cancel', onDismiss: second }).activate();

        key(option(), 'Escape');

        expect(second).toHaveBeenCalledWith('cancel', 'escape');
        expect(first).not.toHaveBeenCalled();
        expect(describeScope(popup()).entries.map(entry => entry.foremost)).toEqual([false, true]);
    });

    it('sends Escape to the foremost registration wherever focus is', () => {
        const dismissed = vi.fn();
        scope(popup(), { onDismiss: dismissed }).activate();

        key(elsewhere(), 'Escape');

        expect(dismissed).toHaveBeenCalledOnce();
    });

    it('delivers cancel as the action a registration declared', () => {
        const dismissed = vi.fn();
        scope(popup(), { escape: 'cancel', onDismiss: dismissed }).activate();

        const event = key(option(), 'Escape');

        expect(dismissed).toHaveBeenCalledWith('cancel', 'escape');
        expect(resolve(event)).toMatchObject({ action: 'cancel', reason: 'escape' });
    });

    it('does not offer a declined Escape to the registration beneath', () => {
        const outer = vi.fn();
        scope(grid(), { onDismiss: outer }).activate();
        scope(popup(), { escape: 'ignore', onDismiss: vi.fn() }).activate();

        const event = key(option(), 'Escape');

        expect(outer).not.toHaveBeenCalled();
        expect(event.defaultPrevented).toBe(false);
    });

    it('ranks a contained scope above the one that contains it, whatever the activation order', () => {
        const outerDismiss = vi.fn();
        const innerDismiss = vi.fn();
        const inner = scope(popup(), { onDismiss: innerDismiss });
        const outer = scope(grid(), { onDismiss: outerDismiss });
        inner.activate();
        outer.activate();

        key(cell(), 'Escape');

        expect(innerDismiss).toHaveBeenCalledOnce();
        expect(outerDismiss).not.toHaveBeenCalled();
    });

    it('takes a parent and the elements within from a placement given beside the profile', () => {
        const popupDismiss = vi.fn();
        const dragDismiss = vi.fn();
        const shared = { onDismiss: dragDismiss };
        scope(popup(), { onDismiss: popupDismiss }).activate();
        handles.push(register(elsewhere(), shared, null, { parent: popup(), within: [trigger()] }));
        handles.at(-1).activate();

        key(cell(), 'Escape');
        press(trigger());

        expect(dragDismiss).toHaveBeenCalledTimes(1);
        expect(popupDismiss).not.toHaveBeenCalled();
        expect(shared.parent).toBeUndefined();
    });

    it('ranks a registration above the parent it declares even outside it', () => {
        const popupDismiss = vi.fn();
        const dragDismiss = vi.fn();
        scope(popup(), { onDismiss: popupDismiss }).activate();
        scope(elsewhere(), { parent: popup(), onDismiss: dragDismiss }).activate();

        key(cell(), 'Escape');

        expect(dragDismiss).toHaveBeenCalledOnce();
        expect(popupDismiss).not.toHaveBeenCalled();
        expect(stack().at(-1)).toMatchObject({ element: elsewhere(), parent: popup(), reason: 'inside its parent' });
    });

    it('puts a modal registration above everything that is not inside it', () => {
        const popupDismiss = vi.fn();
        const dialogDismiss = vi.fn();
        scope(popup(), { onDismiss: popupDismiss }).activate();
        const dialog = dialogWith();
        scope(dialog, { modal: true, onDismiss: dialogDismiss }).activate();

        key(option(), 'Escape');

        expect(dialogDismiss).toHaveBeenCalledOnce();
        expect(popupDismiss).not.toHaveBeenCalled();
        expect(stack()).toMatchObject([{ element: dialog, reason: 'modal barrier' }]);
    });

    it('keeps an unrelated registration activated later beneath the modal', () => {
        const dialogDismiss = vi.fn();
        const lateDismiss = vi.fn();
        scope(dialogWith(), { modal: true, onDismiss: dialogDismiss }).activate();
        scope(popup(), { onDismiss: lateDismiss }).activate();

        key(option(), 'Escape');

        expect(dialogDismiss).toHaveBeenCalledOnce();
        expect(lateDismiss).not.toHaveBeenCalled();
        expect(popup().hasAttribute('data-forma-layer')).toBe(false);
    });

    it('lets a registration inside the modal, or one declaring it as parent, outrank the modal', () => {
        const dialog = dialogWith('<div id="inner-popup"><button id="inner-option">x</button></div>');
        const dialogDismiss = vi.fn();
        const popupDismiss = vi.fn();
        const dragDismiss = vi.fn();
        scope(dialog, { modal: true, onDismiss: dialogDismiss }).activate();
        scope(dialog.querySelector('#inner-popup'), { onDismiss: popupDismiss }).activate();

        key(dialog.querySelector('#inner-option'), 'Escape');

        expect(popupDismiss).toHaveBeenCalledOnce();

        scope(elsewhere(), { parent: dialog.querySelector('#inner-popup'), onDismiss: dragDismiss }).activate();
        key(dialog.querySelector('#inner-option'), 'Escape');

        expect(dragDismiss).toHaveBeenCalledOnce();
        expect(dialogDismiss).not.toHaveBeenCalled();
    });

    it('dismisses on a press outside the foremost registration and swallows the click on what was pressed', () => {
        const dismissed = vi.fn();
        scope(popup(), { onDismiss: dismissed, within: [trigger()] }).activate();
        const clicked = vi.fn();
        elsewhere().addEventListener('click', clicked);

        const down = press(elsewhere());
        click(elsewhere());

        expect(dismissed).toHaveBeenCalledWith('dismiss', 'press-away');
        expect(down.defaultPrevented).toBe(true);
        expect(clicked).not.toHaveBeenCalled();
    });

    it('does not swallow a click on something other than what was pressed, and forgets the press', () => {
        scope(popup(), { onDismiss: vi.fn() }).activate();
        const clicked = vi.fn();
        trigger().addEventListener('click', clicked);
        elsewhere().addEventListener('click', clicked);

        press(elsewhere());
        click(trigger());
        click(elsewhere());

        expect(clicked).toHaveBeenCalledTimes(2);
    });

    it('does not swallow a click from another pointer', () => {
        scope(popup(), { onDismiss: vi.fn() }).activate();
        const clicked = vi.fn();
        elsewhere().addEventListener('click', clicked);

        press(elsewhere(), { pointerId: 3 });
        elsewhere().dispatchEvent(new PointerEvent('click', { bubbles: true, cancelable: true, pointerId: 4, detail: 1 }));

        expect(clicked).toHaveBeenCalledOnce();
    });

    it('does not swallow a click that arrives after the period', () => {
        scope(popup(), { onDismiss: vi.fn() }).activate();
        const clicked = vi.fn();
        elsewhere().addEventListener('click', clicked);
        const now = vi.spyOn(performance, 'now');

        now.mockReturnValue(1000);
        press(elsewhere());
        now.mockReturnValue(2500);
        click(elsewhere());

        expect(clicked).toHaveBeenCalledOnce();
        now.mockRestore();
    });

    it('forgets the swallowed click when the pointer is canceled', () => {
        scope(popup(), { onDismiss: vi.fn() }).activate();
        const clicked = vi.fn();
        elsewhere().addEventListener('click', clicked);

        press(elsewhere(), { pointerId: 7 });
        elsewhere().dispatchEvent(new PointerEvent('pointercancel', { bubbles: true, pointerId: 7 }));
        click(elsewhere());

        expect(clicked).toHaveBeenCalledOnce();
    });

    it('counts a press on an element declared within as inside', () => {
        const dismissed = vi.fn();
        scope(popup(), { onDismiss: dismissed, within: [trigger()] }).activate();

        press(trigger());

        expect(dismissed).not.toHaveBeenCalled();
    });

    it('passes the press through, then hands it to the scope that routes it', () => {
        const pressed = vi.fn();
        scope(elsewhere(), { routes: [route('test.press', pressed, { type: 'pointerdown' })] });
        const dismissed = vi.fn();
        scope(popup(), { onDismiss: dismissed, pressAway: 'pass-through' }).activate();
        const clicked = vi.fn();
        elsewhere().addEventListener('click', clicked);

        const down = press(elsewhere());
        click(elsewhere());

        expect(dismissed).toHaveBeenCalledWith('dismiss', 'press-away');
        expect(pressed).toHaveBeenCalledOnce();
        expect(down.defaultPrevented).toBe(false);
        expect(clicked).toHaveBeenCalledOnce();
    });

    it('resolves an ignored outside press structurally', () => {
        const pressed = vi.fn();
        scope(elsewhere(), { routes: [route('test.press', pressed, { type: 'pointerdown' })] });
        scope(popup(), { onDismiss: vi.fn(), pressAway: 'ignore' }).activate();

        const event = press(elsewhere());

        expect(pressed).toHaveBeenCalledOnce();
        expect(owns(popup(), event)).toBe(false);
    });

    it('reports a dismissal to the .NET owner when no script handler is given', () => {
        const owner = fakeOwner();
        scope(popup(), {}, owner).activate();

        key(option(), 'Escape');

        expect(owner.invokeMethodAsync).toHaveBeenCalledWith('HandleDismissAsync', 'dismiss', 'escape');
    });

    it('stops answering once released', () => {
        const dismissed = vi.fn();
        const handle = scope(popup(), { onDismiss: dismissed });
        handle.activate();
        handle.release();

        key(option(), 'Escape');

        expect(dismissed).not.toHaveBeenCalled();
        expect(stack()).toEqual([]);
    });

    it('inherits the host defaults a profile does not state', () => {
        configure({ pressAway: 'ignore' });
        const dismissed = vi.fn();
        scope(popup(), { onDismiss: dismissed }).activate();

        press(elsewhere());

        expect(dismissed).not.toHaveBeenCalled();
    });
});

describe('InputRouter close requests', () => {
    it('dismisses the registration a close request reaches while it is foremost', () => {
        const dismissed = vi.fn();
        scope(popup(), { escape: 'cancel', onDismiss: dismissed }).activate();

        const decision = requestDismiss(popup());

        expect(dismissed).toHaveBeenCalledWith('cancel', 'close-request');
        expect(decision).toMatchObject({ owner: popup(), action: 'cancel', reason: 'close-request', preventDefault: true });
    });

    it('forwards a close request raised beneath the foremost registration to it', () => {
        const dialogDismiss = vi.fn();
        const popupDismiss = vi.fn();
        const dialog = dialogWith('<div id="inner-popup"></div>');
        scope(dialog, { modal: true, onDismiss: dialogDismiss }).activate();
        scope(dialog.querySelector('#inner-popup'), { onDismiss: popupDismiss }).activate();

        const decision = requestDismiss(dialog);

        expect(popupDismiss).toHaveBeenCalledWith('dismiss', 'close-request');
        expect(dialogDismiss).not.toHaveBeenCalled();
        expect(decision).toMatchObject({ owner: dialog.querySelector('#inner-popup'), action: 'dismiss', preventDefault: true, reason: 'close-request forwarded to the foremost registration' });
    });

    it('consumes a close request the foremost registration ignores', () => {
        scope(popup(), { escape: 'ignore', onDismiss: vi.fn() }).activate();

        expect(requestDismiss(popup())).toMatchObject({ action: null, preventDefault: true });
    });

    it('declines a close request from a scope that is not active, and null for an element that is not a scope', () => {
        const dismissed = vi.fn();
        scope(popup(), {});
        scope(grid(), { onDismiss: dismissed }).activate();

        expect(requestDismiss(popup())).toMatchObject({ action: null, preventDefault: false, reason: 'source not active' });
        expect(dismissed).not.toHaveBeenCalled();
        expect(requestDismiss(elsewhere())).toBeNull();
    });

    it('lets a close request differ from Escape when the registration or the host says so', () => {
        const dismissed = vi.fn();
        const registration = scope(popup(), { escape: 'ignore', closeRequest: 'dismiss', onDismiss: dismissed });
        registration.activate();

        key(option(), 'Escape');
        requestDismiss(popup());

        expect(dismissed).toHaveBeenCalledWith('dismiss', 'close-request');
        expect(describeScope(popup()).entries[0].closeRequest).toEqual({ value: 'dismiss', source: 'registration' });

        registration.update({ closeRequest: null });
        configure({ closeRequest: 'cancel' });
        requestDismiss(popup());

        expect(dismissed).toHaveBeenLastCalledWith('cancel', 'close-request');
        expect(describeScope(popup()).entries[0].closeRequest).toEqual({ value: 'cancel', source: 'host' });
    });

    it('offers a cancel event raised on an active scope as a close request and prevents the native cancellation', () => {
        const dismissed = vi.fn();
        const dialog = dialogWith();
        scope(dialog, { modal: true, onDismiss: dismissed }).activate();

        const event = new Event('cancel', { cancelable: true });
        dialog.dispatchEvent(event);

        expect(dismissed).toHaveBeenCalledWith('dismiss', 'close-request');
        expect(event.defaultPrevented).toBe(true);
    });

    it('resolves a cancel event on a dialog to the popup open above it, leaving the dialog open', () => {
        const dialogDismiss = vi.fn();
        const popupDismiss = vi.fn();
        const dialog = dialogWith('<div id="inner-popup"></div>');
        scope(dialog, { modal: true, onDismiss: dialogDismiss }).activate();
        scope(dialog.querySelector('#inner-popup'), { onDismiss: popupDismiss }).activate();

        const event = new Event('cancel', { cancelable: true });
        dialog.dispatchEvent(event);

        expect(popupDismiss).toHaveBeenCalledWith('dismiss', 'close-request');
        expect(dialogDismiss).not.toHaveBeenCalled();
        expect(event.defaultPrevented).toBe(true);
    });

    it('leaves a cancel event alone on an element that is not an active scope', () => {
        scope(grid(), { onDismiss: vi.fn() }).activate();
        const idle = dialogWith();
        scope(idle, { modal: true, onDismiss: vi.fn() });

        const unregistered = new Event('cancel', { cancelable: true });
        elsewhere().dispatchEvent(unregistered);
        const inactive = new Event('cancel', { cancelable: true });
        idle.dispatchEvent(inactive);

        expect(unregistered.defaultPrevented).toBe(false);
        expect(inactive.defaultPrevented).toBe(false);
    });
});

describe('InputRouter registration lifetime and topology', () => {
    it('answers no call after disposal', () => {
        const dismissed = vi.fn();
        const registration = scope(popup(), { onDismiss: dismissed });
        registration.dispose();
        handles.pop();

        registration.activate();
        registration.update({ escape: 'cancel' });
        key(option(), 'Escape');

        expect(registration.disposed).toBe(true);
        expect(registration.active).toBe(false);
        expect(stack()).toEqual([]);
        expect(dismissed).not.toHaveBeenCalled();
    });

    it('rejects a scope as its own parent', () => {
        expect(() => register(popup(), { parent: popup() })).toThrow();
    });

    it('rejects a parent that would close a cycle', () => {
        scope(grid(), { parent: elsewhere() });
        scope(elsewhere(), { parent: popup() });
        const popupRegistration = scope(popup(), {});

        expect(() => register(popup(), { parent: grid() })).toThrow();
        expect(() => popupRegistration.update({ parent: grid() })).toThrow();
    });

    it('resolves through a shadow root to the host and its scopes', () => {
        const moves = vi.fn();
        const shell = document.createElement('div');
        grid().append(shell);
        const shadow = shell.attachShadow({ mode: 'open' });
        shadow.innerHTML = '<button id="inner">Inner</button>';
        scope(grid(), { routes: arrowsTo(moves) });

        const event = new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true, composed: true, cancelable: true });
        shadow.querySelector('#inner').dispatchEvent(event);

        expect(moves).toHaveBeenCalledOnce();
    });

    it('passes the owning element to a handler, whichever listener resolved the event', () => {
        const pressed = vi.fn();
        scope(grid(), { routes: [route('test.press', pressed, { type: 'pointerdown' })] });
        scope(list(), { routes: [route('test.press-inner', vi.fn(), { type: 'pointerdown', button: 2 })] });

        const event = press(option());

        expect(pressed).toHaveBeenCalledWith(event, expect.objectContaining({ action: 'test.press', owner: grid(), binding: { type: 'pointerdown' } }));
    });

    it('restamps layers when an inactive parent scope goes away', () => {
        const outer = scope(grid(), {});
        scope(popup(), {}).activate();
        scope(elsewhere(), { parent: grid() }).activate();

        expect(stack().map(entry => entry.element)).toEqual([popup(), elsewhere()]);

        outer.dispose();
        handles.splice(handles.indexOf(outer), 1);

        expect(stack().map(entry => entry.element)).toEqual([popup(), elsewhere()]);
        expect(stack().at(-1).parent).toBeNull();
    });
});

describe('InputRouter validation', () => {
    it('rejects an action that is not qualified by its owner', () => {
        expect(() => register(grid(), { routes: [{ action: 'next', bindings: [keyBinding('ArrowDown')] }] })).toThrow(/composer\.bold/);
    });

    it('rejects a binding that requires the primary modifier and requires it absent', () => {
        expect(() => register(grid(), { routes: [route('test.save', vi.fn(), keyBinding('s', { primary: true, control: false }))] })).toThrow();
    });

    it('reports bindings two routes claim equally', () => {
        scope(grid(), { routes: [route('test.first', vi.fn(), keyBinding('y')), route('test.second', vi.fn(), keyBinding('y')), route('test.third', vi.fn(), keyBinding('z'))] });

        expect(describeScope(grid()).conflicts).toEqual([{ binding: keyBinding('y'), actions: ['test.first', 'test.second'] }]);
    });

    it('returns an action to its defaults when a later configuration leaves it out', () => {
        const bold = vi.fn();
        scope(list(), { routes: [route('composer.bold', bold, keyBinding('b', { primary: true }))] });
        configure({ bindings: { 'composer.bold': null } });
        configure({ bindings: {} });

        key(option(), 'b', { ctrlKey: true });

        expect(bold).toHaveBeenCalledOnce();
        expect(describeScope(list()).routes[0].source).toBe('default');
    });

    it('does not prevent the default of a key release by default', () => {
        const released = vi.fn();
        scope(list(), { routes: [route('test.release', released, { type: 'keyup', key: 'Shift' })] });

        const event = new KeyboardEvent('keyup', { key: 'Shift', bubbles: true, cancelable: true });
        option().dispatchEvent(event);

        expect(released).toHaveBeenCalledOnce();
        expect(event.defaultPrevented).toBe(false);
    });

    it('matches a pointer move by the buttons held', () => {
        const dragged = vi.fn();
        scope(grid(), { routes: [route('test.drag', dragged, { type: 'pointermove', buttons: 1 })] });

        cell().dispatchEvent(new PointerEvent('pointermove', { bubbles: true, buttons: 0 }));
        cell().dispatchEvent(new PointerEvent('pointermove', { bubbles: true, buttons: 1 }));
        cell().dispatchEvent(new PointerEvent('pointermove', { bubbles: true, buttons: 3 }));

        expect(dragged).toHaveBeenCalledTimes(2);
    });
});

describe('InputRouter queries', () => {
    it('describes a scope, its active registrations, and its place in the ordering', () => {
        scope(grid(), { routes: arrowsTo(vi.fn()) });
        scope(popup(), { escape: 'cancel' }).activate();

        expect(describeScope(popup())).toMatchObject({ active: true, layer: 1, foremost: true, parent: grid(), entries: [{ escape: { value: 'cancel', source: 'registration' }, foremost: true }] });
        expect(describeScope(grid())).toMatchObject({ active: false, layer: null, parent: null, entries: [], routes: [expect.objectContaining({ action: 'test.next' }), expect.objectContaining({ action: 'test.previous' })] });
        expect(describeScope(elsewhere())).toBeNull();
    });

    it('takes the layer from a scope once its registration is disposed or released', () => {
        const disposed = scope(popup(), {});
        disposed.activate();
        const released = scope(elsewhere(), {});
        released.activate();

        disposed.dispose();
        released.release();

        expect(popup().hasAttribute('data-forma-layer')).toBe(false);
        expect(elsewhere().hasAttribute('data-forma-layer')).toBe(false);
    });

    it('stamps active scopes with their layer and explains the ordering', () => {
        scope(grid(), {}).activate();
        scope(popup(), {}).activate();
        scope(elsewhere(), {}).activate();

        expect(grid().getAttribute('data-forma-layer')).toBe('1');
        expect(popup().getAttribute('data-forma-layer')).toBe('2');
        expect(stack()).toMatchObject([
            { element: grid(), reason: 'first eligible' },
            { element: popup(), reason: 'inside its parent' },
            { element: elsewhere(), reason: 'activated later' },
        ]);
    });

    it('restamps layers when a registration turns modal', () => {
        scope(popup(), {}).activate();
        const dialog = scope(dialogWith(), {});
        dialog.activate();

        expect(popup().getAttribute('data-forma-layer')).toBe('1');

        dialog.update({ modal: true });

        expect(popup().hasAttribute('data-forma-layer')).toBe(false);
        expect(dialog.element.getAttribute('data-forma-layer')).toBe('1');
    });
});

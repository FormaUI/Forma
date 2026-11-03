import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { attach } from '#input/InputInspector.js';
import { register } from '#input/InputRouter.js';
import { fakeOwner } from '#support/owner.js';

let owner;
let inspector;
let registration;

beforeEach(() => {
    document.body.innerHTML = '<div id="grid" class="scope grid wide"><button id="cell">Cell</button></div><div id="panel"></div>';
    owner = fakeOwner();
    inspector = attach(document.querySelector('#panel'), { owner, wheelInterval: 100 });
    registration = register(document.querySelector('#grid'), {
        escape: 'cancel',
        routes: [{ action: 'grid.next', bindings: [{ type: 'keydown', key: 'ArrowDown' }], handle: () => {} }],
    });
});

afterEach(() => {
    inspector.dispose();
    registration.dispose();
});

function report() {
    return owner.invokeMethodAsync.mock.calls.filter(([name]) => name === 'ReportAsync').map(([, decision]) => decision);
}

describe('InputInspector', () => {
    it('reports a key as the router resolves it, with the owner described and the stack', () => {
        registration.activate();

        document.querySelector('#cell').dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true, cancelable: true }));

        const [decision] = report();
        expect(decision).toMatchObject({
            owner: 'div#grid',
            action: 'grid.next',
            precedence: 'structural',
            source: 'default',
            preventDefault: true,
            propagation: 'stop',
            input: { family: 'keyboard', key: 'ArrowDown' },
            stack: [{ element: 'div#grid', layer: 1, modal: false, parent: null, reason: 'first eligible' }],
        });
    });

    it('names a scope by its runtime id before its id, and by its id before its classes', () => {
        const grid = document.querySelector('#grid');
        const press = () => document.querySelector('#cell').dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true, cancelable: true }));
        grid.dataset.rid = '01a0869a-c663-7852-94c3-8147a795218d';
        registration.activate();

        press();
        const byRuntimeId = report().at(-1);
        expect(byRuntimeId.owner).toBe('div[data-rid="01a0869a-c663-7852-94c3-8147a795218d"]');
        expect(byRuntimeId.stack[0].element).toBe('div[data-rid="01a0869a-c663-7852-94c3-8147a795218d"]');

        delete grid.dataset.rid;
        press();
        const byId = report().at(-1);
        expect(byId.owner).toBe('div#grid');
        expect(byId.stack[0].element).toBe('div#grid');

        grid.removeAttribute('id');
        press();
        const byClasses = report().at(-1);
        expect(byClasses.owner).toBe('div.scope.grid');
        expect(byClasses.stack[0].element).toBe('div.scope.grid');
    });

    it('describes an unowned input with no owner and an empty stack', () => {
        document.querySelector('#cell').dispatchEvent(new PointerEvent('pointerdown', { bubbles: true, pointerId: 1 }));

        expect(report().at(-1)).toMatchObject({ owner: null, action: null, precedence: 'none', stack: [] });
    });

    it('reports wheel turns at most once per interval', () => {
        const now = vi.spyOn(performance, 'now');
        now.mockReturnValue(1000);
        document.querySelector('#cell').dispatchEvent(new WheelEvent('wheel', { bubbles: true, deltaY: 10 }));
        now.mockReturnValue(1050);
        document.querySelector('#cell').dispatchEvent(new WheelEvent('wheel', { bubbles: true, deltaY: 10 }));
        now.mockReturnValue(1200);
        document.querySelector('#cell').dispatchEvent(new WheelEvent('wheel', { bubbles: true, deltaY: 10 }));

        expect(report().filter(decision => decision.input.family === 'wheel')).toHaveLength(2);
        now.mockRestore();
    });

    it('stops reporting once disposed', () => {
        inspector.dispose();

        document.querySelector('#cell').dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));

        expect(report()).toHaveLength(0);
    });
});

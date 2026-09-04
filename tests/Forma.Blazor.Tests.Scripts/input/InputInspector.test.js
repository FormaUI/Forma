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

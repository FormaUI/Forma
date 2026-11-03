import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { attach } from '#browser/behaviors/DropZone.js';
import { configure, register, resolve } from '#input/InputRouter.js';
import { fakeOwner } from '#support/owner.js';

describe('DropZone', () => {
    /** @type {HTMLElement} */
    let zone;

    // The handles attached in a test, whose registrations the router holds until they are disposed after it.
    const handles = [];
    const attachZone = (target, options) => {
        const handle = attach(target, { action: 'test.drop', ...options });
        handles.push(handle);

        return handle;
    };

    beforeEach(() => {
        document.body.innerHTML = '<div id="zone"><span id="child">Child</span></div>';
        zone = document.querySelector('#zone');
    });

    afterEach(() => {
        handles.splice(0).forEach(handle => handle.dispose());
        configure({ bindings: {} });
    });

    // The transfer given is the event's own, so a test can read back the effect the zone set on it.
    const dragEvent = (type, dataTransfer = {}) => {
        const event = new Event(type, { bubbles: true, cancelable: true });
        dataTransfer.types ??= [];
        event.dataTransfer = dataTransfer;

        return event;
    };

    const drag = (type, target = zone, dataTransfer = {}) => {
        const event = dragEvent(type, dataTransfer);
        target.dispatchEvent(event);

        return event;
    };

    it('marks the zone active while a drag is over it', () => {
        attachZone(zone, { activeClass: 'is-dragging' });

        drag('dragenter');

        expect(zone.classList.contains('is-dragging')).toBe(true);
    });

    it('takes the drag as it enters, so the drag passing over reaches the zone rather than the body', () => {
        attachZone(zone);

        const event = drag('dragenter');

        expect(event.defaultPrevented).toBe(true);
    });

    // A drag crossing a child fires dragenter for the child and dragleave for the zone.
    it('stays active while the drag crosses a child', () => {
        attachZone(zone, { activeClass: 'is-dragging' });

        drag('dragenter');
        drag('dragenter', document.querySelector('#child'));
        drag('dragleave');

        expect(zone.classList.contains('is-dragging')).toBe(true);
    });

    it('clears the active mark once the drag has left every level', () => {
        attachZone(zone, { activeClass: 'is-dragging' });

        drag('dragenter');
        drag('dragenter', document.querySelector('#child'));
        drag('dragleave');
        drag('dragleave');

        expect(zone.classList.contains('is-dragging')).toBe(false);
    });

    it('declares the drag passing over and the drop as one route under the action it is given', () => {
        attachZone(zone, { action: 'picker.drop' });

        const over = drag('dragover');
        const drop = drag('drop');

        expect(resolve(over)).toMatchObject({ owner: zone, action: 'picker.drop', precedence: 'structural' });
        expect(resolve(drop)).toMatchObject({ owner: zone, action: 'picker.drop', precedence: 'structural' });
    });

    it('claims the drag so the browser does not open the file', () => {
        attachZone(zone);

        const event = drag('dragover');

        expect(event.defaultPrevented).toBe(true);
    });

    it('asks for a copy unless told another effect', () => {
        attachZone(zone);
        const transfer = { types: [] };

        drag('dragover', zone, transfer);

        expect(transfer.dropEffect).toBe('copy');
    });

    it('shows the effect it was given', () => {
        attachZone(zone, { dropEffect: 'move' });
        const transfer = { types: [] };

        drag('dragover', zone, transfer);

        expect(transfer.dropEffect).toBe('move');
    });

    it('hands the dropped transfer and the zone to the caller', () => {
        const dropped = [];
        attachZone(zone, { onDrop: (transfer, target) => dropped.push([transfer, target]) });
        const transfer = { types: [], files: [] };

        drag('drop', zone, transfer);

        expect(dropped).toEqual([[transfer, zone]]);
    });

    it('takes only a drag carrying the data it accepts', () => {
        const dropped = vi.fn();
        attachZone(zone, { accepts: ['Files'], onDrop: dropped });

        const text = drag('dragover', zone, { types: ['text/plain'] });
        drag('drop', zone, { types: ['text/plain'] });
        drag('drop', zone, { types: ['Files'] });

        expect(text.defaultPrevented).toBe(false);
        expect(dropped).toHaveBeenCalledOnce();
    });

    it('takes the host\'s remap of the drop', () => {
        configure({ bindings: { 'test.drop': [{ type: 'drop', data: ['text/plain'] }] } });
        const dropped = vi.fn();
        attachZone(zone, { accepts: ['Files'], onDrop: dropped });

        drag('drop', zone, { types: ['Files'] });
        drag('drop', zone, { types: ['text/plain'] });

        expect(dropped).toHaveBeenCalledOnce();
    });

    it('leaves a drop a nested scope routes to that scope', () => {
        const dropped = vi.fn();
        attachZone(zone, { onDrop: dropped });
        const inner = register(document.querySelector('#child'), { routes: [{ action: 'test.inner', bindings: [{ type: 'drop' }], handle: () => {} }] });
        handles.push(inner);

        const event = drag('drop', document.querySelector('#child'));

        expect(resolve(event).owner).toBe(document.querySelector('#child'));
        expect(dropped).not.toHaveBeenCalled();
    });

    it('clears the active mark on drop however deep the drag went', () => {
        attachZone(zone, { activeClass: 'is-dragging' });

        drag('dragenter');
        drag('dragenter', document.querySelector('#child'));
        drag('drop');

        expect(zone.classList.contains('is-dragging')).toBe(false);
    });

    it('clears the active mark when disposed mid-drag', () => {
        const handle = attachZone(zone, { activeClass: 'is-dragging' });
        drag('dragenter');

        handle.dispose();

        expect(zone.classList.contains('is-dragging')).toBe(false);
    });

    it('stops responding once disposed', () => {
        const handle = attachZone(zone, { activeClass: 'is-dragging' });

        handle.dispose();
        drag('dragenter');
        const event = drag('dragover');

        expect(zone.classList.contains('is-dragging')).toBe(false);
        expect(event.defaultPrevented).toBe(false);
    });

    describe('with targets inside it', () => {
        beforeEach(() => {
            document.body.innerHTML = `
                <ul id="zone">
                    <li id="root" data-drop-target="true"><span id="root-name">All</span></li>
                    <li id="races" data-drop-target="true" data-drop-key="races"><span id="races-name">Races</span></li>
                    <li id="sealed"><span id="sealed-name">Sealed</span></li>
                </ul>`;
            zone = document.querySelector('#zone');
        });

        it('tells the owner the key of the target that took the drop', () => {
            const owner = fakeOwner();
            attachZone(zone, { owner, targets: '[data-drop-target]' });

            drag('drop', document.querySelector('#races-name'));

            expect(owner.invokeMethodAsync).toHaveBeenCalledWith('HandleDropAsync', 'races');
        });

        it('tells the owner nothing for a target without a key', () => {
            const owner = fakeOwner();
            attachZone(zone, { owner, targets: '[data-drop-target]' });

            drag('drop', document.querySelector('#root-name'));

            expect(owner.invokeMethodAsync).toHaveBeenCalledWith('HandleDropAsync', null);
        });

        it('leaves a drag over a part that is no target to the browser', () => {
            const owner = fakeOwner();
            attachZone(zone, { owner, targets: '[data-drop-target]' });

            const over = drag('dragover', document.querySelector('#sealed-name'));
            drag('drop', document.querySelector('#sealed-name'));

            expect(over.defaultPrevented).toBe(false);
            expect(resolve(over).action).toBeNull();
            expect(owner.invokeMethodAsync).not.toHaveBeenCalled();
        });

        it('hands the target to a function given instead of an owner', () => {
            const dropped = [];
            attachZone(zone, { targets: '[data-drop-target]', onDrop: (transfer, target) => dropped.push(target.id) });

            drag('drop', document.querySelector('#races-name'));

            expect(dropped).toEqual(['races']);
        });
    });
});

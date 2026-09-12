import { describe, it, expect, beforeEach } from 'vitest';
import { attach } from '#common/behaviors/DropZone.js';

describe('DropZone', () => {
    /** @type {HTMLElement} */
    let zone;

    beforeEach(() => {
        document.body.innerHTML = '<div id="zone"><span id="child">Child</span></div>';
        zone = document.querySelector('#zone');
    });

    const dragEvent = (type, dataTransfer = {}) => {
        const event = new Event(type, { bubbles: true, cancelable: true });
        event.dataTransfer = dataTransfer;

        return event;
    };

    it('marks the zone active while a drag is over it', () => {
        attach(zone, { activeClass: 'is-dragging' });

        zone.dispatchEvent(dragEvent('dragenter'));

        expect(zone.classList.contains('is-dragging')).toBe(true);
    });

    // A drag crossing a child fires dragenter for the child and dragleave for the zone.
    it('stays active while the drag crosses a child', () => {
        attach(zone, { activeClass: 'is-dragging' });

        zone.dispatchEvent(dragEvent('dragenter'));
        document.querySelector('#child').dispatchEvent(dragEvent('dragenter'));
        zone.dispatchEvent(dragEvent('dragleave'));

        expect(zone.classList.contains('is-dragging')).toBe(true);
    });

    it('clears the active mark once the drag has left every level', () => {
        attach(zone, { activeClass: 'is-dragging' });

        zone.dispatchEvent(dragEvent('dragenter'));
        document.querySelector('#child').dispatchEvent(dragEvent('dragenter'));
        zone.dispatchEvent(dragEvent('dragleave'));
        zone.dispatchEvent(dragEvent('dragleave'));

        expect(zone.classList.contains('is-dragging')).toBe(false);
    });

    it('claims the drag so the browser does not open the file', () => {
        attach(zone, {});

        const event = dragEvent('dragover');
        zone.dispatchEvent(event);

        expect(event.defaultPrevented).toBe(true);
    });

    it('asks for a copy rather than a move', () => {
        attach(zone, {});
        const transfer = {};

        zone.dispatchEvent(dragEvent('dragover', transfer));

        expect(transfer.dropEffect).toBe('copy');
    });

    it('hands the dropped transfer to the caller', () => {
        const dropped = [];
        attach(zone, { onDrop: transfer => dropped.push(transfer) });
        const transfer = { files: [] };

        zone.dispatchEvent(dragEvent('drop', transfer));

        expect(dropped).toEqual([transfer]);
    });

    it('clears the active mark on drop however deep the drag went', () => {
        attach(zone, { activeClass: 'is-dragging' });

        zone.dispatchEvent(dragEvent('dragenter'));
        document.querySelector('#child').dispatchEvent(dragEvent('dragenter'));
        zone.dispatchEvent(dragEvent('drop'));

        expect(zone.classList.contains('is-dragging')).toBe(false);
    });

    it('clears the active mark when disposed mid-drag', () => {
        const handle = attach(zone, { activeClass: 'is-dragging' });
        zone.dispatchEvent(dragEvent('dragenter'));

        handle.dispose();

        expect(zone.classList.contains('is-dragging')).toBe(false);
    });

    it('stops responding once disposed', () => {
        const handle = attach(zone, { activeClass: 'is-dragging' });

        handle.dispose();
        zone.dispatchEvent(dragEvent('dragenter'));

        expect(zone.classList.contains('is-dragging')).toBe(false);
    });
});

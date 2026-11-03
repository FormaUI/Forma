/**
 * Splitter — a splitter's browser side: its bar dragged along the axis it moves on, the travel told to its owner once a
 * frame, and the drag's end told once the bar is let go or the drag is canceled.
 *
 * Built on DragGesture, which captures the pointer as the press lands and cancels the drag on Escape, so the drag
 * follows the pointer and ends on its release wherever the pointer is, however long the owner takes to answer.
 */

import { attach as attachDragGesture } from '../behaviors/DragGesture.js';
import { FrameScheduler } from '../../../core/scripts/FrameScheduler.js';
import { tell } from '../../../core/scripts/Owner.js';

/**
 * @param {HTMLElement} bar The splitter's bar.
 * @param {{ owner: import('../../../core/scripts/Owner.js').Owner, axis: 'inline' | 'block', action: string }} options
 *   The owner told of the drag, the axis the bar moves along, inline for a vertical bar and block for a horizontal
 *   one, and the action the press is routed as.
 */
export function attach(bar, { owner, axis, action }) {
    return new Splitter(bar, owner, axis, action);
}

class Splitter {
    #owner;
    #gesture;
    #scheduler = new FrameScheduler();
    #travel = null;

    constructor(bar, owner, axis, action) {
        this.#owner = owner;
        const isInline = axis === 'inline';

        this.#gesture = attachDragGesture(bar, {
            threshold: 0,
            axis: 'any',
            autoScroll: false,
            action,
            findSubject: () => bar,
            onStart: () => tell(owner, 'HandleResizeStartAsync'),
            onMove: ({ clientX, clientY, startX, startY }) => this.#move(isInline ? clientX - startX : clientY - startY),
            onDrop: () => this.#end(false),
            onCancel: () => this.#end(true),
        });
    }

    dispose() {
        this.#scheduler.cancel();
        this.#gesture.dispose();
    }

    // The latest travel waits for the next frame, so the owner hears once a frame however often the pointer moves.
    #move(travel) {
        this.#travel = travel;
        this.#scheduler.request(() => this.#flush());
    }

    #flush() {
        if (this.#travel !== null) {
            tell(this.#owner, 'HandleResizeAsync', this.#travel);
            this.#travel = null;
        }
    }

    // The travel still waiting is told before the end, so the owner ends on where the pointer was let go.
    #end(isCanceled) {
        this.#scheduler.cancel();

        if (!isCanceled) {
            this.#flush();
        }

        this.#travel = null;
        tell(this.#owner, 'HandleResizeEndAsync', isCanceled);
    }
}

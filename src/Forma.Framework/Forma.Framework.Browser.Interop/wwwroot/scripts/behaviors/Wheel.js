/**
 * Wheel — what a surface does with a wheel turned over it
 */

import { tell } from '../../../core/scripts/Owner.js';
import { register } from '../../../input/scripts/InputRouter.js';

/** Milliseconds between events that still belong to one gesture. */
const GestureGap = 150;

/** Pixels below which a delta is a trackpad's rather than a wheel notch's. */
const NotchDistance = 40;

/** The router's name for each key a turn can be gated on. */
const Modifiers = { ctrl: 'control', alt: 'alt', shift: 'shift' };

/** @typedef {import('../../../core/scripts/Owner.js').Owner} Owner */

/**
 * @param {HTMLElement} target The surface the wheel is turned over.
 * @param {{ action: string, owner: Owner, modifier?: 'ctrl' | 'alt' | 'shift' }} options
 *   The action the turn is routed as, the owner told its direction, and the modifier key the turn is gated on.
 */
export function attach(target, options) {
    return new Wheel(target, options);
}

/** Tells an owner the direction of a wheel turned over the target. */
class Wheel {
    #registration;

    constructor(target, { action, owner, modifier }) {
        const sink = new DirectionSink(owner);
        const modifiers = modifier ? { [Modifiers[modifier]]: true } : {};

        this.#registration = register(target, {
            routes: [{ action, bindings: [sink.describeBinding(modifiers)], handle: event => sink.move(event) }],
        });
    }

    dispose() {
        this.#registration.dispose();
    }
}

/** Tells an owner which way the wheel moved, once for each notch and once for each trackpad gesture. */
class DirectionSink {
    #owner;
    #lastAt = Number.NEGATIVE_INFINITY;
    #isGestureReported = false;

    constructor(owner) {
        this.#owner = owner;
    }

    /** Describes the binding of a wheel turn on either axis. */
    describeBinding(modifiers) {
        return { type: 'wheel', modifiers, when: event => DirectionSink.#deltaOf(event) !== 0 };
    }

    // A host's remap drops the binding's check of the delta.
    move(event) {
        const delta = DirectionSink.#deltaOf(event);

        if (delta === 0) {
            return;
        }

        const isNotch = event.deltaMode !== WheelEvent.DOM_DELTA_PIXEL || Math.abs(delta) >= NotchDistance;
        const isSameGesture = event.timeStamp - this.#lastAt < GestureGap;
        this.#lastAt = event.timeStamp;

        if (!isNotch && isSameGesture && this.#isGestureReported) {
            return;
        }

        this.#isGestureReported = !isNotch;
        tell(this.#owner, 'HandleWheelAsync', Math.sign(delta));
    }

    static #deltaOf(event) {
        return event.deltaX !== 0 ? event.deltaX : event.deltaY;
    }
}

/**
 * Wheel — what a surface does with a wheel turned over it
 *
 * A vertical turn moves a track along its inline axis, or a turn tells an owner which way it
 * turned. Either is a route on the input router, declared on the surface under the action the
 * surface names.
 */

import { Direction } from '../../../core/scripts/Direction.js';
import { tell } from '../../../core/scripts/Owner.js';
import { register } from '../../../input/scripts/InputRouter.js';

/** Pixels a line is taken to be, for a wheel that reports in lines. */
const LineHeight = 16;

/** Milliseconds between events that still belong to one gesture. */
const GestureGap = 150;

/** Pixels below which a delta is a trackpad's rather than a wheel notch's. */
const NotchDistance = 40;

/** The router's name for each key a turn can be gated on. */
const Modifiers = { ctrl: 'control', alt: 'alt', shift: 'shift' };

/** @typedef {import('../../../core/scripts/Owner.js').Owner} Owner */

/**
 * @param {HTMLElement} target The surface the wheel is turned over.
 * @param {{ action: string, track?: HTMLElement, owner?: Owner, modifier?: 'ctrl' | 'alt' | 'shift' }} options
 *   The action the turn is routed as; the track the wheel moves, the target when none is named;
 *   the owner told the direction through HandleWheelAsync; and the modifier key held for the turn
 *   to be answered.
 */
export function attach(target, options) {
    return new Wheel(target, options);
}

/** Answers a wheel turned over the target: a track moves by it, or an owner is told its direction. */
class Wheel {
    #registration;

    constructor(target, { action, track, owner, modifier }) {
        const sink = owner ? new DirectionSink(owner) : new TrackSink(track ?? target);
        const modifiers = modifier ? { [Modifiers[modifier]]: true } : {};

        this.#registration = register(target, {
            routes: [{ action, bindings: [sink.describeBinding(modifiers)], handle: event => sink.move(event) }],
        });
    }

    dispose() {
        this.#registration.dispose();
    }
}

/** Scrolls a track by the movement until it reaches either end. */
class TrackSink {
    #track;

    constructor(track) {
        this.#track = track;
    }

    /** A vertical turn while the track can scroll, consumed at either end too. */
    describeBinding(modifiers) {
        return { type: 'wheel', axis: 'vertical', modifiers, when: () => this.#slack() > 1 };
    }

    move(event) {
        const slack = this.#slack();
        const isRightToLeft = Direction.isRightToLeft(this.#track);
        const distance = event.deltaMode === WheelEvent.DOM_DELTA_LINE ? event.deltaY * LineHeight : event.deltaY;
        const offset = this.#track.scrollLeft;
        const wanted = offset + (isRightToLeft ? -distance : distance);
        const next = isRightToLeft ? Math.min(0, Math.max(-slack, wanted)) : Math.max(0, Math.min(slack, wanted));

        if (Math.abs(next - offset) < 1) {
            return;
        }

        this.#track.scrollTo({ left: next, behavior: 'instant' });
    }

    /** How far the track can scroll; a sub-pixel track width leaves a fractional remainder below one pixel. */
    #slack() {
        return this.#track.scrollWidth - this.#track.clientWidth;
    }
}

/** Tells an owner which way the wheel moved: every notch of a wheel, and each gesture of a trackpad once. */
class DirectionSink {
    #owner;
    #lastAt = Number.NEGATIVE_INFINITY;
    #isGestureReported = false;

    constructor(owner) {
        this.#owner = owner;
    }

    /** A turn on either axis that moved at all. */
    describeBinding(modifiers) {
        return { type: 'wheel', modifiers, when: event => DirectionSink.#deltaOf(event) !== 0 };
    }

    // The delta is checked again, since a host's remap of the action replaces the binding and its predicate with it.
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

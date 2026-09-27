/**
 * Wheel — what a surface does with a wheel turned over it
 *
 * A wheel that reports vertically is turned into movement along a track, or an owner is told
 * which way it turned. Either is a route on the input router, declared on the surface under the
 * action the surface names, so a turn over a popover opened from the surface, or over a panel
 * inside it that scrolls on its own, is theirs and never reaches the surface, and the host remaps
 * the turn by that action. Inline offsets are read through Math.abs where a right-to-left
 * container counts scrollLeft downwards from zero.
 */

import { Direction } from '../../../core/scripts/Direction.js';
import { tell } from '../../../core/scripts/Owner.js';
import { register } from '../../../input/scripts/InputRouter.js';

/** Pixels a line is taken to be, for the wheels that report in lines rather than pixels. */
const LineHeight = 16;

/** Milliseconds between events that still belong to one gesture. */
const GestureGap = 150;

/**
 * Pixels below which a delta is a trackpad's rather than a wheel notch's. A notch reports whole
 * lines, or in pixels no fewer than the 48 of three lines, while a trackpad reports the pointer's
 * own movement, a few pixels at a time.
 */
const NotchDistance = 40;

/** The router's name for each key a turn can be gated on. */
const Modifiers = { ctrl: 'control', alt: 'alt', shift: 'shift' };

/** @typedef {import('../../../core/scripts/Owner.js').Owner} Owner */

/**
 * @param {HTMLElement} target The surface the wheel is turned over.
 * @param {{ action: string, track?: HTMLElement, owner?: Owner, modifier?: 'ctrl' | 'alt' | 'shift' }} options
 *   The action the turn is routed as; answer a wheel over the target by moving the track, the
 *   target when none is named, or by telling the owner the direction through HandleWheelAsync,
 *   only with the modifier key held when one is named.
 */
export function attach(target, options) {
    return new Wheel(target, options);
}

/** Answers a wheel turned over the target, which reports vertically alone: a track moves by it, or an owner is told its direction. */
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

    /** A vertical turn, while the track has somewhere to scroll; the turn is consumed at either end too, so the page stays put. */
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

        // Each notch lands at once.
        this.#track.scrollTo({ left: next, behavior: 'instant' });
    }

    /** How far the track can scroll; a sub-pixel track width leaves a fractional remainder, so a pixel is the smallest real slack. */
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

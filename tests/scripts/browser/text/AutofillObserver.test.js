import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { AutofillObserver } from '#browser/text/AutofillObserver.mjs';

describe('AutofillObserver', () => {
    /** @type {HTMLInputElement} */
    let field;
    /** @type {ReturnType<typeof vi.fn>} */
    let publish;
    /** @type {AutofillObserver[]} */
    let observers;

    beforeEach(() => {
        document.body.innerHTML = '<input id="field" />';
        field = document.querySelector('#field');
        publish = vi.fn();
        observers = [];
    });

    afterEach(() => {
        observers.forEach(observer => observer.dispose());
    });

    const watch = () => {
        const observer = new AutofillObserver(field, {}, publish);
        observers.push(observer);

        return { observer, first: observer.read() };
    };

    const animate = animationName => {
        const event = new Event('animationstart', { bubbles: true });
        event.animationName = animationName;
        field.dispatchEvent(event);
    };

    // A browser matching the autofill pseudo-class in the form given, filled or not, and throwing on any other.
    const matchAutofill = (form, isFilled) => vi.spyOn(field, 'matches').mockImplementation(selector => {
        if (selector !== form) {
            throw new SyntaxError(`'${selector}' is not a valid selector`);
        }

        return isFilled;
    });

    it('reads a field the browser has not filled as unfilled', () => {
        const { first } = watch();

        expect(first).toEqual({ isFilled: false });
    });

    it('reads a fill made before the observation began from the field\'s autofill state, in either form', () => {
        matchAutofill(':-webkit-autofill', true);

        const { first } = watch();

        expect(first).toEqual({ isFilled: true });
    });

    it('publishes the fill as the browser makes it, and its clearing', () => {
        watch();

        animate('forma-autofill-start');
        animate('forma-autofill-cancel');

        expect(publish.mock.calls.map(([state]) => state)).toEqual([{ isFilled: true }, { isFilled: false }]);
    });

    it('publishes nothing for an animation the field runs for its own reasons, or for a state already published', () => {
        watch();

        animate('forma-shake');
        animate('forma-autofill-cancel');

        expect(publish).not.toHaveBeenCalled();
    });

    it('publishes the state again once refreshed, read afresh where the browser can say', () => {
        const { observer } = watch();
        matchAutofill(':autofill', true);

        observer.refresh();

        expect(publish).toHaveBeenCalledExactlyOnceWith({ isFilled: true });
    });

    it('keeps the state the signals gave where the browser matches neither form', () => {
        const { observer } = watch();
        animate('forma-autofill-start');
        vi.spyOn(field, 'matches').mockImplementation(() => {
            throw new SyntaxError('not a valid selector');
        });

        observer.refresh();

        expect(publish.mock.calls.map(([state]) => state)).toEqual([{ isFilled: true }, { isFilled: true }]);
    });

    it('stops observing once disposed', () => {
        const { observer } = watch();

        observer.dispose();
        animate('forma-autofill-start');

        expect(publish).not.toHaveBeenCalled();
    });
});

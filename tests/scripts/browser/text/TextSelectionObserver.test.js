import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { TextSelectionObserver } from '#browser/text/TextSelectionObserver.mjs';

describe('TextSelectionObserver', () => {
    /** @type {HTMLTextAreaElement} */
    let field;
    /** @type {ReturnType<typeof vi.fn>} */
    let publish;
    /** @type {TextSelectionObserver[]} */
    let observers;

    beforeEach(() => {
        document.body.innerHTML = '<textarea id="field">Ada Lovelace</textarea><input id="other" value="Other">';
        field = document.querySelector('#field');
        publish = vi.fn();
        observers = [];
    });

    afterEach(() => {
        observers.forEach(observer => observer.dispose());
    });

    const watch = () => {
        const observer = new TextSelectionObserver(field, {}, publish);
        observers.push(observer);

        return { observer, first: observer.read() };
    };

    const select = (start, end, target = field) => {
        target.setSelectionRange(start, end);
        target.dispatchEvent(new Event('select'));
    };

    // A caret the reader moves fires no select, where setting the range does, so the move holds that event back.
    const moveCaret = (start, end) => {
        const holdSelect = event => event.stopImmediatePropagation();
        window.addEventListener('select', holdSelect, { capture: true });
        field.setSelectionRange(start, end);
        window.removeEventListener('select', holdSelect, { capture: true });
    };

    it('reads the range its selection spans', () => {
        field.setSelectionRange(4, 12);

        expect(watch().first).toEqual({ start: 4, end: 12 });
    });

    it('publishes a range the reader selects', () => {
        watch();

        select(0, 3);

        expect(publish).toHaveBeenCalledExactlyOnceWith({ start: 0, end: 3 });
    });

    it('publishes a caret moved within the field, as the field reports it', () => {
        watch();

        moveCaret(5, 5);
        expect(publish).not.toHaveBeenCalled();
        field.dispatchEvent(new Event('selectionchange'));

        expect(publish).toHaveBeenCalledExactlyOnceWith({ start: 5, end: 5 });
    });

    it('publishes a caret moved within the field it holds the focus of, as the document reports it', () => {
        field.focus();
        watch();

        moveCaret(5, 5);
        expect(publish).not.toHaveBeenCalled();
        document.dispatchEvent(new Event('selectionchange'));

        expect(publish).toHaveBeenCalledExactlyOnceWith({ start: 5, end: 5 });
    });

    it('takes no report of another field\'s selection', () => {
        const other = document.querySelector('#other');
        other.focus();
        watch();

        moveCaret(5, 5);
        other.setSelectionRange(1, 2);
        document.dispatchEvent(new Event('selectionchange'));

        expect(publish).not.toHaveBeenCalled();
    });

    it('publishes nothing for a range it already published', () => {
        watch();

        select(0, 3);
        select(0, 3);

        expect(publish).toHaveBeenCalledOnce();
    });

    it('publishes the range again once refreshed', () => {
        const { observer } = watch();

        observer.refresh();

        expect(publish).toHaveBeenCalledOnce();
    });

    it('publishes nothing once disposed', () => {
        watch().observer.dispose();

        select(0, 3);

        expect(publish).not.toHaveBeenCalled();
    });
});

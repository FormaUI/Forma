import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { TextEdits } from '#browser/text/TextEdits.mjs';
import { TextSources } from '#browser/text/TextSources.mjs';

const edits = [];

function follow(options = {}, owner = {}, field = document.querySelector('#field')) {
    // A field shows its owner's text as its render leaves it, unless the reader typed into it first.
    if (!field.value) {
        field.value = options.text ?? '';
    }

    const textEdits = new TextEdits(field, { sendsOnInput: true, text: '', revision: 0, ...options }, {
        name: 'owner o, registration 1.1',
        report: vi.fn().mockResolvedValue(undefined),
        predecessor: null,
        ...owner,
    });
    edits.push(textEdits);

    return textEdits;
}

// A reader types into the field holding the focus.
function type(value, field = document.querySelector('#field')) {
    field.focus();
    field.value = value;
    field.dispatchEvent(new Event('input', { bubbles: true }));
}

function commit(field = document.querySelector('#field')) {
    field.dispatchEvent(new Event('change', { bubbles: true }));
}

// The edits a report carried, each as the owner reads it.
function sent(report) {
    return report.mock.calls.map(([method, revision, ...rest]) => method === 'EditAsync'
        ? { revision, start: rest[0], removed: rest[1], inserted: rest[2].value, isCommit: rest[3] }
        : { replaces: revision, text: rest[0].value });
}

// Two characters past the Basic Multilingual Plane that share their low surrogate.
const grinning = String.fromCharCode(0xD83D, 0xDE00);
const sharingItsLow = String.fromCharCode(0xD83E, 0xDE00);
const grinningWithOpenMouth = String.fromCharCode(0xD83D, 0xDE03);

beforeEach(() => {
    document.body.innerHTML = '<textarea id="field"></textarea><input id="other"><bdi id="count">0</bdi>';
});

afterEach(() => {
    while (edits.length > 0) {
        edits.pop().dispose();
    }

    vi.restoreAllMocks();
});

describe('TextEdits', () => {
    describe('diff', () => {
        it('takes what two texts share at either end out of the edit', () => {
            expect(TextEdits.diff('Hello world', 'Hello brave world')).toEqual({ start: 6, removed: 0, inserted: 'brave ' });
        });

        it('replaces a run with what took its place', () => {
            expect(TextEdits.diff('Ada Byron', 'Ada Lovelace')).toEqual({ start: 4, removed: 5, inserted: 'Lovelace' });
        });

        it('takes a whole text from nothing', () => {
            expect(TextEdits.diff('', 'Notes')).toEqual({ start: 0, removed: 0, inserted: 'Notes' });
        });

        it('never parts a surrogate pair at the edit\'s start', () => {
            expect(TextEdits.diff(`a${grinning}`, `a${grinningWithOpenMouth}`)).toEqual({ start: 1, removed: 2, inserted: grinningWithOpenMouth });
        });

        it('never parts a surrogate pair at the edit\'s end', () => {
            expect(TextEdits.diff(`x${grinning}y`, `x${sharingItsLow}y`)).toEqual({ start: 1, removed: 2, inserted: sharingItsLow });
        });

        it('makes no edit of a text that stayed', () => {
            expect(TextEdits.diff('Same', 'Same')).toEqual({ start: 4, removed: 0, inserted: '' });
        });
    });

    describe('typing', () => {
        it('sends each edit as it is typed, revision by revision', () => {
            const report = vi.fn().mockResolvedValue(undefined);
            follow({}, { report });

            type('A');
            type('Ad');
            type('Ada');

            expect(sent(report)).toEqual([
                { revision: 1, start: 0, removed: 0, inserted: 'A', isCommit: false },
                { revision: 2, start: 1, removed: 0, inserted: 'd', isCommit: false },
                { revision: 3, start: 2, removed: 0, inserted: 'a', isCommit: false },
            ]);
        });

        it('sends the edits since its last commit as it commits, where it doesn\'t send each as it is typed', () => {
            const report = vi.fn().mockResolvedValue(undefined);
            follow({ sendsOnInput: false, text: 'Ada' }, { report });

            type('Ada L');
            type('Ada Lovelace');
            commit();

            expect(sent(report)).toEqual([{ revision: 1, start: 3, removed: 0, inserted: ' Lovelace', isCommit: true }]);
        });

        it('sends a commit that changed nothing as an empty edit', () => {
            const report = vi.fn().mockResolvedValue(undefined);
            follow({ text: 'Ada' }, { report });

            commit();

            expect(sent(report)).toEqual([{ revision: 1, start: 3, removed: 0, inserted: '', isCommit: true }]);
        });

        it('goes on from the revision its owner\'s text stands at', () => {
            const report = vi.fn().mockResolvedValue(undefined);
            follow({ text: 'Ada', revision: 7 }, { report });

            type('Ada!');

            expect(sent(report)).toEqual([{ revision: 8, start: 3, removed: 0, inserted: '!', isCommit: false }]);
        });

        it('sends nothing once disposed', () => {
            const report = vi.fn().mockResolvedValue(undefined);
            follow({}, { report }).dispose();

            type('After');

            expect(report).not.toHaveBeenCalled();
        });

        it('reports an edit that could not be delivered, naming its registration', async () => {
            const error = vi.spyOn(console, 'error').mockImplementation(() => {});
            follow({}, { report: vi.fn().mockRejectedValue(new Error('The circuit is down.')) });

            type('Lost');
            await Promise.resolve();
            await Promise.resolve();

            expect(error).toHaveBeenCalledExactlyOnceWith(expect.stringContaining('owner o, registration 1.1'), expect.any(Error));
        });
    });

    describe('attaching', () => {
        it('sends what the reader typed before it attached, keeping it in the field', () => {
            const report = vi.fn().mockResolvedValue(undefined);
            const field = document.querySelector('#field');
            field.value = 'Ada L';
            field.focus();

            follow({ text: 'Ada' }, { report });

            expect(field.value).toBe('Ada L');
            expect(sent(report)).toEqual([{ revision: 1, start: 3, removed: 0, inserted: ' L', isCommit: false }]);
        });

        it('leaves what the reader typed before it attached to the commit, where it doesn\'t send each edit as it is typed', () => {
            const report = vi.fn().mockResolvedValue(undefined);
            document.querySelector('#field').value = 'Ada L';
            document.querySelector('#field').focus();

            follow({ sendsOnInput: false, text: 'Ada' }, { report });
            commit();

            expect(sent(report)).toEqual([{ revision: 1, start: 3, removed: 0, inserted: ' L', isCommit: true }]);
        });

        it('goes on from where its predecessor left the same element, sending what was typed between them', () => {
            const report = vi.fn().mockResolvedValue(undefined);
            const predecessor = follow({ text: 'Ada', revision: 4 });
            type('Ada L');
            predecessor.dispose();
            type('Ada Lo');

            follow({ text: 'Ada', revision: 4 }, { report, predecessor });

            expect(sent(report)).toEqual([{ revision: 6, start: 5, removed: 0, inserted: 'o', isCommit: false }]);
        });

        it('commits what the reader typed before it attached where the reader has left the field', () => {
            const report = vi.fn().mockResolvedValue(undefined);
            document.querySelector('#field').value = 'Ada L';
            document.querySelector('#other').focus();

            follow({ sendsOnInput: false, text: 'Ada' }, { report });

            expect(sent(report)).toEqual([{ revision: 1, start: 3, removed: 0, inserted: ' L', isCommit: true }]);
        });

        it('gives an element drawn in its predecessor\'s place the text the reader left there, committing it since the focus went with the old', () => {
            const report = vi.fn().mockResolvedValue(undefined);
            const predecessor = follow({ sendsOnInput: false, text: 'Ada' });
            type('Ada Lovelace');
            const replacement = document.querySelector('#other');
            replacement.focus();
            replacement.blur();

            follow({ sendsOnInput: false, text: 'Ada' }, { report, predecessor }, replacement);

            expect(replacement.value).toBe('Ada Lovelace');
            expect(sent(report)).toEqual([{ revision: 1, start: 3, removed: 0, inserted: ' Lovelace', isCommit: true }]);
        });
    });

    describe('counting', () => {
        it('keeps the count of the field\'s characters as the reader types', () => {
            follow({ count: { output: 'count', maxLength: 280 } });

            type('Hello');

            expect(document.querySelector('#count').textContent).toBe('5 / 280');
        });

        it('counts a text of its owner\'s own as it writes it', () => {
            const textEdits = follow({ count: { output: 'count', maxLength: null } });
            type('Hello');

            textEdits.write('');

            expect(document.querySelector('#count').textContent).toBe('0');
        });

        it('stops counting once disposed', () => {
            follow({ count: { output: 'count', maxLength: null } }).dispose();

            type('After');

            expect(document.querySelector('#count').textContent).toBe('0');
        });
    });

    describe('commands', () => {
        it('writes a text of its owner\'s own, answering the revision it then stands at', () => {
            const report = vi.fn().mockResolvedValue(undefined);
            const textEdits = follow({ text: 'Ada', revision: 2 }, { report });

            const revision = textEdits.write('ADA');
            type('ADA!');

            expect(revision).toBe(3);
            expect(document.querySelector('#field').value).toBe('ADA!');
            expect(sent(report)).toEqual([{ revision: 4, start: 3, removed: 0, inserted: '!', isCommit: false }]);
        });

        it('sends its whole text where its owner\'s stands at another revision', () => {
            const report = vi.fn().mockResolvedValue(undefined);
            const textEdits = follow({}, { report });
            type('Ada');

            const isSent = textEdits.resend(0);

            expect(isSent).toBe(true);
            expect(sent(report).at(-1)).toEqual({ replaces: 1, text: 'Ada' });
        });

        it('sends nothing where its owner\'s text stands at its own revision', () => {
            const report = vi.fn().mockResolvedValue(undefined);
            const textEdits = follow({}, { report });
            type('Ada');

            expect(textEdits.resend(1)).toBe(false);
            expect(report).toHaveBeenCalledOnce();
        });

        it('commits what the field holds as its owner flushes it, answering the revision it then stands at', () => {
            const report = vi.fn().mockResolvedValue(undefined);
            const textEdits = follow({ sendsOnInput: false, text: 'Ada' }, { report });
            document.querySelector('#field').value = 'Ada L';

            const revision = textEdits.flush();

            expect(revision).toBe(1);
            expect(sent(report)).toEqual([{ revision: 1, start: 3, removed: 0, inserted: ' L', isCommit: true }]);
        });

        it('commits the edits it sent as they were typed as its owner flushes it', () => {
            const report = vi.fn().mockResolvedValue(undefined);
            const textEdits = follow({ text: 'Ada' }, { report });
            type('Ada L');

            textEdits.flush();

            expect(sent(report).at(-1)).toEqual({ revision: 2, start: 5, removed: 0, inserted: '', isCommit: true });
        });

        it('sends nothing as its owner flushes it with every edit committed', () => {
            const report = vi.fn().mockResolvedValue(undefined);
            const textEdits = follow({ text: 'Ada' }, { report });

            expect(textEdits.flush()).toBe(0);
            expect(report).not.toHaveBeenCalled();
        });

        it('offers its owner writing, resending and flushing alone', () => {
            expect([...TextEdits.commands]).toEqual(['write', 'resend', 'flush']);
        });
    });

    describe('a source its script provides', () => {
        /** A canvas whose script owns its text, committing at its own pace. */
        const provideCanvas = () => {
            document.body.insertAdjacentHTML('beforeend', '<div id="canvas" contenteditable="true"></div>');
            const canvas = document.querySelector('#canvas');
            const source = {
                html: '',
                commit: null,
                commitsUnchanged: false,
                read() {
                    return this.html;
                },
                write(text) {
                    this.html = text;
                },
                holdsFocus: () => false,
                listen(onInput, onCommit, signal) {
                    this.commit = onCommit;
                    signal.addEventListener('abort', () => { this.commit = null; });
                },
            };
            const handle = TextSources.provide(canvas, source);

            return { canvas, source, handle };
        };

        it('sends what its source commits, as edits of the text it read last', () => {
            const { canvas, source } = provideCanvas();
            const report = vi.fn().mockResolvedValue(undefined);
            follow({ sendsOnInput: false }, { report }, canvas);

            source.html = '<p>Ada</p>';
            source.commit();
            source.html = '<p>Ada Lovelace</p>';
            source.commit();

            expect(sent(report)).toEqual([
                { revision: 1, start: 0, removed: 0, inserted: '<p>Ada</p>', isCommit: true },
                { revision: 2, start: 6, removed: 0, inserted: ' Lovelace', isCommit: true },
            ]);
        });

        it('sends nothing for a commit that left its source as it was sent', () => {
            const { canvas, source } = provideCanvas();
            const report = vi.fn().mockResolvedValue(undefined);
            follow({ sendsOnInput: false }, { report }, canvas);

            source.html = '<p>Ada</p>';
            source.commit();
            source.commit();

            expect(report).toHaveBeenCalledOnce();
        });

        it('writes a text of the owner through its source', () => {
            const { canvas, source } = provideCanvas();
            const textEdits = follow({ sendsOnInput: false }, {}, canvas);

            textEdits.write('<p>Notes</p>');

            expect(source.html).toBe('<p>Notes</p>');
        });

        it('hears nothing from its source once disposed', () => {
            const { canvas, source } = provideCanvas();
            follow({ sendsOnInput: false }, {}, canvas).dispose();

            expect(source.commit).toBeNull();
        });

        it('reads the value of a field once the source provided for the element is disposed', () => {
            const { handle } = provideCanvas();
            const field = document.querySelector('#field');
            const report = vi.fn().mockResolvedValue(undefined);
            handle.dispose();
            follow({}, { report }, field);

            type('Ada', field);

            expect(sent(report)).toEqual([{ revision: 1, start: 0, removed: 0, inserted: 'Ada', isCommit: false }]);
        });
    });
});

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { SubmissionHold } from '#browser/text/SubmissionHold.mjs';
import { TextEdits } from '#browser/text/TextEdits.mjs';

describe('SubmissionHold', () => {
    /** @type {HTMLFormElement} */
    let form;
    /** @type {Array<{ dispose(): void }>} */
    let handles;
    /** @type {Array<SubmitEvent>} */
    let submissions;

    const listener = event => {
        event.preventDefault();
        submissions.push(event);
    };

    beforeEach(() => {
        document.body.innerHTML = `
            <form id="form"><textarea id="field"></textarea><button id="save" type="submit">Save</button></form>
            <form id="other"><textarea id="elsewhere"></textarea><button type="submit">Send</button></form>`;
        form = document.querySelector('#form');
        handles = [];
        submissions = [];
        document.addEventListener('submit', listener);
    });

    afterEach(() => {
        document.removeEventListener('submit', listener);
        handles.forEach(handle => handle.dispose());
    });

    // A field's edits with one on its way until the returned function delivers it.
    const delivering = element => {
        let deliver;
        const edits = { element, isDelivering: true, delivered: new Promise(resolve => { deliver = resolve; }) };
        handles.push(SubmissionHold.track(edits));

        return () => {
            edits.isDelivering = false;
            deliver();
        };
    };

    const settle = () => new Promise(resolve => setTimeout(resolve));

    it('holds a submission while an edit of a field in the form is on its way, and makes it again by the same submitter once it arrives', async () => {
        const deliver = delivering(document.querySelector('#field'));

        form.requestSubmit(document.querySelector('#save'));
        const heldBack = submissions.length;
        deliver();
        await settle();

        expect(heldBack).toBe(0);
        expect(submissions).toHaveLength(1);
        expect(submissions[0].submitter).toBe(document.querySelector('#save'));
    });

    it('lets a submission through when no edit is on its way', () => {
        handles.push(SubmissionHold.track({ element: document.querySelector('#field'), isDelivering: false, delivered: Promise.resolve() }));

        form.requestSubmit();

        expect(submissions).toHaveLength(1);
    });

    it('lets a form through whose fields have nothing on its way, though another form’s do', () => {
        delivering(document.querySelector('#elsewhere'));

        form.requestSubmit();

        expect(submissions).toHaveLength(1);
    });

    it('holds nothing once the edits are no longer tracked', () => {
        delivering(document.querySelector('#field'));
        handles.splice(0).forEach(handle => handle.dispose());

        form.requestSubmit();

        expect(submissions).toHaveLength(1);
    });

    it('holds the submission of a field whose edit the owner has yet to take', async () => {
        let take;
        const field = document.querySelector('#field');
        const report = vi.fn(() => new Promise(resolve => { take = resolve; }));
        const edits = new TextEdits(field, { sendsOnInput: true, text: '', revision: 0 }, { name: 'owner o, registration 1.1', report, predecessor: null });
        handles.push(edits);
        field.value = 'Ada';
        field.dispatchEvent(new Event('input', { bubbles: true }));

        form.requestSubmit();
        const heldBack = submissions.length;
        take();
        await settle();

        expect(heldBack).toBe(0);
        expect(submissions).toHaveLength(1);
    });
});

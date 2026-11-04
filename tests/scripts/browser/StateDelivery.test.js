import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { StateDelivery } from '#browser/StateDelivery.mjs';

describe('StateDelivery', () => {
    /** @type {Array<{ state: unknown, resolve: () => void, reject: (error: unknown) => void }>} */
    let sent;
    /** @type {ReturnType<typeof vi.fn>} */
    let report;
    /** @type {StateDelivery} */
    let delivery;

    beforeEach(() => {
        vi.useFakeTimers();
        sent = [];
        report = vi.fn();
        delivery = new StateDelivery(
            state => new Promise((resolve, reject) => sent.push({ state, resolve, reject })),
            report);
    });

    afterEach(() => {
        vi.useRealTimers();
    });

    // Lets a settled send's continuation run.
    const settle = () => vi.advanceTimersByTimeAsync(0);

    it('holds every sample until a task after it opens', async () => {
        delivery.offer('first');
        delivery.open();

        expect(sent).toHaveLength(0);

        await settle();

        expect(sent.map(send => send.state)).toEqual(['first']);
    });

    it('folds what was measured before it opened into the latest sample', async () => {
        delivery.offer('first');
        delivery.offer('second');
        delivery.open();
        await settle();

        expect(sent.map(send => send.state)).toEqual(['second']);
    });

    it('keeps one sample in flight and only the latest waiting behind it', async () => {
        delivery.open();
        await settle();

        delivery.offer('first');
        delivery.offer('second');
        delivery.offer('third');
        expect(sent.map(send => send.state)).toEqual(['first']);

        sent[0].resolve();
        await settle();

        expect(sent.map(send => send.state)).toEqual(['first', 'third']);
    });

    it('reports a failed send and suspends delivery, keeping the sample', async () => {
        delivery.open();
        await settle();
        delivery.offer('first');

        const failure = new Error('The circuit dropped.');
        sent[0].reject(failure);
        await settle();
        delivery.offer('second');

        expect(report).toHaveBeenCalledExactlyOnceWith(failure);
        expect(delivery.isSuspended).toBe(true);
        expect(sent).toHaveLength(1);
    });

    it('sends the latest sample once resumed', async () => {
        delivery.open();
        await settle();
        delivery.offer('first');
        sent[0].reject(new Error('The circuit dropped.'));
        await settle();

        delivery.resume();

        expect(sent.map(send => send.state)).toEqual(['first', 'first']);
    });

    it('resumes with a sample newer than the one that failed', async () => {
        delivery.open();
        await settle();
        delivery.offer('first');
        delivery.offer('second');
        sent[0].reject(new Error('The circuit dropped.'));
        await settle();

        delivery.resume();

        expect(sent.map(send => send.state)).toEqual(['first', 'second']);
    });

    // A call made while the circuit was down is never answered, so resuming gives up on it.
    it('gives up on a send never answered once resumed, sending the latest sample', async () => {
        delivery.open();
        await settle();
        delivery.offer('lost');
        delivery.offer('latest');

        delivery.resume();

        expect(sent.map(send => send.state)).toEqual(['lost', 'latest']);
    });

    it('keeps the send in flight waiting when a send it gave up on is answered late', async () => {
        delivery.open();
        await settle();
        delivery.offer('lost');
        delivery.resume();
        delivery.offer('current');

        sent[0].resolve();
        await settle();
        delivery.offer('next');

        expect(sent.map(send => send.state)).toEqual(['lost', 'current']);
    });

    it('sends nothing once closed, the sample waiting included', async () => {
        delivery.open();
        await settle();
        delivery.offer('first');
        delivery.offer('second');

        delivery.close();
        sent[0].resolve();
        await settle();
        delivery.offer('third');

        expect(sent.map(send => send.state)).toEqual(['first']);
    });

    describe('delivering every state', () => {
        beforeEach(() => {
            delivery = new StateDelivery(
                state => new Promise((resolve, reject) => sent.push({ state, resolve, reject })),
                report,
                { deliversEveryState: true });
        });

        it('sends each sample as it is measured, in order, without waiting for the one before', async () => {
            delivery.open();
            await settle();

            delivery.offer('first');
            delivery.offer('second');
            delivery.offer('third');

            expect(sent.map(send => send.state)).toEqual(['first', 'second', 'third']);
        });

        it('sends every sample measured before it opened, in order', async () => {
            delivery.offer('first');
            delivery.offer('second');
            delivery.open();
            await settle();

            expect(sent.map(send => send.state)).toEqual(['first', 'second']);
        });

        it('suspends on a failed send and resumes with the latest sample alone', async () => {
            delivery.open();
            await settle();
            delivery.offer('first');
            delivery.offer('second');

            sent[0].reject(new Error('The circuit dropped.'));
            await settle();
            delivery.offer('third');
            delivery.resume();

            expect(sent.map(send => send.state)).toEqual(['first', 'second', 'third']);
            expect(delivery.isSuspended).toBe(false);
        });

        it('stays open when a send made before it resumed fails late', async () => {
            delivery.open();
            await settle();
            delivery.offer('lost');
            delivery.resume();

            sent[0].reject(new Error('The circuit dropped.'));
            await settle();
            delivery.offer('current');

            expect(sent.map(send => send.state)).toEqual(['lost', 'current']);
            expect(delivery.isSuspended).toBe(false);
        });
    });

    it('never opens once closed before its task came', async () => {
        delivery.offer('first');
        delivery.open();

        delivery.close();
        await settle();

        expect(sent).toHaveLength(0);
    });
});

import { describe, it, expect, beforeEach } from 'vitest';
import { SwitchCoordinator, SwitchRegistration } from '#coordination/SwitchCoordinator.js';

// A coordinator writing each value the document takes into `written`, its registrations' receivers recording each delivery.
function createCoordinator() {
    const written = [];
    const coordinator = new SwitchCoordinator({ write: value => written.push(value.name), toArguments: value => [value.name] });
    coordinator.start({ name: 'first' });

    return { coordinator, written };
}

function createRegistration(coordinator, identity = 'state', runtime = 'server') {
    const registration = coordinator.register(identity, () => new SwitchRegistration(coordinator, identity, runtime));
    const deliveries = [];
    registration.receiver = { invokeMethodAsync: (method, ...args) => { deliveries.push([method, ...args]); return Promise.resolve(); } };

    return { registration, deliveries };
}

// A stand-in for Blazor's API, announcing the enhanced page updates a check names.
class FakeBlazor extends EventTarget {
    announce(name) {
        this.dispatchEvent(new Event(name));
    }
}

describe('SwitchCoordinator', () => {
    let coordinator;
    let written;

    beforeEach(() => {
        ({ coordinator, written } = createCoordinator());
    });

    describe('accepting', () => {
        it('should number each accepted value after the page own, number zero', () => {
            // Act
            coordinator.accept({ name: 'second' });
            const accepted = coordinator.accept({ name: 'third' });

            // Assert
            expect(accepted).toEqual({ name: 'third', number: 2 });
            expect(written).toEqual(['second', 'third']);
        });

        it('should deliver each accepted value with the document and its number, but to the registrations awaiting their own', () => {
            // Arrange
            const { registration: requester, deliveries: requesterDeliveries } = createRegistration(coordinator, 'requester');
            const { deliveries } = createRegistration(coordinator, 'other');

            // Act
            coordinator.accept({ name: 'second' }, [requester]);

            // Assert
            expect(deliveries).toEqual([['ApplyAsync', 'second', coordinator.documentKey, 1]]);
            expect(requesterDeliveries).toEqual([]);
        });
    });

    describe('ordering', () => {
        it('should run each request once the one before it settled, whatever its outcome', async () => {
            // Arrange
            const order = [];
            let failFirst;
            const first = coordinator.enqueue(() => new Promise((_, reject) => failFirst = reject));

            // Act — the second waits while the first runs
            const second = coordinator.enqueue(() => order.push('second'));

            while (failFirst === undefined) {
                await Promise.resolve();
            }

            order.push('first fails');
            failFirst(new Error('refused'));

            // Assert
            await expect(first).rejects.toThrow('refused');
            await second;
            expect(order).toEqual(['first fails', 'second']);
        });

        it('should refuse a request or a landing made before one its registration already took', () => {
            // Arrange
            const { registration } = createRegistration(coordinator);

            // Act
            const taken = [registration.takeRequest(2), registration.takeRequest(1), registration.takeLanding(1), registration.takeLanding(1)];

            // Assert
            expect(taken).toEqual([true, false, true, false]);
        });
    });

    describe('page updates', () => {
        it('should hold a request and the document through a navigation until it lands, and read the landing before the hold ends', async () => {
            // Arrange
            const blazor = new FakeBlazor();
            const events = [];
            coordinator.track(blazor, { land: () => events.push(`landed held ${coordinator.isHeld}`), end: () => events.push('ended') });
            blazor.announce('enhancednavigationstart');

            // Act
            const request = coordinator.enqueue(() => events.push('request ran'));
            coordinator.accept({ name: 'during' });
            await Promise.resolve();
            const isRequestHeld = events.length === 0;
            blazor.announce('enhancedload');
            await request;

            // Assert
            expect(isRequestHeld).toBe(true);
            expect(events).toEqual(['landed held true', 'request ran']);
            expect(written).toEqual(['during', 'during']);
        });

        it('should set the accepted value again after any other page update', () => {
            // Arrange
            const blazor = new FakeBlazor();
            coordinator.track(blazor, { land: () => {}, end: () => {} });

            // Act
            blazor.announce('enhancedload');

            // Assert
            expect(written).toEqual(['first']);
        });

        it('should end the hold of a navigation that ends without landing, and say so', () => {
            // Arrange
            const blazor = new FakeBlazor();
            const events = [];
            coordinator.track(blazor, { land: () => events.push('landed'), end: () => events.push('ended') });
            blazor.announce('enhancednavigationstart');

            // Act
            blazor.announce('enhancednavigationend');

            // Assert
            expect(events).toEqual(['ended']);
            expect(coordinator.isHeld).toBe(false);
        });
    });

    describe('registrations', () => {
        it('should recover a state live registration and release every registration of a runtime the client closed', () => {
            // Arrange
            const { registration: circuit } = createRegistration(coordinator, 'circuit', 'server');
            const { registration: client } = createRegistration(coordinator, 'client', 'webassembly');

            // Act
            const recovered = coordinator.register('circuit', () => { throw new Error('a new registration was made'); });
            coordinator.releaseRuntime('server');

            // Assert
            expect(recovered).toBe(circuit);
            expect([circuit.isLive, client.isLive]).toEqual([false, true]);
        });
    });
});

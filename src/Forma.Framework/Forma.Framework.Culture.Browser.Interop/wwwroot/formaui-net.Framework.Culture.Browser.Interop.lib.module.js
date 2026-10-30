// Reads the page's culture marker and policy as the document loads, follows what the server serves to the document from
// the start, and releases the closed circuit's registrations when the client closes it.
import { initialize, releaseCircuit, track } from './scripts/CultureCoordinator.js';

export function beforeWebStart(options) {
    initialize();

    const circuit = options.circuit ??= {};

    (circuit.circuitHandlers ??= []).push({ onCircuitClosed: releaseCircuit });
}

export function afterWebStarted(blazor) {
    track(blazor);
}

// Reads the page's theme marker and its themes' stylesheets as the document loads, follows what the server serves to the
// document from the start, and releases the closed circuit's registrations when the client closes it.
import { initialize, releaseCircuit, track } from './scripts/ThemeCoordinator.js';

export function beforeWebStart(options) {
    initialize();

    const circuit = options.circuit ??= {};

    (circuit.circuitHandlers ??= []).push({ onCircuitClosed: releaseCircuit });
}

export function afterWebStarted(blazor) {
    track(blazor);
}

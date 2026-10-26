// Tracks what the server serves to the document from the start, and releases the document's owner when the client
// closes the circuit that owns it.
import { release, track } from './scripts/DocumentLanguage.js';

export function beforeWebStart(options) {
    const circuit = options.circuit ??= {};

    (circuit.circuitHandlers ??= []).push({ onCircuitClosed: release });
}

export function afterWebStarted(blazor) {
    track(blazor);
}

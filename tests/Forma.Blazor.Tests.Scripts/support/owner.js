import { vi } from 'vitest';

/** A stand-in for the .NET object reference a behavior reports back through. */
export function fakeOwner() {
    return { invokeMethodAsync: vi.fn().mockResolvedValue(undefined) };
}

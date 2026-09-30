/**
 * Owner — reports from a behavior to its .NET owner
 */

/** @typedef {{ invokeMethodAsync: (name: string, ...args: unknown[]) => Promise<unknown> }} Owner */

/**
 * Reports to the owner, discarding the call's rejection.
 * @param {Owner | null | undefined} owner
 * @param {string} method The name of the owner's JSInvokable method.
 * @param {...unknown} args The method's arguments.
 */
export function tell(owner, method, ...args) {
    owner?.invokeMethodAsync(method, ...args).catch(() => {});
}

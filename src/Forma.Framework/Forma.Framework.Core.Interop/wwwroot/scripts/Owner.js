/**
 * Owner — telling the .NET owner of a behavior what happened
 *
 * An owner is a DotNetObjectReference, and a report can land after the owner was disposed, in
 * which case the call rejects.
 */

/** @typedef {{ invokeMethodAsync: (name: string, ...args: unknown[]) => Promise<unknown> }} Owner */

/**
 * Reports to the owner, letting go of the rejection a disposed owner answers with.
 * @param {Owner | null | undefined} owner
 * @param {string} method The JSInvokable method's name.
 * @param {...unknown} args What the method is told.
 */
export function tell(owner, method, ...args) {
    owner?.invokeMethodAsync(method, ...args).catch(() => {});
}

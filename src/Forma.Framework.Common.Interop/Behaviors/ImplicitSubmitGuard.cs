namespace Forma.Framework.Common.Interop.Behaviors;

using Forma.Framework.Core.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Stops Enter from submitting the form around a control.</summary>
public sealed class ImplicitSubmitGuard(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/common/scripts/behaviors/ImplicitSubmitGuard.js")
{
    /// <summary>Attaches the guard to the control.</summary>
    public ValueTask<bool> AttachAsync(ElementReference target)
        => AttachCoreAsync(target);
}

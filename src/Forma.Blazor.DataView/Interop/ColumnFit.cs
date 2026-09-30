namespace Forma.Blazor.Components.DataView.Interop;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Tells an owner the width a table has and the widths its columns want, measured again whenever either changes.</summary>
internal sealed class ColumnFit(IJSEnvironment js)
    : JSInterop(js, "./_content/Forma.Blazor.DataView/scripts/ColumnFit.js")
{
    /// <summary>Attaches to <paramref name="table"/>; the <paramref name="owner"/> is told through <c>HandleColumnFitAsync</c> with a <see cref="ColumnFitReport"/> whenever the width it has or the widths its columns want change.</summary>
    /// <param name="table">The table, laid out inside the container that gives it its width.</param>
    /// <param name="columnAttribute">The attribute a header cell names its column by.</param>
    /// <param name="measuringClass">The class that lays the table out at its content's own width while it is measured.</param>
    /// <param name="owner">The owner told the report.</param>
    public ValueTask<bool> AttachAsync<TOwner>(ElementReference table, string columnAttribute, string measuringClass, DotNetObjectReference<TOwner> owner)
        where TOwner : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(columnAttribute);
        ArgumentException.ThrowIfNullOrWhiteSpace(measuringClass);
        ArgumentNullException.ThrowIfNull(owner);

        return AttachCoreAsync(table, new { owner, columnAttribute, measuringClass });
    }
}

namespace Forma.Blazor.Browser.Components.Checks.Services;

using Forma.Framework.Resources;

/// <summary>A resource provider resolving the checks' own <c>check:</c> addresses to the documents it holds, each already converted to markup.</summary>
public sealed class CheckResourceProvider : IResourceProvider
{
    private static readonly Dictionary<string, MarkupResourceInfo> Documents = new(StringComparer.Ordinal)
    {
        ["check:notes.md"] = new("check:notes.md", "<h1>Notes</h1><p>The first document.</p>") { Name = "notes.md" },
        ["check:todo.md"] = new("check:todo.md", "<h1>To Do</h1><p>The second document.</p>") { Name = "todo.md" },
    };

    /// <inheritdoc />
    public ValueTask<ResourceInfo?> GetResourceAsync(string source, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        return ValueTask.FromResult<ResourceInfo?>(Documents.GetValueOrDefault(source));
    }
}

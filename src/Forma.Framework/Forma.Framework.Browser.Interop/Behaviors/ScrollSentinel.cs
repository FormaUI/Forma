namespace Forma.Framework.Browser.Interop.Behaviors;

using Microsoft.AspNetCore.Components;

/// <summary>A sentinel watched for coming into view of <paramref name="Root"/>.</summary>
/// <param name="Root">The scrolling panel the sentinel is measured against; <see langword="null"/> is the viewport.</param>
public sealed record ScrollSentinel(ElementReference? Root = null);

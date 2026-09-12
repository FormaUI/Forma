namespace Forma.Framework.Common.Interop.Behaviors;

using Microsoft.AspNetCore.Components;

/// <summary>A sentinel watched for coming into view of <paramref name="Root"/>, the viewport when <see langword="null"/>.</summary>
/// <param name="Root">The scrolling panel the sentinel is measured against.</param>
public sealed record ScrollSentinel(ElementReference? Root = null);

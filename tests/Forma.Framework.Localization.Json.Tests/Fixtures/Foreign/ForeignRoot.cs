// The namespace departs from the folder on purpose, which is the layout under test.
#pragma warning disable IDE0130
namespace Contoso.Widgets;
#pragma warning restore IDE0130

/// <summary>A type whose namespace root, <c>Contoso.Widgets</c>, differs from its assembly's name.</summary>
public sealed class ForeignRoot;

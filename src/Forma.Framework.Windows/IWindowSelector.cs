namespace Forma.Framework.Windows;

using System.Diagnostics.CodeAnalysis;

/// <summary>Describes which windows a search or an operation applies to: a <see cref="WindowSelector"/> of criteria, or one of its standing selections.</summary>
[SuppressMessage("Design", "CA1040:Avoid empty interfaces", Justification = "A marker the workspace resolves by its implementation; the criteria record and the standing selections share no members.")]
public interface IWindowSelector;

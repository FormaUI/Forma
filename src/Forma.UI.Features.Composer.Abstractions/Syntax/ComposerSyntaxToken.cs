namespace Forma.UI.Features.Composer.Syntax;

/// <summary>One classified span of source code.</summary>
public readonly record struct ComposerSyntaxToken(int Start, int Length, string Scope);

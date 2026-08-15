namespace Forma.Framework.Localization.Linter.Tests.Support.Sources;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

/// <summary>Builds compilations a test can walk.</summary>
public static class CompilationTestFactory
{
    /// <summary>A compilation carrying no references, so no symbol in it binds to anything.</summary>
    /// <param name="sources">The C# sources to parse.</param>
    public static Compilation Unreferenced(params string[] sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        return CSharpCompilation.Create(
            "Scratch",
            sources.Select(source => CSharpSyntaxTree.ParseText(source)));
    }
}

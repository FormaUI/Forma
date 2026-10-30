namespace Forma.Analyzers.Tests;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Xunit;

public sealed class KeptMembersGeneratorTests
{
    private const string Kept = "System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)";

    private static (string Source, Compilation Output) Generate(string source)
    {
        var paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        var compilation = CSharpCompilation.Create(
            "Sample",
            [CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken)],
            paths.Select(path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        CSharpGeneratorDriver.Create(new KeptMembersGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out _, TestContext.Current.CancellationToken);
        var generated = output.SyntaxTrees.Skip(1).SingleOrDefault()?.ToString() ?? string.Empty;

        return (generated, output);
    }

    [Fact]
    public void Should_keep_each_annotated_type_and_each_type_derived_from_one_by_its_documentation_identifier()
    {
        // Arrange
        var source = $$"""
            namespace Forma.Sample
            {
                [{{Kept}}]
                public abstract record Binding;

                public sealed record KeyBinding : Binding;

                public sealed record Plain;

                public sealed class Wrapper
                {
                    [{{Kept}}]
                    private sealed record Payload;
                }
            }
            """;

        // Act
        var (generated, output) = Generate(source);

        // Assert
        Assert.Multiple(
            () => Assert.Contains("DynamicallyAccessedMemberTypes)515, \"Forma.Sample.Binding\", \"Sample\")]", generated, StringComparison.Ordinal),
            () => Assert.Contains("\"Forma.Sample.KeyBinding\", \"Sample\")]", generated, StringComparison.Ordinal),
            () => Assert.Contains("\"Forma.Sample.Wrapper.Payload\", \"Sample\")]", generated, StringComparison.Ordinal),
            () => Assert.DoesNotContain("Forma.Sample.Plain", generated, StringComparison.Ordinal),
            () => Assert.Contains("[global::System.Runtime.CompilerServices.ModuleInitializer]", generated, StringComparison.Ordinal),
            () => Assert.Empty(output.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)));
    }

    [Fact]
    public void Should_keep_what_an_annotated_interface_names_on_the_types_implementing_it()
    {
        // Arrange
        var source = $$"""
            namespace Forma.Sample
            {
                [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
                public interface IState;

                public sealed record State : IState;
            }
            """;

        // Act
        var (generated, _) = Generate(source);

        // Assert
        Assert.Contains("DynamicallyAccessedMemberTypes)512, \"Forma.Sample.State\", \"Sample\")]", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_generate_nothing_for_an_assembly_without_an_annotated_type()
    {
        // Act
        var (generated, _) = Generate("namespace Forma.Sample { public sealed record Plain; }");

        // Assert
        Assert.Empty(generated);
    }
}

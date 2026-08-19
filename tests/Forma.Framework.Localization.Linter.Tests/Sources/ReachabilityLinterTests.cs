namespace Forma.Framework.Localization.Linter.Tests.Sources;

using Forma.Framework.Localization.Linter.Projects;

public sealed class ReachabilityLinterTests
{
    private static readonly string ProjectPath = Path.Combine("C:", "Scratch", "Scratch.csproj");

    [Fact]
    public void Should_report_a_file_no_type_answers_to()
    {
        // Arrange
        var compilation = CompilationTestFactory.Unreferenced("""
            namespace Scratch.Components.Explorer;

            public sealed class FormaExplorer;
            """);
        var project = new LoadedProject(ProjectPath, compilation, false, false);
        var file = Path.Combine("C:", "Scratch", "Components", "Explorer", "FormaGallery.he.json");

        // Act
        var diagnostics = new ReachabilityLinter().Lint(project, [file]);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(LintCodes.UnreachableTranslation, diagnostic.Code);
        Assert.Contains("Scratch.Components.Explorer.FormaGallery", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_leave_a_file_a_type_still_answers_to_alone()
    {
        // Arrange
        var compilation = CompilationTestFactory.Unreferenced("""
            namespace Scratch.Components.Explorer;

            public sealed class FormaExplorer;
            """);
        var project = new LoadedProject(ProjectPath, compilation, false, false);
        var file = Path.Combine("C:", "Scratch", "Components", "Explorer", "FormaExplorer.he.json");

        // Act
        var diagnostics = new ReachabilityLinter().Lint(project, [file]);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Should_find_a_type_declared_at_the_project_root()
    {
        // Arrange
        var compilation = CompilationTestFactory.Unreferenced("""
            namespace Scratch;

            public sealed class ResourceFormatter;
            """);
        var project = new LoadedProject(ProjectPath, compilation, false, false);
        var file = Path.Combine("C:", "Scratch", "ResourceFormatter.he.json");

        // Act
        var diagnostics = new ReachabilityLinter().Lint(project, [file]);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Should_ignore_a_file_carrying_no_recognised_culture()
    {
        // Arrange
        var compilation = CompilationTestFactory.Unreferenced("namespace Scratch;");
        var project = new LoadedProject(ProjectPath, compilation, false, false);
        var file = Path.Combine("C:", "Scratch", "launchSettings.json");

        // Act
        var diagnostics = new ReachabilityLinter().Lint(project, [file]);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Should_leave_a_file_a_resources_path_shifts_alone()
    {
        // Arrange — the type sits at Fixtures, the file under Resources/Fixtures, which ResourcesPath inserts.
        var compilation = CompilationTestFactory.Unreferenced("""
            namespace Scratch.Fixtures;

            public sealed class PathScoped;
            """);
        var project = new LoadedProject(ProjectPath, compilation, false, false);
        var file = Path.Combine("C:", "Scratch", "Resources", "Fixtures", "PathScoped.he.json");

        // Act
        var diagnostics = new ReachabilityLinter().Lint(project, [file]);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Should_still_report_a_file_whose_tail_names_no_type()
    {
        // Arrange — shedding every leading segment must not turn an absent type into a match.
        var compilation = CompilationTestFactory.Unreferenced("""
            namespace Scratch.Fixtures;

            public sealed class PathScoped;
            """);
        var project = new LoadedProject(ProjectPath, compilation, false, false);
        var file = Path.Combine("C:", "Scratch", "Resources", "Fixtures", "Renamed.he.json");

        // Act
        var diagnostics = new ReachabilityLinter().Lint(project, [file]);

        // Assert
        Assert.Equal(LintCodes.UnreachableTranslation, Assert.Single(diagnostics).Code);
    }

    [Fact]
    public void Should_leave_a_file_named_for_a_generic_type_alone()
    {
        // Arrange — the file name drops the arity the type's metadata name carries.
        var compilation = CompilationTestFactory.Unreferenced("""
            namespace Scratch.Fixtures;

            public sealed class GenericArity<TValue>;
            """);
        var project = new LoadedProject(ProjectPath, compilation, false, false);
        var file = Path.Combine("C:", "Scratch", "Fixtures", "GenericArity.he.json");

        // Act
        var diagnostics = new ReachabilityLinter().Lint(project, [file]);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Should_leave_a_file_named_for_a_type_in_the_global_namespace_alone()
    {
        // Arrange
        var compilation = CompilationTestFactory.Unreferenced("public sealed class GlobalRoot;");
        var project = new LoadedProject(ProjectPath, compilation, false, false);
        var file = Path.Combine("C:", "Scratch", "Fixtures", "GlobalRoot.he.json");

        // Act
        var diagnostics = new ReachabilityLinter().Lint(project, [file]);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Should_read_the_root_namespace_rather_than_the_assembly_name()
    {
        // Arrange — an .Abstractions project embeds under the root its sibling shares, not its own name.
        var compilation = CompilationTestFactory.Unreferenced("""
            namespace Forma.UI.Features.Feedback;

            public sealed class ToastMessage;
            """);
        var project = new LoadedProject(ProjectPath, compilation, false, false, "Forma.UI.Features");
        var file = Path.Combine("C:", "Scratch", "Feedback", "ToastMessage.he.json");

        // Act
        var diagnostics = new ReachabilityLinter().Lint(project, [file]);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Should_report_nothing_when_the_project_owns_no_translation_files()
    {
        // Arrange
        var compilation = CompilationTestFactory.Unreferenced("namespace Scratch;");
        var project = new LoadedProject(ProjectPath, compilation, false, false);

        // Act
        var diagnostics = new ReachabilityLinter().Lint(project, []);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Should_report_nothing_when_the_project_failed_to_load()
    {
        // Arrange
        var project = new LoadedProject(ProjectPath, null, false, false);
        var file = Path.Combine("C:", "Scratch", "FormaGallery.he.json");

        // Act
        var diagnostics = new ReachabilityLinter().Lint(project, [file]);

        // Assert
        Assert.Empty(diagnostics);
    }
}

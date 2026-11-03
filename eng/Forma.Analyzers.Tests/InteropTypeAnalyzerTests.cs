namespace Forma.Analyzers.Tests;

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.JSInterop;

using Xunit;

public sealed class InteropTypeAnalyzerTests
{
    // A wrapper as Forma's interop assemblies declare one, and the annotations a payload takes.
    private const string Wrapper = """
        using System.Collections.Generic;
        using System.Diagnostics.CodeAnalysis;
        using System.Linq;
        using System.Threading.Tasks;
        using Microsoft.JSInterop;

        namespace Forma.Sample.Interop
        {
            public sealed class Module
            {
                public Task SendAsync(string identifier, params object?[] args) => Task.CompletedTask;
            }
        }

        namespace Forma.Sample
        {
            public sealed class Formatter
            {
                public string Format(string pattern, params object?[] values) => pattern;
            }
        }

        """;

    private const string Kept = "System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)";

    private const string PropertiesKept = "System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)";

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source)
    {
        var paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(IJSRuntime).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase);
        var compilation = CSharpCompilation.Create(
            "Sample",
            [CSharpSyntaxTree.ParseText(Wrapper + source, cancellationToken: TestContext.Current.CancellationToken)],
            paths.Select(path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        Assert.Empty(compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

        return await compilation.WithAnalyzers([new InteropTypeAnalyzer()]).GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);
    }

    private static string[] GetMessages(ImmutableArray<Diagnostic> diagnostics)
        => [.. diagnostics.Select(diagnostic => $"{diagnostic.Id}: {diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture)}")];

    public sealed class Arguments
    {
        [Fact]
        public async Task Should_report_an_anonymous_payload()
        {
            // Arrange
            const string Source = """
                namespace Forma.Sample.Interop
                {
                    public sealed class Caller
                    {
                        public Task CallAsync(Module module) => module.SendAsync("open", new { Modal = true });
                    }
                }
                """;

            // Act
            var diagnostics = await AnalyzeAsync(Source);

            // Assert
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("FORMA0002", diagnostic.Id);
        }

        [Fact]
        public async Task Should_report_a_named_payload_that_keeps_nothing()
        {
            // Arrange
            const string Source = """
                namespace Forma.Sample.Interop
                {
                    public sealed class Options
                    {
                        public bool Modal { get; init; }
                    }

                    public sealed class Caller
                    {
                        public Task CallAsync(Module module) => module.SendAsync("open", new Options());
                    }
                }
                """;

            // Act
            var diagnostics = await AnalyzeAsync(Source);

            // Assert
            Assert.Equal(
                ["FORMA0001: 'Options' crosses the JavaScript interop boundary, but trimming removes the members its serialization uses; annotate it with [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]"],
                GetMessages(diagnostics));
        }

        [Fact]
        public async Task Should_follow_a_payload_into_the_types_it_holds()
        {
            // Arrange
            const string Source = $$"""
                namespace Forma.Sample.Interop
                {
                    public sealed record Route { public string Key { get; init; } = ""; }

                    [{{Kept}}]
                    public sealed record Profile { public IReadOnlyList<Route> Routes { get; init; } = []; public Route? Fallback { get; init; } }

                    public sealed class Caller
                    {
                        public Task CallAsync(Module module) => module.SendAsync("register", new Profile());
                    }
                }
                """;

            // Act
            var diagnostics = await AnalyzeAsync(Source);

            // Assert
            Assert.Equal(
                ["FORMA0001: 'Route' crosses the JavaScript interop boundary as 'Profile.Routes', but trimming removes the members its serialization uses; annotate it with [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]"],
                GetMessages(diagnostics));
        }

        [Fact]
        public async Task Should_accept_payloads_that_keep_their_members_beside_the_framework_types_the_interop_writes()
        {
            // Arrange
            const string Source = $$"""
                namespace Forma.Sample.Interop
                {
                    public sealed class Owner;

                    [{{Kept}}]
                    public sealed record Route { public string Key { get; init; } = ""; public int[] Codes { get; init; } = []; }

                    [{{Kept}}]
                    public sealed record Profile { public IReadOnlyList<Route> Routes { get; init; } = []; public Dictionary<string, Route> Named { get; init; } = []; public DotNetObjectReference<Owner>? Owner { get; init; } public System.DayOfWeek Day { get; init; } }

                    public sealed class Caller
                    {
                        public Task CallAsync(Module module, DotNetObjectReference<Owner> owner) => module.SendAsync("register", owner, 3, "text", new Profile { Owner = owner, Day = System.DayOfWeek.Monday });
                    }
                }
                """;

            // Act
            var diagnostics = await AnalyzeAsync(Source);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task Should_take_an_interfaces_annotation_for_the_types_implementing_it()
        {
            // Arrange
            const string Source = $$"""
                namespace Forma.Sample.Interop
                {
                    [{{Kept}}]
                    public interface IPayload;

                    public sealed record Options : IPayload { public bool Modal { get; init; } }

                    public sealed class Caller
                    {
                        public Task CallAsync(Module module) => module.SendAsync("open", new Options { Modal = true });
                    }
                }
                """;

            // Act
            var diagnostics = await AnalyzeAsync(Source);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task Should_follow_a_polymorphic_payload_into_each_type_it_serializes_as()
        {
            // Arrange
            const string Source = $$"""
                namespace Forma.Sample.Interop
                {
                    public sealed record Chord { public string Key { get; init; } = ""; }

                    [{{Kept}}]
                    [System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "family")]
                    [System.Text.Json.Serialization.JsonDerivedType(typeof(KeyBinding), "keyboard")]
                    public abstract record Binding;

                    public sealed record KeyBinding : Binding { public Chord? Chord { get; init; } }

                    public sealed class Caller
                    {
                        public Task CallAsync(Module module, Binding binding) => module.SendAsync("bind", binding);
                    }
                }
                """;

            // Act
            var diagnostics = await AnalyzeAsync(Source);

            // Assert
            Assert.Equal(
                ["FORMA0001: 'Chord' crosses the JavaScript interop boundary as 'KeyBinding.Chord', but trimming removes the members its serialization uses; annotate it with [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]"],
                GetMessages(diagnostics));
        }

        [Fact]
        public async Task Should_leave_a_method_outside_the_interop_alone()
        {
            // Arrange
            const string Source = """
                namespace Forma.Sample
                {
                    public sealed class Options
                    {
                        public bool Modal { get; init; }
                    }

                    public sealed class Caller
                    {
                        public string Call(Formatter formatter) => formatter.Format("{0}", new Options());
                    }
                }
                """;

            // Act
            var diagnostics = await AnalyzeAsync(Source);

            // Assert
            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task Should_report_an_anonymous_object_passed_as_an_object_anywhere()
        {
            // Arrange
            const string Source = """
                namespace Forma.Sample
                {
                    public sealed class Caller
                    {
                        public System.Func<int, object> Describe { get; } = size => new { size };
                    }
                }
                """;

            // Act
            var diagnostics = await AnalyzeAsync(Source);

            // Assert
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("FORMA0002", diagnostic.Id);
        }
    }

    public sealed class Constructors
    {
        [Fact]
        public async Task Should_report_a_type_serialization_builds_through_its_constructors_parameter_names()
        {
            // Arrange
            const string Source = $$"""
                namespace Forma.Sample.Interop
                {
                    [{{Kept}}]
                    public sealed record TextRange(int Start, int End);

                    [{{Kept}}]
                    public sealed record Caret
                    {
                        public Caret(int at) => At = at;

                        public Caret()
                        {
                        }

                        public int At { get; init; }
                    }

                    public sealed class Caller
                    {
                        public ValueTask<TextRange> ReadAsync(IJSObjectReference module) => module.InvokeAsync<TextRange>("readSelection");

                        public ValueTask<Caret> ReadCaretAsync(IJSObjectReference module) => module.InvokeAsync<Caret>("readCaret");
                    }
                }
                """;

            // Act
            var diagnostics = await AnalyzeAsync(Source);

            // Assert
            Assert.Equal(
                ["FORMA0003: 'TextRange' crosses the JavaScript interop boundary, but its serialization matches its constructor by parameter names, which trimming removes; give it a public parameterless constructor and init properties"],
                GetMessages(diagnostics));
        }

        [Fact]
        public async Task Should_report_a_type_deriving_from_a_crossing_type_that_serialization_builds_through_its_constructors_parameter_names()
        {
            // Arrange — results sent as their base, each serialized as itself
            const string Source = $$"""
                namespace Forma.Sample
                {
                    [{{Kept}}]
                    public abstract record DialogResult;

                    public sealed record LinkResult(string Href) : DialogResult;

                    public sealed record TableResult : DialogResult
                    {
                        public int Rows { get; init; }
                    }
                }

                namespace Forma.Sample.Interop
                {
                    public sealed class Caller
                    {
                        public Task InsertAsync(Module module, DialogResult result) => module.SendAsync("insert", result);
                    }
                }
                """;

            // Act
            var diagnostics = await AnalyzeAsync(Source);

            // Assert
            Assert.Equal(
                ["FORMA0003: 'LinkResult' crosses the JavaScript interop boundary, but its serialization matches its constructor by parameter names, which trimming removes; give it a public parameterless constructor and init properties"],
                GetMessages(diagnostics));
        }
    }

    public sealed class Descriptions
    {
        [Fact]
        public async Task Should_report_an_anonymous_object_reached_through_a_conditional_or_a_list()
        {
            // Arrange
            const string Source = """
                namespace Forma.Sample
                {
                    public sealed class Session
                    {
                        public object? Describe(int[] tokens)
                            => tokens.Length == 0 ? null : new { registrations = tokens.Select(token => new { token }).ToList() };

                        public object DescribeAll(int[] tokens)
                            => tokens.Select(token => new { token }).ToList();
                    }
                }
                """;

            // Act
            var diagnostics = await AnalyzeAsync(Source);

            // Assert
            Assert.Equal(["FORMA0002", "FORMA0002"], diagnostics.Select(diagnostic => diagnostic.Id));
        }

        [Fact]
        public async Task Should_check_a_named_value_an_interop_namespace_passes_as_an_object()
        {
            // Arrange
            const string Source = """
                namespace Forma.Sample.Interop
                {
                    public sealed record Description { public bool Wraps { get; init; } }

                    public sealed class Capability
                    {
                        public System.Func<bool, object> Describe { get; } = wraps => new Description { Wraps = wraps };
                    }
                }
                """;

            // Act
            var diagnostics = await AnalyzeAsync(Source);

            // Assert
            Assert.Equal(
                ["FORMA0001: 'Description' crosses the JavaScript interop boundary, but trimming removes the members its serialization uses; annotate it with [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]"],
                GetMessages(diagnostics));
        }
    }

    public sealed class Delegates
    {
        [Fact]
        public async Task Should_check_what_a_method_handed_over_as_an_object_returning_delegate_returns()
        {
            // Arrange
            const string Source = """
                namespace Forma.Sample.Interop
                {
                    public sealed record Description { public bool Wraps { get; init; } }

                    public sealed class Capability
                    {
                        public System.Func<bool, object> Describe { get; } = DescribeWraps;

                        private static Description DescribeWraps(bool wraps) => new() { Wraps = wraps };
                    }
                }
                """;

            // Act
            var diagnostics = await AnalyzeAsync(Source);

            // Assert
            Assert.Equal(
                ["FORMA0001: 'Description' crosses the JavaScript interop boundary, but trimming removes the members its serialization uses; annotate it with [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]"],
                GetMessages(diagnostics));
        }
    }

    public sealed class Results
    {
        [Fact]
        public async Task Should_report_a_result_the_interop_writes_without_its_constructors()
        {
            // Arrange
            const string Source = $$"""
                namespace Forma.Sample.Interop
                {
                    [{{PropertiesKept}}]
                    public sealed record TextRange { public int Start { get; init; } public int End { get; init; } }

                    public sealed class Caller
                    {
                        public ValueTask<TextRange> ReadAsync(IJSObjectReference module) => module.InvokeAsync<TextRange>("readSelection");
                    }
                }
                """;

            // Act
            var diagnostics = await AnalyzeAsync(Source);

            // Assert
            Assert.Equal(
                ["FORMA0001: 'TextRange' crosses the JavaScript interop boundary, but trimming removes the members its serialization uses; annotate it with [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]"],
                GetMessages(diagnostics));
        }

        [Fact]
        public async Task Should_accept_a_result_that_keeps_what_its_serialization_writes()
        {
            // Arrange
            const string Source = $$"""
                namespace Forma.Sample.Interop
                {
                    [{{Kept}}]
                    public sealed record TextRange { public int Start { get; init; } public int End { get; init; } }

                    public sealed class Caller
                    {
                        public ValueTask<TextRange> ReadAsync(IJSObjectReference module) => module.InvokeAsync<TextRange>("readSelection");
                    }
                }
                """;

            // Act
            var diagnostics = await AnalyzeAsync(Source);

            // Assert
            Assert.Empty(diagnostics);
        }
    }

    public sealed class Invokables
    {
        [Fact]
        public async Task Should_report_a_parameter_the_interop_writes_and_a_result_it_reads()
        {
            // Arrange
            const string Source = """
                namespace Forma.Sample
                {
                    public sealed record Action { public string Name { get; init; } = ""; }

                    public sealed record Outcome { public bool Handled { get; init; } }

                    public sealed class Owner
                    {
                        [JSInvokable]
                        public Task<Outcome> HandleAsync(Action action) => Task.FromResult(new Outcome { Handled = true });
                    }
                }
                """;

            // Act
            var diagnostics = await AnalyzeAsync(Source);

            // Assert
            Assert.Equal(
                [
                    "FORMA0001: 'Action' crosses the JavaScript interop boundary, but trimming removes the members its serialization uses; annotate it with [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]",
                    "FORMA0001: 'Outcome' crosses the JavaScript interop boundary, but trimming removes the members its serialization uses; annotate it with [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]",
                ],
                GetMessages(diagnostics).Order(StringComparer.Ordinal));
        }
    }
}

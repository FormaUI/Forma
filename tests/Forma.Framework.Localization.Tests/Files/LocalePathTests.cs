namespace Forma.Framework.Localization.Tests.Files;

using System.Diagnostics.CodeAnalysis;

public sealed class LocalePathTests
{
    private const string Beside = "$RootDir/$AnchorName.$Culture$Extension";
    private const string Inside = "$RootDir/$AnchorName/$CurrentName.$Culture$Extension";
    private const string NamedForAssembly = "$RootDir/$AssemblyName.$Culture$Extension";
    private const string InParentsDir = "$RootDir/$ParentName/$LocalesDir/$CurrentName.$Culture$Extension";
    private const string NamedInOwnDir = "$RootDir/$AnchorName/$LocalesDir/$CurrentName.$Culture$Extension";
    private const string CultureInOwnDir = "$RootDir/$AnchorName/$LocalesDir/$Culture$Extension";
    private const string CultureInHostDir = "$RootDir/$AnchorName/$Culture$Extension";
    private const string NamedInHostDir = "$RootDir/$ParentName/$CurrentName.$Culture$Extension";

    // Every form a config file might name, the defaults among them, so the binder and matcher agree on each.
    private static readonly string[] Forms = [Beside, Inside, NamedForAssembly, InParentsDir, NamedInOwnDir, CultureInOwnDir, CultureInHostDir, NamedInHostDir];

    private static readonly LocalePathValues Embedded = new("Contoso.Widgets", "Contoso.Widgets", "Contoso.Widgets", ".locales", "he", ".json");

    private static readonly LocalePathValues Borrowed = new("Contoso.Widgets.Extras", "Contoso.Widgets.Plugins", "Contoso.Widgets.Plugins", ".locales", "he", ".json");

    private static readonly LocalePathValues Unrooted = Embedded with { RootDir = string.Empty };

    public static TheoryData<string, string[], string?> EmbeddedNames => new()
    {
        { Beside, [], "Contoso.Widgets.he.json" },
        { Beside, ["Components", "Bars"], "Contoso.Widgets.Components.Bars.he.json" },
        { Beside, ["Components", "Bars", "Toolbar"], "Contoso.Widgets.Components.Bars.Toolbar.he.json" },
        { Inside, [], "Contoso.Widgets.he.json" },
        { Inside, ["Components", "Bars"], "Contoso.Widgets.Components.Bars.Bars.he.json" },
        { NamedForAssembly, [], "Contoso.Widgets.Contoso.Widgets.he.json" },
        { NamedForAssembly, ["Components"], null },
        { InParentsDir, [], null },
        { InParentsDir, ["Bars"], "Contoso.Widgets..locales.Bars.he.json" },
        { InParentsDir, ["Components", "Bars"], "Contoso.Widgets.Components..locales.Bars.he.json" },
        { NamedInOwnDir, [], "Contoso.Widgets..locales.he.json" },
        { NamedInOwnDir, ["Components", "Bars"], "Contoso.Widgets.Components.Bars..locales.Bars.he.json" },
        { CultureInOwnDir, [], "Contoso.Widgets..locales.he.json" },
        { CultureInOwnDir, ["Components", "Bars"], "Contoso.Widgets.Components.Bars..locales.he.json" },
    };

    public static TheoryData<string, string[]> Anchors
    {
        get
        {
            var data = new TheoryData<string, string[]>();
            foreach (var path in Forms)
            {
                string[][] anchors = [[], ["Bars"], ["Components", "Bars"], ["Components", "Bars", "Toolbar"]];
                foreach (var anchor in anchors)
                {
                    data.Add(path, anchor);
                }
            }

            return data;
        }
    }

    private static string? GetName(string path, string[] anchor, LocalePathValues values, char separator)
        => new LocalePathBinder(values, separator).GetName(LocalePath.Parse(path), anchor);

    private static bool TryGetAnchor(string path, string name, LocalePathValues values, char separator, [NotNullWhen(true)] out IReadOnlyList<string>? anchor)
        => new LocalePathMatcher(values, separator, StringComparison.Ordinal).TryGetAnchor(LocalePath.Parse(path), name, out anchor);

    public sealed class Binding
    {
        [Theory]
        [MemberData(nameof(EmbeddedNames), MemberType = typeof(LocalePathTests))]
        public void Should_name_an_embedded_file_for_every_default_form(string path, string[] anchor, string? expected)
            => Assert.Equal(expected, GetName(path, anchor, Embedded, '.'));

        [Theory]
        [InlineData(Beside, "Contoso.Widgets.he.json")]
        [InlineData(NamedForAssembly, "Contoso.Widgets/Contoso.Widgets.he.json")]
        [InlineData(CultureInOwnDir, "Contoso.Widgets/.locales/he.json")]
        public void Should_name_a_host_file_at_the_root_as_a_path(string path, string expected)
            => Assert.Equal(expected, GetName(path, [], Embedded, Path.AltDirectorySeparatorChar));

        [Fact]
        public void Should_name_a_host_file_below_the_root_as_a_path()
            => Assert.Equal("Contoso.Widgets/Components/.locales/Bars.he.json", GetName(InParentsDir, ["Components", "Bars"], Embedded, Path.AltDirectorySeparatorChar));

        [Fact]
        public void Should_name_a_file_for_an_assembly_whose_root_namespace_is_another()
            => Assert.Equal("Contoso.Widgets.Plugins.Contoso.Widgets.Extras.he.json", GetName(NamedForAssembly, [], Borrowed, '.'));

        [Fact]
        public void Should_leave_the_current_name_empty_at_the_root()
            => Assert.Equal(GetName(Beside, [], Embedded, '.'), GetName(Inside, [], Embedded, '.'));

        [Fact]
        public void Should_write_a_nested_locales_dir_with_the_separator_in_use()
            => Assert.Equal("Contoso.Widgets.Bars.i18n.strings.he.json", GetName(CultureInOwnDir, ["Bars"], Embedded with { LocalesDir = "i18n/strings" }, '.'));

        [Fact]
        public void Should_read_every_other_token_from_the_values()
            => Assert.Equal(
                "Contoso.Widgets.Plugins/Contoso.Widgets.Extras/Bars/he.json",
                GetName("$RootNamespace/$AssemblyName/$AnchorName/$Culture$Extension", ["Bars"], Borrowed, Path.AltDirectorySeparatorChar));

        [Fact]
        public void Should_start_a_name_below_an_unknown_root_without_a_separator()
            => Assert.Equal("Contoso.Widgets.Bars.he.json", GetName(Beside, ["Contoso", "Widgets", "Bars"], Unrooted, '.'));

        [Theory]
        [InlineData(Beside)]
        [InlineData(Inside)]
        [InlineData(NamedForAssembly)]
        [InlineData(CultureInOwnDir)]
        public void Should_give_no_root_file_where_the_root_is_unknown(string path)
            => Assert.Null(GetName(path, [], Unrooted, '.'));

        [Fact]
        public void Should_give_a_path_standing_for_the_root_no_name_below_it()
        {
            // Arrange
            const string RootOnlyPath = "$RootDir/$LocalesDir/$Culture$Extension";

            // Assert
            Assert.Multiple(
                () => Assert.True(LocalePath.Parse(RootOnlyPath).IsRootOnly),
                () => Assert.Equal("Contoso.Widgets/.locales/he.json", GetName(RootOnlyPath, [], Embedded, Path.AltDirectorySeparatorChar)),
                () => Assert.Null(GetName(RootOnlyPath, ["Bars"], Embedded, Path.AltDirectorySeparatorChar)));
        }

        [Fact]
        public void Should_drop_the_separator_after_an_empty_leading_token()
            => Assert.Equal(".locales/he.json", GetName("$AnchorName/$LocalesDir/$Culture$Extension", [], Embedded, Path.AltDirectorySeparatorChar));

        [Fact]
        public void Should_write_an_escaped_marker_as_a_dollar()
            => Assert.Equal("Contoso.Widgets/$strings/Bars.he.json", GetName("$RootDir/$$strings/$AnchorName.$Culture$Extension", ["Bars"], Embedded, Path.AltDirectorySeparatorChar));
    }

    public sealed class Matching
    {
        [Theory]
        [MemberData(nameof(Anchors), MemberType = typeof(LocalePathTests))]
        public void Should_read_back_the_anchor_every_default_path_names_as_a_resource(string text, string[] anchor)
            => AssertReadsBack(text, anchor, Embedded, '.');

        [Theory]
        [MemberData(nameof(Anchors), MemberType = typeof(LocalePathTests))]
        public void Should_read_back_the_anchor_every_default_path_names_as_a_path(string text, string[] anchor)
            => AssertReadsBack(text, anchor, Embedded, Path.AltDirectorySeparatorChar);

        [Theory]
        [MemberData(nameof(Anchors), MemberType = typeof(LocalePathTests))]
        public void Should_read_back_the_anchor_under_an_assembly_whose_root_namespace_is_another(string text, string[] anchor)
            => AssertReadsBack(text, anchor, Borrowed, '.');

        [Theory]
        [MemberData(nameof(Anchors), MemberType = typeof(LocalePathTests))]
        public void Should_read_back_the_anchor_below_an_unknown_root(string text, string[] anchor)
            => AssertReadsBack(text, anchor, Unrooted, '.');

        [Theory]
        [InlineData("$RootDir/$CurrentName/$AnchorName.$Culture$Extension", "")]
        [InlineData("$RootDir/$CurrentName/$AnchorName.$Culture$Extension", "Components.Bars")]
        [InlineData("$AnchorName/$LocalesDir/$Culture$Extension", "")]
        [InlineData("$AnchorName/$LocalesDir/$Culture$Extension", "Bars")]
        [InlineData("$RootDir/$$strings/$AnchorName.$Culture$Extension", "Bars")]
        [InlineData("$RootDir/$LocalesDir/$Culture$Extension", "")]
        public void Should_read_back_the_anchor_of_a_path_written_in_another_order(string text, string anchor)
            => AssertReadsBack(text, anchor.Length == 0 ? [] : anchor.Split('.'), Embedded, Path.AltDirectorySeparatorChar);

        [Fact]
        public void Should_read_a_file_named_for_the_assembly_as_the_root()
        {
            // Act
            var isRead = TryGetAnchor(NamedForAssembly, "Contoso.Widgets.Plugins.Contoso.Widgets.Extras.he.json", Borrowed, '.', out var anchor);

            // Assert
            Assert.Multiple(
                () => Assert.True(isRead),
                () => Assert.Empty(anchor!));
        }

        [Fact]
        public void Should_not_read_a_file_named_for_its_folder_under_another_name()
            => Assert.False(TryGetAnchor(Inside, "Contoso.Widgets.Components.Bars.Toolbar.he.json", Embedded, '.', out _));

        [Fact]
        public void Should_not_read_a_stray_separator_where_the_current_name_is_empty()
            => Assert.False(TryGetAnchor(NamedInOwnDir, "Contoso.Widgets..locales..he.json", Embedded, '.', out _));

        [Fact]
        public void Should_not_read_a_file_named_for_another_culture()
            => Assert.False(TryGetAnchor(Beside, "Contoso.Widgets.Components.Bars.es.json", Embedded, '.', out _));

        [Fact]
        public void Should_not_read_a_file_outside_the_root()
            => Assert.False(TryGetAnchor(CultureInOwnDir, "Fabrikam.Bars..locales.he.json", Embedded, '.', out _));

        [Fact]
        public void Should_not_read_a_locales_dir_as_a_name_segment()
            => Assert.False(TryGetAnchor(Beside, "Contoso.Widgets.Bars..locales.he.json", Embedded, '.', out _));

        [Theory]
        [InlineData("Contoso.Widgets/Bars/.locales/he.json")]
        [InlineData("Contoso.Widgets/Bars/2024/he.json")]
        [InlineData("Contoso.Widgets/Bars/Old Bars/he.json")]
        public void Should_not_read_a_folder_that_is_not_part_of_a_name_as_a_segment_in_a_path(string name)
            => Assert.False(TryGetAnchor(CultureInHostDir, name, Embedded, Path.AltDirectorySeparatorChar, out _));

        [Theory]
        [InlineData("strings", "Contoso.Widgets/Bars/strings/he.json", '/')]
        [InlineData("strings", "Contoso.Widgets.Bars.strings.he.json", '.')]
        [InlineData("i18n/strings", "Contoso.Widgets/Bars/i18n/strings/he.json", '/')]
        public void Should_not_read_the_locales_dir_as_a_segment_whatever_it_is_named(string localesDir, string name, char separator)
        {
            // Arrange — a project may name its folder of locale files as a namespace segment could be named, and the folder still completes no name.
            var values = Embedded with { LocalesDir = localesDir };

            // Act
            var isReadThroughTheFolder = TryGetAnchor(CultureInHostDir, name, values, separator, out _);
            var isReadInTheFolder = TryGetAnchor(CultureInOwnDir, name, values, separator, out var anchor);

            // Assert
            Assert.Multiple(
                () => Assert.False(isReadThroughTheFolder),
                () => Assert.True(isReadInTheFolder),
                () => Assert.Equal(["Bars"], anchor!));
        }

        [Fact]
        public void Should_not_read_a_root_file_where_the_root_is_unknown()
            => Assert.False(TryGetAnchor("$RootDir/$LocalesDir/$Culture$Extension", ".locales/he.json", Unrooted, Path.AltDirectorySeparatorChar, out _));

        [Fact]
        public void Should_compare_a_name_as_asked()
        {
            // Arrange
            var path = LocalePath.Parse(CultureInOwnDir);
            const string Name = "contoso.widgets/Bars/.LOCALES/HE.JSON";

            // Assert
            Assert.Multiple(
                () => Assert.False(new LocalePathMatcher(Embedded, Path.AltDirectorySeparatorChar, StringComparison.Ordinal).TryGetAnchor(path, Name, out _)),
                () => Assert.True(new LocalePathMatcher(Embedded, Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase).TryGetAnchor(path, Name, out _)));
        }

        [Fact]
        public void Should_refuse_a_comparison_that_is_not_ordinal()
            => Assert.Throws<ArgumentOutOfRangeException>(() => new LocalePathMatcher(Embedded, Path.AltDirectorySeparatorChar, StringComparison.CurrentCulture));

        private static void AssertReadsBack(string text, string[] anchor, LocalePathValues values, char separator)
        {
            // Arrange
            var name = GetName(text, anchor, values, separator);

            // Act
            var isRead = name is not null
                && TryGetAnchor(text, name, values, separator, out var readAnchor)
                && readAnchor.SequenceEqual(anchor);

            // Assert
            Assert.True(name is null || isRead, $"'{name}' did not read back as [{string.Join(", ", anchor)}] under '{text}'.");
        }
    }

    public sealed class Defaults
    {
        [Fact]
        public void Should_read_a_folders_own_locales_folder_and_a_file_named_for_a_segment_by_default()
            => Assert.Equal([CultureInOwnDir, InParentsDir], LocalePath.Defaults.Select(path => path.Text));

        [Fact]
        public void Should_read_a_hosts_folder_as_every_source_does_and_then_as_its_locales_root()
            => Assert.Equal([CultureInOwnDir, InParentsDir, CultureInHostDir, NamedInHostDir], LocalePath.HostDefaults.Select(path => path.Text));
    }

    public sealed class Parsing
    {
        [Fact]
        public void Should_keep_the_path_as_written()
            => Assert.Equal(Inside, LocalePath.Parse(Inside).ToString());

        [Theory]
        [InlineData("", "empty")]
        [InlineData("$RootDir/$Folder.$Culture$Extension", "'$Folder'")]
        [InlineData("$RootDir/$AnchorName/$AnchorName.$Culture$Extension", "more than once")]
        [InlineData("$RootDir/$AnchorName.json", "'$Culture' and '$Extension'")]
        [InlineData("$RootDir/$CurrentName.$Culture$Extension", "part of an anchor")]
        [InlineData("$RootDir/$ParentName/$AnchorName.$Culture$Extension", "both")]
        public void Should_say_what_is_wrong_with_a_malformed_path(string text, string expected)
        {
            // Act
            var thrown = Assert.Throws<FormatException>(() => LocalePath.Parse(text));

            // Assert
            Assert.Contains(expected, thrown.Message, StringComparison.Ordinal);
        }
    }
}

namespace Forma.Framework.Localization.Tests.Files;

using System.Text;

public sealed class LocaleConfigTests
{
    private static LocaleConfig Read(string json)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        return LocaleConfig.Read(stream, LocaleConfig.FileName);
    }

    public sealed class Keys
    {
        [Fact]
        public void Should_read_every_key_the_runtime_owns()
        {
            // Act
            var settings = Read("""
                {
                  "rootNamespace": "Contoso.Widgets.Plugins",
                  "rootDir": "../../../Contoso.slnx",
                  "localesDir": "i18n/strings",
                  "paths": ["$RootDir/$AnchorName/$LocalesDir/$Culture$Extension", "$RootDir/$AnchorName.$Culture$Extension"]
                }
                """);

            // Assert
            Assert.Multiple(
                () => Assert.Equal("Contoso.Widgets.Plugins", settings.RootNamespace),
                () => Assert.Equal("../../../Contoso.slnx", settings.RootDir),
                () => Assert.Equal("i18n/strings", settings.LocalesDir),
                () => Assert.Equal(
                    ["$RootDir/$AnchorName/$LocalesDir/$Culture$Extension", "$RootDir/$AnchorName.$Culture$Extension"],
                    settings.Paths!.Select(path => path.Text)));
        }

        [Theory]
        [InlineData("./Contoso.csproj")]
        [InlineData("./Widgets/Contoso.csproj")]
        [InlineData("../../../Contoso.slnx")]
        [InlineData("strings")]
        [InlineData("..")]
        public void Should_read_a_relative_root_dir_as_written(string rootDir)
            => Assert.Equal(rootDir, Read($$"""{ "rootDir": "{{rootDir}}" }""").RootDir);

        [Fact]
        public void Should_leave_a_key_the_file_does_not_carry_unset()
            => Assert.Equal(LocaleConfig.Empty, Read("{}"));

        [Fact]
        public void Should_pass_over_the_linters_keys_and_its_comments()
        {
            // Act
            var settings = Read("""
                {
                  // The linter's own entries.
                  "ignore": { "src/**/Page.razor": ["LOC0209"] },
                  "exclude": ["*/Fixtures/*"],
                  "rootNamespace": "Contoso.Widgets",
                }
                """);

            // Assert
            Assert.Equal(new LocaleConfig("Contoso.Widgets", null, null, null), settings);
        }
    }

    public sealed class Refusal
    {
        [Theory]
        [InlineData("[]", "not a JSON object")]
        [InlineData("{", "not readable JSON")]
        [InlineData("""{ "rootNamespace": "A", "rootNamespace": "B" }""", "not readable JSON")]
        [InlineData("""{ "rootNamespace": 1 }""", "'rootNamespace'")]
        [InlineData("""{ "rootNamespace": "" }""", "'rootNamespace'")]
        [InlineData("""{ "rootNamespace": "Contoso..Widgets" }""", "not a namespace")]
        [InlineData("""{ "rootNamespace": "Contoso Widgets" }""", "not a namespace")]
        [InlineData("""{ "rootDir": "" }""", "'rootDir'")]
        [InlineData("""{ "rootDir": "/strings" }""", "relative path")]
        [InlineData("""{ "rootDir": "C:/strings" }""", "relative path")]
        [InlineData("""{ "rootDir": "strings\\here" }""", "relative path")]
        [InlineData("""{ "rootDir": "strings//here" }""", "relative path")]
        [InlineData("""{ "localesDir": "../strings" }""", "relative folder")]
        [InlineData("""{ "localesDir": "i18n\\strings" }""", "relative folder")]
        [InlineData("""{ "localesDir": "/strings" }""", "relative folder")]
        [InlineData("""{ "paths": "$RootDir/$AnchorName.$Culture$Extension" }""", "list of paths")]
        [InlineData("""{ "paths": [] }""", "list of paths")]
        [InlineData("""{ "paths": [1] }""", "'paths'")]
        [InlineData("""{ "paths": ["$RootDir/$Folder.$Culture$Extension"] }""", "'$Folder'")]
        public void Should_say_what_is_wrong_with_a_key_the_runtime_owns(string json, string expected)
        {
            // Act
            var thrown = Assert.Throws<LocaleConfigException>(() => Read(json));

            // Assert
            Assert.Multiple(
                () => Assert.Contains(LocaleConfig.FileName, thrown.Message, StringComparison.Ordinal),
                () => Assert.Contains(expected, thrown.Message, StringComparison.Ordinal));
        }

        [Fact]
        public void Should_leave_a_malformed_linter_key_to_the_linter()
            => Assert.Equal(LocaleConfig.Empty, Read("""{ "ignore": 1, "exclude": "everything" }"""));
    }

    public sealed class Rules
    {
        [Theory]
        [InlineData(LocaleConfig.RootNamespaceKey, "Contoso.Widgets")]
        [InlineData(LocaleConfig.RootDirKey, "./Contoso.csproj")]
        [InlineData(LocaleConfig.LocalesDirKey, "i18n/strings")]
        public void Should_accept_a_value_a_key_owns(string key, string value)
        {
            // Act
            var isAccepted = LocaleConfig.Accepts(key, value, out var problem);

            // Assert
            Assert.Multiple(
                () => Assert.True(isAccepted),
                () => Assert.Null(problem));
        }

        [Theory]
        [InlineData(LocaleConfig.RootNamespaceKey, "", "names nothing")]
        [InlineData(LocaleConfig.RootNamespaceKey, "Contoso..Widgets", "not a namespace")]
        [InlineData(LocaleConfig.RootDirKey, "/strings", "relative path")]
        [InlineData(LocaleConfig.LocalesDirKey, "../strings", "relative folder")]
        public void Should_say_what_is_wrong_with_a_value_a_key_refuses(string key, string value, string expected)
        {
            // Act
            var isAccepted = LocaleConfig.Accepts(key, value, out var problem);

            // Assert
            Assert.Multiple(
                () => Assert.False(isAccepted),
                () => Assert.Contains(key, problem!, StringComparison.Ordinal),
                () => Assert.Contains(expected, problem!, StringComparison.Ordinal));
        }

        [Theory]
        [InlineData("ignore")]
        [InlineData(LocaleConfig.PathsKey)]
        public void Should_accept_any_text_under_a_key_whose_value_is_not_text(string key)
            => Assert.True(LocaleConfig.Accepts(key, "anything", out _));

        [Fact]
        public void Should_name_the_default_dir_of_locale_files_with_a_leading_dot()
            => Assert.Equal(".locales", LocaleConfig.DefaultLocalesDir);

        [Fact]
        public void Should_list_the_keys_the_runtime_reads()
            => Assert.Equal(
                [LocaleConfig.RootNamespaceKey, LocaleConfig.RootDirKey, LocaleConfig.LocalesDirKey, LocaleConfig.PathsKey],
                LocaleConfig.Keys);
    }
}

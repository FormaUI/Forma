namespace Forma.Framework.Localization.Json.Tests;

using Forma.Tests.Localization;

// A file in any layout or source, read into the one entry he · Contoso.App · Contoso.App.Uploads.UploadStatus · "Saved" → "נשמר".
public sealed class CanonicalFormTests
{
    private const string Type = "Contoso.App.Uploads.UploadStatus";
    private const string Marker = "\"$localization\": \"SchemaVersion: 1.0.0; Culture: he;\"";
    private const string Section = "{ " + Marker + ", \"UploadStatus\": { \"Saved\": \"נשמר\" } }";
    private const string Own = "{ " + Marker + ", \"Saved\": \"נשמר\" }";
    private const string Nested = "{ " + Marker + ", \"Uploads\": { \"UploadStatus\": { \"Saved\": \"נשמר\" } } }";

    private static readonly JsonLocaleFileFormat Format = new();

    // The resource name the build gives a file at a path in a project.
    private static string GetResourceName(string rootNamespace, string path)
        => $"{rootNamespace}/{path}";

    private static (string Key, string? Value) GetSoleEntry(IReadOnlyList<LocaleSourceEntry> entries)
    {
        var entry = Assert.Single(entries);

        return (entry.Key, entry.Value);
    }

    public sealed class Layouts
    {
        [Theory]
        [InlineData("Uploads/.locales/he.json", Section)]
        [InlineData("Uploads/.locales/UploadStatus.he.json", Own)]
        [InlineData("Uploads.he.json", Section)]
        [InlineData(".locales/he/Uploads.json", Section)]
        [InlineData(".locales/he/Uploads/UploadStatus.json", Own)]
        [InlineData(".locales/he.json", Nested)]
        [InlineData(".locales/he/he.json", Nested)]
        [InlineData(".locales/he/Uploads.he.json", Section)]
        [InlineData(".locales/Uploads.he.json", Section)]
        public void Should_read_a_file_in_any_layout_of_the_assembly_into_the_same_entry(string path, string content)
        {
            // Arrange
            var application = new FakeAssembly("Contoso.App") { Resources = { [GetResourceName("Contoso.App", path)] = content } };

            // Act
            var entry = GetSoleEntry(new EmbeddedLocaleSource(application, Format).Read("Contoso.App", Type, "he"));

            // Assert
            Assert.Equal(("Saved", "נשמר"), entry);
        }

        [Theory]
        [InlineData("strings-he/Uploads.json", Section)]
        [InlineData("strings-he/Uploads/UploadStatus.json", Own)]
        [InlineData("strings-he/Uploads.UploadStatus.json", Own)]
        public void Should_read_a_file_a_stated_path_places_into_the_same_entry(string path, string content)
        {
            // Arrange
            var application = new FakeAssembly("Contoso.App")
            {
                Resources =
                {
                    ["Contoso.App/.locale.json"] = """{ "paths": ["$RootDir/strings-$Culture/$AnchorName$Extension"] }""",
                    [GetResourceName("Contoso.App", path)] = content,
                },
            };

            // Act
            var entry = GetSoleEntry(new EmbeddedLocaleSource(application, Format).Read("Contoso.App", Type, "he"));

            // Assert
            Assert.Equal(("Saved", "נשמר"), entry);
        }
    }

    public sealed class Sources
    {
        [Fact]
        public void Should_read_a_pack_s_file_into_the_same_entry()
        {
            // Arrange
            var pack = new FakeAssembly("Contoso.Pack") { Resources = { ["Contoso.App.Uploads.he.json"] = Section } };

            // Act
            var entry = GetSoleEntry(new LocalePackSource(pack, Format, _ => null).Read("Contoso.App", Type, "he"));

            // Assert
            Assert.Equal(("Saved", "נשמר"), entry);
        }

        [Fact]
        public void Should_read_an_application_s_file_for_the_assembly_into_the_same_entry()
        {
            // Arrange
            var host = new FakeAssembly("Contoso.Host") { Resources = { [GetResourceName("Contoso.Host", ".locales/he/Contoso.App/Uploads.json")] = Section } };

            // Act
            var entry = GetSoleEntry(new ApplicationLocaleSource(host, Format, _ => null).Read("Contoso.App", Type, "he"));

            // Assert
            Assert.Equal(("Saved", "נשמר"), entry);
        }
    }
}

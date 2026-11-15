namespace Forma.Framework.Localization.Tests.Files;

using Forma.Framework.Localization.Files;

public sealed class LocaleFileNamesTests
{
    public sealed class Reading
    {
        [Theory]
        [InlineData(".locales/es/Accounts.Admin.json", "es", "Accounts.Admin", true)]
        [InlineData("Accounts/Admin/.locales/es.json", "es", "Accounts.Admin", true)]
        [InlineData("Accounts.Admin.es.json", "es", "Accounts.Admin", false)]
        [InlineData("es-419/Accounts.json", "es-419", "Accounts", false)]
        [InlineData(".locales/he.json", "he", "", true)]
        public void Should_read_the_culture_and_the_parts_a_name_stands_for(string name, string culture, string parts, bool isInLocalesDir)
        {
            // Act
            var isRead = LocaleFileNames.TryRead(name, ".locales", ".json", StringComparison.Ordinal, out var fileName);

            // Assert
            Assert.Multiple(
                () => Assert.True(isRead),
                () => Assert.Equal(culture, fileName?.Culture),
                () => Assert.Equal(parts, string.Join('.', fileName?.Parts ?? [])),
                () => Assert.Equal(isInLocalesDir, fileName?.IsInLocalesDir));
        }

        // Each resource name is the one the build gives the file whose path stands beside it, in a project whose root namespace is
        // Contoso.App, so every layout of one namespace's or one type's file reads into one canonical name.
        [Theory]
        [InlineData("Contoso.App.Uploads..locales.he.json", "he", "Contoso.App.Uploads")]                                 // Uploads/.locales/he.json
        [InlineData("Contoso.App.Uploads.he.json", "he", "Contoso.App.Uploads")]                                          // Uploads.he.json
        [InlineData("Contoso.App..locales.he.Uploads.json", "he", "Contoso.App.Uploads")]                                 // .locales/he/Uploads.json
        [InlineData("Contoso.App..locales.Uploads.he.json", "he", "Contoso.App.Uploads")]                                 // .locales/Uploads/he.json
        [InlineData("Contoso.App.Uploads..locales.UploadStatus.he.json", "he", "Contoso.App.Uploads.UploadStatus")]       // Uploads/.locales/UploadStatus.he.json
        [InlineData("Contoso.App.Uploads.UploadStatus.he.json", "he", "Contoso.App.Uploads.UploadStatus")]                // Uploads/UploadStatus.he.json
        [InlineData("Contoso.App..locales.he.Uploads.UploadStatus.json", "he", "Contoso.App.Uploads.UploadStatus")]       // .locales/he/Uploads/UploadStatus.json
        [InlineData("Contoso.App..locales.he.json", "he", "Contoso.App")]                                                 // .locales/he.json
        [InlineData("Contoso.App..locales.es-419.Uploads.json", "es-419", "Contoso.App.Uploads")]                         // .locales/es-419/Uploads.json
        [InlineData("Contoso.App.Strings.he.Uploads.json", "he", "Contoso.App.Strings.Uploads")]                          // Strings/he/Uploads.json
        public void Should_read_every_layout_s_resource_name_into_one_canonical_name(string name, string culture, string parts)
        {
            // Act
            var isRead = LocaleFileNames.TryRead(name, ".locales", ".json", StringComparison.Ordinal, out var fileName);

            // Assert
            Assert.Multiple(
                () => Assert.True(isRead),
                () => Assert.Equal(culture, fileName?.Culture),
                () => Assert.Equal(parts, string.Join('.', fileName?.Parts ?? [])));
        }

        [Theory]
        [InlineData("Accounts.Admin.json")]
        [InlineData("ES/Accounts.json")]
        [InlineData("Contoso.App..locales.ES.Uploads.json")]
        [InlineData("Accounts..es.json")]
        [InlineData("Accounts.es.txt")]
        public void Should_read_nothing_from_a_name_with_no_culture_an_empty_part_or_another_extension(string name)
        {
            // Act
            var isRead = LocaleFileNames.TryRead(name, ".locales", ".json", StringComparison.Ordinal, out var fileName);

            // Assert
            Assert.Multiple(
                () => Assert.False(isRead),
                () => Assert.Null(fileName));
        }

        [Theory]
        [InlineData("", ".json")]
        [InlineData(".locales", "")]
        public void Should_reject_an_empty_folder_name_or_extension(string localesDir, string extension)
            => Assert.Throws<ArgumentException>(() => LocaleFileNames.TryRead("Accounts.es.json", localesDir, extension, StringComparison.Ordinal, out _));

        [Fact]
        public void Should_read_one_name_twice_into_equal_readings()
        {
            // Act
            LocaleFileNames.TryRead(".locales/es/Accounts.Admin.json", ".locales", ".json", StringComparison.Ordinal, out var first);
            LocaleFileNames.TryRead(".locales/es/Accounts.Admin.json", ".locales", ".json", StringComparison.Ordinal, out var second);

            // Assert
            Assert.Multiple(
                () => Assert.Equal(first, second),
                () => Assert.Equal(first?.GetHashCode(), second?.GetHashCode()));
        }
    }

    public sealed class Cultures
    {
        [Theory]
        [InlineData("es", true)]
        [InlineData("es-419", true)]
        [InlineData("zh-Hant", true)]
        [InlineData("ES", false)]
        [InlineData("Admin", false)]
        [InlineData("xx", false)]
        public void Should_tell_a_culture_only_as_dotnet_writes_its_name(string part, bool isCulture)
            => Assert.Equal(isCulture, LocaleFileNames.IsCulture(part));
    }
}

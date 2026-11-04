namespace Forma.Framework.Localization.Integration.Tests;

using Forma.Framework.Localization.Integration.Tests.Support;

using Xunit;

public sealed class LocalizationTargetsTests
{
    private const string LocaleReferenceAttribute = "Forma.Framework.Localization.LocaleReferenceAttribute";

    // What a server built on the Web SDK and a WebAssembly client declare, which is how the targets know an application.
    private const string Server = "<UsingMicrosoftNETSdkWeb>true</UsingMicrosoftNETSdkWeb>";
    private const string Client = "<UsingMicrosoftNETSdkBlazorWebAssembly>true</UsingMicrosoftNETSdkBlazorWebAssembly>";

    public sealed class OwnFiles
    {
        [Fact]
        public async Task Should_embed_each_file_carrying_the_entry_once_under_the_name_its_path_forms()
        {
            // Arrange — a library's files in the default layout and one at a path its config would state
            using var projects = new FixtureProjects();
            projects.WriteProject("Contoso.Library");
            projects.WriteLocaleFile("Contoso.Library/.locales/he.json");
            projects.WriteLocaleFile("Contoso.Library/Components/.locales/he.json");
            projects.WriteLocaleFile("Contoso.Library/Components/.locales/Toolbar.he.json");
            projects.WriteLocaleFile("Contoso.Library/Strings/App.he.json");

            // Act
            var resources = await projects.GetEmbeddedResourcesAsync("Contoso.Library");

            // Assert
            Assert.Equal(
                [
                    (".locales/he.json", "Contoso.Library..locales.he.json", "false"),
                    ("Components/.locales/Toolbar.he.json", "Contoso.Library.Components..locales.Toolbar.he.json", "false"),
                    ("Components/.locales/he.json", "Contoso.Library.Components..locales.he.json", "false"),
                    ("Strings/App.he.json", "Contoso.Library.Strings.App.he.json", "false"),
                ],
                resources.OrderBy(resource => resource.Path, StringComparer.Ordinal));
        }

        [Fact]
        public async Task Should_leave_a_file_the_project_embeds_itself_to_its_own_item()
        {
            // Arrange
            using var projects = new FixtureProjects();
            projects.WriteProject("Contoso.Library", items: """<EmbeddedResource Include=".locales\he.json" LogicalName="Contoso.Strings.he.json" WithCulture="false" />""");
            projects.WriteLocaleFile("Contoso.Library/.locales/he.json");

            // Act
            var resources = await projects.GetEmbeddedResourcesAsync("Contoso.Library");

            // Assert
            Assert.Equal([(".locales/he.json", "Contoso.Strings.he.json", "false")], resources);
        }

        [Fact]
        public async Task Should_embed_each_file_once_where_a_project_s_own_item_and_a_host_s_folder_share_its_root_locales()
        {
            // Arrange — a client keeping its own root file and a host's folder in one .locales, with an item of its own for its own files
            using var projects = new FixtureProjects();
            projects.WriteProject("Contoso.Client", Client, """<EmbeddedResource Include="**\.locales\*.json" WithCulture="false" />""");
            projects.WriteLocaleFile("Contoso.Client/.locales/es.json", "es");
            projects.WriteLocaleFile("Contoso.Client/Layout/.locales/es.json", "es");
            projects.WriteLocaleFile("Contoso.Client/.locales/Forma.Blazor/Components/Bars/es.json", "es");

            // Act
            var resources = await projects.GetEmbeddedResourcesAsync("Contoso.Client");

            // Assert
            Assert.Equal(
                [
                    (".locales/Forma.Blazor/Components/Bars/es.json", ".locales.Forma.Blazor.Components.Bars.es.json", "false"),
                    (".locales/es.json", "Contoso.Client..locales.es.json", "false"),
                    ("Layout/.locales/es.json", "Contoso.Client.Layout..locales.es.json", "false"),
                ],
                resources.OrderBy(resource => resource.Path, StringComparer.Ordinal));
        }
    }

    public sealed class CopiedFiles
    {
        [Fact]
        public async Task Should_leave_a_file_the_project_copies_beside_its_output_to_its_own_item()
        {
            // Arrange — a folder of locale files the project copies beside its output, as a test reading a host's folder from disk does
            using var projects = new FixtureProjects();
            projects.WriteProject("Contoso.Library", items: """<Content Include="Hosts\**\*.json" CopyToOutputDirectory="PreserveNewest" />""");
            projects.WriteLocaleFile("Contoso.Library/Hosts/Contoso.Library/he.json");

            // Act
            var resources = await projects.GetEmbeddedResourcesAsync("Contoso.Library");
            var content = await projects.GetContentAsync("Contoso.Library");

            // Assert
            Assert.Multiple(
                () => Assert.Empty(resources),
                () => Assert.Equal(["Hosts/Contoso.Library/he.json"], content));
        }

        [Fact]
        public async Task Should_embed_a_server_s_own_file_its_sdk_takes_as_content_and_leave_it_out_of_the_content()
        {
            // Arrange — a file at a path a config states, which the Web SDK takes as content by default
            using var projects = new FixtureProjects();
            projects.WriteProject("Contoso.Server", sdk: "Microsoft.NET.Sdk.Web");
            projects.WriteLocaleFile("Contoso.Server/Strings/App.he.json");

            // Act
            var resources = await projects.GetEmbeddedResourcesAsync("Contoso.Server");
            var content = await projects.GetContentAsync("Contoso.Server");

            // Assert
            Assert.Multiple(
                () => Assert.Equal([("Strings/App.he.json", "Contoso.Server.Strings.App.he.json", "false")], resources),
                () => Assert.DoesNotContain("Strings/App.he.json", content));
        }
    }

    public sealed class HostFiles
    {
        [Fact]
        public async Task Should_embed_a_host_s_files_in_an_application_under_their_path_after_the_prefix()
        {
            // Arrange
            using var projects = new FixtureProjects();
            projects.WriteProject("Contoso.Server", Server);
            projects.WriteLocaleFile("Contoso.Server/.locales/Forma.Blazor/Components/Bars/he.json");
            projects.WriteLocaleFile("Contoso.Server/.locales/Forma.Blazor/he.json");

            // Act
            var resources = await projects.GetEmbeddedResourcesAsync("Contoso.Server");

            // Assert
            Assert.Equal(
                [
                    (".locales/Forma.Blazor/Components/Bars/he.json", ".locales.Forma.Blazor.Components.Bars.he.json", "false"),
                    (".locales/Forma.Blazor/he.json", ".locales.Forma.Blazor.he.json", "false"),
                ],
                resources.OrderBy(resource => resource.Path, StringComparer.Ordinal));
        }

        [Fact]
        public async Task Should_leave_a_host_s_folder_out_of_a_library()
        {
            // Arrange — a library's folder for another assembly, which only an application overrides
            using var projects = new FixtureProjects();
            projects.WriteProject("Contoso.Library");
            projects.WriteLocaleFile("Contoso.Library/.locales/Forma.Blazor/Components/Bars/he.json");

            // Act
            var resources = await projects.GetEmbeddedResourcesAsync("Contoso.Library");

            // Assert
            Assert.Empty(resources);
        }
    }

    public sealed class Exclusions
    {
        [Fact]
        public async Task Should_leave_out_json_without_the_entry_the_build_output_and_folders_whose_names_start_with_a_dot()
        {
            // Arrange — a locale file beside JSON that isn't one, and locale files where no locale file belongs
            using var projects = new FixtureProjects();
            projects.WriteProject("Contoso.Library");
            projects.WriteLocaleFile("Contoso.Library/.locales/he.json");
            projects.WriteFile("Contoso.Library/appsettings.json", """{ "Logging": { "LogLevel": { "Default": "Information" } } }""");
            projects.WriteLocaleFile("Contoso.Library/bin/Debug/he.json");
            projects.WriteLocaleFile("Contoso.Library/obj/he.json");
            projects.WriteLocaleFile("Contoso.Library/.vscode/he.json");
            projects.WriteLocaleFile("Contoso.Library/node_modules/contoso/.locales/he.json");

            // Act
            var resources = await projects.GetEmbeddedResourcesAsync("Contoso.Library");

            // Assert
            Assert.Equal([(".locales/he.json", "Contoso.Library..locales.he.json", "false")], resources);
        }
    }

    public sealed class Servers
    {
        [Fact]
        public async Task Should_record_the_client_a_server_references_whose_build_embeds_a_host_s_files()
        {
            // Arrange
            using var projects = new FixtureProjects();
            projects.WriteProject("Contoso.Client", Client);
            projects.WriteLocaleFile("Contoso.Client/.locales/Forma.Blazor/he.json");
            projects.WriteProject("Contoso.Server", Server, """<ProjectReference Include="..\Contoso.Client\Contoso.Client.csproj" />""");

            // Act
            var named = await projects.GetAssemblyAttributeArgumentsAsync("Contoso.Server", LocaleReferenceAttribute);

            // Assert
            Assert.Equal(["Contoso.Client"], named);
        }

        [Fact]
        public async Task Should_record_nothing_for_a_client_whose_build_embeds_no_host_s_files()
        {
            // Arrange — a client with its own strings alone
            using var projects = new FixtureProjects();
            projects.WriteProject("Contoso.Client", Client);
            projects.WriteLocaleFile("Contoso.Client/Components/.locales/he.json");
            projects.WriteProject("Contoso.Server", Server, """<ProjectReference Include="..\Contoso.Client\Contoso.Client.csproj" />""");

            // Act
            var named = await projects.GetAssemblyAttributeArgumentsAsync("Contoso.Server", LocaleReferenceAttribute);

            // Assert
            Assert.Empty(named);
        }
    }
}

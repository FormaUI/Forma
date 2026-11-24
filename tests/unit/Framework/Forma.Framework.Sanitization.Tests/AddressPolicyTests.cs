namespace Forma.Framework.Sanitization.Tests;

using Xunit;

public sealed class AddressPolicyTests
{
    // Addresses that run script, or open a document of their own, wherever they are written.
    public static TheoryData<string> HostileAddresses => new()
    {
        "javascript:alert(1)",
        "JaVaScRiPt:alert(1)",
        "  javascript:alert(1)",
        "\u0001\u001fjavascript:alert(1)",
        "\tjava\nscr\ript:alert(1)",
        "javascript\t:alert(1)",
        "vbscript:msgbox(1)",
        "VBScript:msgbox(1)",
        "data:text/html,<script>alert(1)</script>",
        "data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==",
        " DATA:text/html,<script>alert(1)</script>",
        "data:image/svg+xml,<svg onload=alert(1)>",
    };

    public static TheoryData<AddressKind> Kinds => new(Enum.GetValues<AddressKind>());

    public sealed class Allows
    {
        [Theory]
        [MemberData(nameof(HostileAddresses), MemberType = typeof(AddressPolicyTests))]
        public void Should_refuse_a_hostile_address_as_a_link(string address)
        {
            // Act
            var allowed = AddressPolicy.Allows(address, AddressKind.Link);

            // Assert
            Assert.False(allowed);
        }

        [Theory]
        [MemberData(nameof(HostileAddresses), MemberType = typeof(AddressPolicyTests))]
        public void Should_refuse_a_hostile_address_as_a_frame_source(string address)
        {
            // Act
            var allowed = AddressPolicy.Allows(address, AddressKind.FrameSource);

            // Assert
            Assert.False(allowed);
        }

        [Theory]
        [MemberData(nameof(HostileAddresses), MemberType = typeof(AddressPolicyTests))]
        public void Should_refuse_a_hostile_address_as_an_image_source(string address)
        {
            // Act
            var allowed = AddressPolicy.Allows(address, AddressKind.ImageSource);

            // Assert
            Assert.False(allowed);
        }

        [Theory]
        [InlineData("javascript:alert(1)")]
        [InlineData("JaVaScRiPt:alert(1)")]
        [InlineData("\u0001javascript:alert(1)")]
        [InlineData("vbscript:msgbox(1)")]
        [InlineData("file:///C:/report.pdf")]
        public void Should_refuse_a_script_or_a_file_whatever_the_kind(string address)
        {
            // Act
            var allowingKinds = Enum.GetValues<AddressKind>().Where(kind => AddressPolicy.Allows(address, kind)).ToList();

            // Assert
            Assert.Empty(allowingKinds);
        }

        [Theory]
        [MemberData(nameof(Kinds), MemberType = typeof(AddressPolicyTests))]
        public void Should_allow_a_relative_address_and_a_web_address_whatever_the_kind(AddressKind kind)
        {
            // Arrange
            string[] addresses =
            [
                "/media/podium.png",
                "media/podium.png",
                "../podium.png",
                "//cdn.example.com/podium.png",
                "#results",
                "?page=2",
                "",
                "http://example.com/podium.png",
                "HTTPS://example.com/podium.png",
                "  https://example.com/podium.png  ",
            ];

            // Act
            var refusedAddresses = addresses.Where(address => !AddressPolicy.Allows(address, kind)).ToList();

            // Assert
            Assert.Empty(refusedAddresses);
        }

        [Theory]
        [InlineData("mailto:editor@example.com")]
        [InlineData("tel:+573001234567")]
        public void Should_allow_a_mail_or_telephone_address_only_as_a_link(string address)
        {
            // Act
            var isLinkAllowed = AddressPolicy.Allows(address, AddressKind.Link);
            var isDownloadAllowed = AddressPolicy.Allows(address, AddressKind.DownloadLink);
            var isSourceAllowed = AddressPolicy.Allows(address, AddressKind.ImageSource);

            // Assert
            Assert.True(isLinkAllowed);
            Assert.True(isDownloadAllowed);
            Assert.False(isSourceAllowed);
        }

        [Theory]
        [InlineData("blob:https://example.com/8f4b0c2e")]
        [InlineData("data:text/csv,stage,rider")]
        [InlineData("data:application/pdf;base64,JVBERi0=")]
        public void Should_allow_a_blob_or_data_address_for_a_link_that_downloads_but_not_one_a_reader_follows(string address)
        {
            // Act
            var isDownloadAllowed = AddressPolicy.Allows(address, AddressKind.DownloadLink);
            var isLinkAllowed = AddressPolicy.Allows(address, AddressKind.Link);

            // Assert
            Assert.True(isDownloadAllowed);
            Assert.False(isLinkAllowed);
        }

        [Theory]
        [InlineData(AddressKind.ImageSource)]
        [InlineData(AddressKind.VideoSource)]
        [InlineData(AddressKind.AudioSource)]
        [InlineData(AddressKind.TrackSource)]
        [InlineData(AddressKind.FrameSource)]
        public void Should_allow_a_blob_address_as_any_source(AddressKind kind)
        {
            // Act
            var allowed = AddressPolicy.Allows("blob:https://example.com/8f4b0c2e", kind);

            // Assert
            Assert.True(allowed);
        }

        [Theory]
        [InlineData(AddressKind.ImageSource, "data:image/png;base64,iVBORw0KGgo=")]
        [InlineData(AddressKind.ImageSource, "data:IMAGE/WEBP;base64,UklGRg==")]
        [InlineData(AddressKind.ImageSource, "data: image/jpeg ;base64,/9j/")]
        [InlineData(AddressKind.VideoSource, "data:video/mp4;base64,AAAA")]
        [InlineData(AddressKind.AudioSource, "data:audio/mpeg;base64,AAAA")]
        [InlineData(AddressKind.TrackSource, "data:text/vtt,WEBVTT")]
        public void Should_allow_a_data_address_of_the_sources_own_kind(AddressKind kind, string address)
        {
            // Act
            var allowed = AddressPolicy.Allows(address, kind);

            // Assert
            Assert.True(allowed);
        }

        [Theory]
        [InlineData(AddressKind.ImageSource, "data:video/mp4;base64,AAAA")]
        [InlineData(AddressKind.ImageSource, "data:image/svg+xml;base64,PHN2Zz48L3N2Zz4=")]
        [InlineData(AddressKind.ImageSource, "data:image/png")]
        [InlineData(AddressKind.ImageSource, "data:,podium")]
        [InlineData(AddressKind.VideoSource, "data:image/png;base64,iVBORw0KGgo=")]
        [InlineData(AddressKind.VideoSource, "data:video/,AAAA")]
        [InlineData(AddressKind.AudioSource, "data:video/mp4;base64,AAAA")]
        [InlineData(AddressKind.TrackSource, "data:text/html,<script>alert(1)</script>")]
        [InlineData(AddressKind.FrameSource, "data:application/pdf;base64,JVBERi0=")]
        [InlineData(AddressKind.FrameSource, "data:image/png;base64,iVBORw0KGgo=")]
        public void Should_refuse_a_data_address_of_another_kind(AddressKind kind, string address)
        {
            // Act
            var allowed = AddressPolicy.Allows(address, kind);

            // Assert
            Assert.False(allowed);
        }

        [Theory]
        [InlineData("ftp://files.example.com/report.pdf")]
        [InlineData("ws://example.com/socket")]
        [InlineData("chrome://settings")]
        [InlineData("C:\\reports\\report.pdf")]
        public void Should_refuse_a_scheme_no_kind_names(string address)
        {
            // Act
            var allowed = AddressPolicy.Allows(address, AddressKind.Link);

            // Assert
            Assert.False(allowed);
        }

        [Theory]
        [InlineData("java script:alert(1)")]
        [InlineData("1javascript:alert(1)")]
        [InlineData("\u00a0javascript:alert(1)")]
        public void Should_allow_an_address_the_browser_reads_as_relative(string address)
        {
            // Act
            var allowed = AddressPolicy.Allows(address, AddressKind.Link);

            // Assert
            Assert.True(allowed);
        }
    }

    public sealed class CanRunScript
    {
        [Theory]
        [InlineData("javascript:alert(1)")]
        [InlineData("\u0001 java\tscript:alert(1)")]
        [InlineData("VBScript:msgbox(1)")]
        [InlineData("data:text/html,<script>alert(1)</script>")]
        [InlineData("data:image/svg+xml,<svg onload=alert(1)>")]
        [InlineData("data:application/javascript,alert(1)")]
        public void Should_tell_an_address_that_can_run_script(string address)
        {
            // Act
            var canRunScript = AddressPolicy.CanRunScript(address);

            // Assert
            Assert.True(canRunScript);
        }

        [Theory]
        [InlineData("https://example.com/podium.png")]
        [InlineData("/media/podium.png")]
        [InlineData("ftp://example.com/podium.png")]
        [InlineData("mailto:press@example.com")]
        [InlineData("blob:https://example.com/8f4b0c2e")]
        [InlineData("data:image/png;base64,AAAA")]
        [InlineData("data:video/mp4;base64,AAAA")]
        [InlineData("data:audio/mpeg;base64,AAAA")]
        [InlineData("data:text/vtt,WEBVTT")]
        public void Should_tell_an_address_that_runs_no_script(string address)
        {
            // Act
            var canRunScript = AddressPolicy.CanRunScript(address);

            // Assert
            Assert.False(canRunScript);
        }
    }

    public sealed class GetScheme
    {
        [Theory]
        [InlineData("JaVaScRiPt:alert(1)", "javascript")]
        [InlineData("\u0001 java\tscript:alert(1)", "javascript")]
        [InlineData("https://example.com", "https")]
        [InlineData("web+forma:stage-12", "web+forma")]
        public void Should_read_the_scheme_as_the_browser_reads_it(string address, string scheme)
        {
            // Act
            var readScheme = AddressPolicy.GetScheme(address);

            // Assert
            Assert.Equal(scheme, readScheme);
        }

        [Theory]
        [InlineData("/media/podium.png")]
        [InlineData("podium.png")]
        [InlineData("#results")]
        [InlineData("java script:alert(1)")]
        [InlineData("")]
        public void Should_read_no_scheme_from_a_relative_address(string address)
        {
            // Act
            var readScheme = AddressPolicy.GetScheme(address);

            // Assert
            Assert.Null(readScheme);
        }
    }
}

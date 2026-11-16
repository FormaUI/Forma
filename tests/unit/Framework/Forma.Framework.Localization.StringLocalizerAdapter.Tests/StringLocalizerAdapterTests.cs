namespace Forma.Framework.Localization.StringLocalizerAdapter.Tests;

using Microsoft.Extensions.DependencyInjection.Extensions;

public sealed class StringLocalizerAdapterTests
{
    private static ServiceProvider CreateServiceProvider()
        => new ServiceCollection().AddJsonLocalization().AddFormaStringLocalizerAdapter().BuildServiceProvider();

    private static IStringLocalizer CreateLocalizer(ServiceProvider serviceProvider)
        => serviceProvider.GetRequiredService<IStringLocalizer<Greeting>>();

    public sealed class Lookup
    {
        [Fact]
        public void Should_read_a_type_s_strings_in_the_ui_culture_of_each_lookup()
        {
            // Arrange
            using var serviceProvider = CreateServiceProvider();
            var localizer = CreateLocalizer(serviceProvider);
            using var cultures = new CultureFixture();

            // Act
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("he");
            var hebrew = localizer["Hello"];
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("es");
            var spanish = localizer["Hello"];

            // Assert
            Assert.Multiple(
                () => Assert.Equal("שלום", hebrew.Value),
                () => Assert.False(hebrew.ResourceNotFound),
                () => Assert.Equal("Hola", spanish.Value));
        }

        [Fact]
        public void Should_answer_a_missing_string_with_its_key_and_say_it_was_not_found()
        {
            // Arrange
            using var serviceProvider = CreateServiceProvider();
            using var cultures = new CultureFixture("he");

            // Act
            var text = CreateLocalizer(serviceProvider)["Goodbye"];

            // Assert
            Assert.Multiple(
                () => Assert.Equal("Goodbye", text.Value),
                () => Assert.True(text.ResourceNotFound));
        }

        [Fact]
        public void Should_list_the_ui_culture_s_strings_as_written()
        {
            // Arrange
            using var serviceProvider = CreateServiceProvider();
            using var cultures = new CultureFixture("he");

            // Act
            var strings = CreateLocalizer(serviceProvider).GetAllStrings(includeParentCultures: false).ToDictionary(text => text.Name, text => text.Value);

            // Assert
            Assert.Multiple(
                () => Assert.Equal("שלום", strings["Hello"]),
                () => Assert.Equal("שלום {0}", strings["Hello {0}"]));
        }

        [Fact]
        public void Should_create_the_same_localizer_by_type_and_by_base_name_and_location()
        {
            // Arrange
            using var serviceProvider = CreateServiceProvider();
            var factory = serviceProvider.GetRequiredService<IStringLocalizerFactory>();
            using var cultures = new CultureFixture("he");

            // Act
            var byType = factory.Create(typeof(Greeting))["Hello"];
            var byName = factory.Create(typeof(Greeting).FullName!, typeof(Greeting).Assembly.GetName().Name!)["Hello"];

            // Assert
            Assert.Multiple(
                () => Assert.Equal("שלום", byType.Value),
                () => Assert.Equal("שלום", byName.Value));
        }

        [Fact]
        public void Should_answer_with_each_key_where_no_locale_factory_is_registered()
        {
            // Arrange
            using var serviceProvider = new ServiceCollection().AddFormaStringLocalizerAdapter().BuildServiceProvider();
            using var cultures = new CultureFixture("he");

            // Act
            var text = CreateLocalizer(serviceProvider)["Hello"];

            // Assert
            Assert.Equal("Hello", text.Value);
        }
    }

    public sealed class Formatting
    {
        [Fact]
        public void Should_fill_a_composite_placeholder_in_the_formatting_culture()
        {
            // Arrange
            using var serviceProvider = CreateServiceProvider();
            using var cultures = new CultureFixture();
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("he");
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

            // Act
            var text = CreateLocalizer(serviceProvider)["Paid {0:N2}", 1234.5];

            // Assert
            Assert.Equal("שולם 1.234,50", text.Value);
        }

        [Fact]
        public void Should_resolve_a_composite_text_s_escaped_braces_once()
        {
            // Arrange
            using var serviceProvider = CreateServiceProvider();
            using var cultures = new CultureFixture("he");
            var localizer = CreateLocalizer(serviceProvider);

            // Act
            var filled = localizer["Use {{braces}} for {0}", "x"];
            var written = localizer["Use {{braces}} for {0}"];

            // Assert
            Assert.Multiple(
                () => Assert.Equal("השתמשו ב-{סוגריים} עבור x", filled.Value),
                () => Assert.Equal("השתמשו ב-{{סוגריים}} עבור {0}", written.Value));
        }

        [Fact]
        public void Should_return_a_composite_key_s_text_as_written_when_given_no_arguments()
        {
            // Arrange
            using var serviceProvider = CreateServiceProvider();
            using var cultures = new CultureFixture("he");

            // Act
            var text = CreateLocalizer(serviceProvider)["Hello {0}"];

            // Assert
            Assert.Equal("שלום {0}", text.Value);
        }

        [Fact]
        public void Should_fill_forma_holes_and_plural_forms()
        {
            // Arrange
            using var serviceProvider = CreateServiceProvider();
            using var cultures = new CultureFixture("he");
            var localizer = CreateLocalizer(serviceProvider);

            // Act
            var stored = localizer["Stored {Stored} of {Total}.", 3, 5];
            var files = localizer["[$ plural; one: # file; other: # files; $]", 2];

            // Assert
            Assert.Multiple(
                () => Assert.Equal("נשמרו 3 מתוך 5.", stored.Value),
                () => Assert.Equal("שני קבצים", files.Value));
        }

        [Fact]
        public void Should_follow_a_text_that_reorders_its_placeholders_by_index_or_by_name()
        {
            // Arrange
            using var serviceProvider = CreateServiceProvider();
            using var cultures = new CultureFixture("he");
            var localizer = CreateLocalizer(serviceProvider);

            // Act
            var byIndex = localizer["{0} of {1}", 3, 5];
            var byName = localizer["{Stored} of {Total}", 3, 5];

            // Assert
            Assert.Multiple(
                () => Assert.Equal("מתוך 5, 3", byIndex.Value),
                () => Assert.Equal("מתוך 5, 3", byName.Value));
        }

        [Fact]
        public void Should_fill_a_placeholder_used_twice_from_one_argument()
        {
            // Arrange
            using var serviceProvider = CreateServiceProvider();
            using var cultures = new CultureFixture("he");
            var localizer = CreateLocalizer(serviceProvider);

            // Act
            var byIndex = localizer["{0} and {0}", "a"];
            var byName = localizer["{Name} and {Name}", "a"];

            // Assert
            Assert.Multiple(
                () => Assert.Equal("a ו-a", byIndex.Value),
                () => Assert.Equal("a ו-a", byName.Value));
        }

        [Fact]
        public void Should_apply_a_forma_hole_s_format_in_the_formatting_culture()
        {
            // Arrange
            using var serviceProvider = CreateServiceProvider();
            using var cultures = new CultureFixture();
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("he");
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

            // Act
            var text = CreateLocalizer(serviceProvider)["{Ratio:P1} done", 0.256];

            // Assert
            Assert.Equal("25.6% הושלמו", text.Value);
        }

        [Fact]
        public void Should_refuse_a_key_mixing_composite_placeholders_with_forma_holes()
        {
            // Arrange
            using var serviceProvider = CreateServiceProvider();
            var localizer = CreateLocalizer(serviceProvider);

            // Act
            var filled = Record.Exception(() => localizer["Hello {0} and {Name}", "a", "b"]);
            var written = Record.Exception(() => localizer["Hello {0} and {Name}"]);

            // Assert
            Assert.Multiple(
                () => Assert.IsType<FormatException>(filled),
                () => Assert.IsType<FormatException>(written));
        }

        [Fact]
        public void Should_refuse_a_text_using_the_syntax_its_key_does_not()
        {
            // Arrange
            using var serviceProvider = CreateServiceProvider();
            using var cultures = new CultureFixture("he");
            var localizer = CreateLocalizer(serviceProvider);

            // Act
            var formaInComposite = Record.Exception(() => localizer["Mismatched {0}", "x"]);
            var compositeInForma = Record.Exception(() => localizer["Owner {Name}", "x"]);
            var expressionInComposite = Record.Exception(() => localizer["Files {0}", 2]);
            var namedExpressionInComposite = Record.Exception(() => localizer["Count {0}", 2]);
            var writtenWithFormaSyntax = Record.Exception(() => localizer["Mismatched {0}"]);

            // Assert
            Assert.Multiple(
                () => Assert.IsType<FormatException>(formaInComposite),
                () => Assert.IsType<FormatException>(compositeInForma),
                () => Assert.IsType<FormatException>(expressionInComposite),
                () => Assert.IsType<FormatException>(namedExpressionInComposite),
                () => Assert.IsType<FormatException>(writtenWithFormaSyntax));
        }
    }

    public sealed class Registration
    {
        [Fact]
        public void Should_keep_a_factory_registered_before_it()
        {
            // Arrange
            var hostFactory = new HostLocalizerFactory();
            var services = new ServiceCollection().AddSingleton<IStringLocalizerFactory>(hostFactory);

            // Act
            using var serviceProvider = services.AddFormaStringLocalizerAdapter().BuildServiceProvider();

            // Assert
            Assert.Same(hostFactory, serviceProvider.GetRequiredService<IStringLocalizerFactory>());
        }

        [Fact]
        public void Should_be_kept_when_a_factory_is_added_after_it_with_try_add()
        {
            // Arrange
            var services = new ServiceCollection().AddJsonLocalization().AddFormaStringLocalizerAdapter();

            // Act
            services.TryAddSingleton<IStringLocalizerFactory>(new HostLocalizerFactory());
            using var serviceProvider = services.BuildServiceProvider();

            // Assert
            Assert.IsNotType<HostLocalizerFactory>(serviceProvider.GetRequiredService<IStringLocalizerFactory>());
        }

        [Fact]
        public void Should_register_the_factory_once_as_a_singleton_and_the_typed_localizer()
        {
            // Act
            var services = new ServiceCollection().AddFormaStringLocalizerAdapter().AddFormaStringLocalizerAdapter();

            // Assert
            Assert.Multiple(
                () => Assert.Equal(ServiceLifetime.Singleton, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IStringLocalizerFactory)).Lifetime),
                () => Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IStringLocalizer<>)));
        }

        private sealed class HostLocalizerFactory : IStringLocalizerFactory
        {
            public IStringLocalizer Create(Type resourceSource)
                => throw new NotSupportedException();

            public IStringLocalizer Create(string baseName, string location)
                => throw new NotSupportedException();
        }
    }
}

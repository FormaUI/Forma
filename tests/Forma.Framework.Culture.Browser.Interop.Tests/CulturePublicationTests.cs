namespace Forma.Framework.Culture.Browser.Interop.Tests;

using System.Globalization;
using System.Text.Json;

public sealed class CulturePublicationTests
{
    private static readonly string[] AddressAttributes = ["content", "data-default", "data-prefix", "data-switch", "data-root", "data-cookie-name", "data-cookie-path"];

    public sealed class Marker
    {
        [Fact]
        public void Should_name_each_culture_on_its_own_and_the_invariant_culture_by_the_empty_name()
        {
            // Act
            var attributes = CulturePublication.GetMarkerAttributes(CultureInfo.InvariantCulture, CultureInfo.GetCultureInfo("he"));

            // Assert
            Assert.Equal(["data-culture=", "data-ui-culture=he"], attributes.Select(attribute => $"{attribute.Key}={attribute.Value}"));
        }
    }

    public sealed class Policy
    {
        [Fact]
        public void Should_publish_the_state_alone_under_the_default_policy()
        {
            // Act
            var attributes = CulturePublication.GetPolicyAttributes(CultureSwitchPolicy.Default, map: null);

            // Assert
            Assert.Equal("content=state", $"{Assert.Single(attributes).Key}={attributes["content"]}");
        }

        [Fact]
        public void Should_publish_every_setting_writing_the_cookie_takes()
        {
            // Arrange
            var policy = new CultureSwitchPolicy
            {
                Persistence = CultureSwitchPersistence.Cookie,
                CookieName = "culture",
                CookiePath = "/app",
                CookieDomain = "example.com",
                CookieLifetime = TimeSpan.FromDays(1),
                IsCookieSecure = true,
                CookieSameSite = CultureCookieSameSite.Strict,
            };

            // Act
            var attributes = CulturePublication.GetPolicyAttributes(policy, map: null);

            // Assert
            Assert.Equal(
                "content=cookie data-domain=example.com data-max-age=86400 data-name=culture data-path=/app data-same-site=strict data-secure=",
                string.Join(' ', attributes.OrderBy(attribute => attribute.Key, StringComparer.Ordinal).Select(attribute => $"{attribute.Key}={attribute.Value}")));
        }

        [Fact]
        public void Should_leave_out_a_cookie_domain_and_lifetime_it_has_none_of_and_the_security_of_one_that_is_not_secure()
        {
            // Arrange
            var policy = new CultureSwitchPolicy { Persistence = CultureSwitchPersistence.Cookie, CookieLifetime = null };

            // Act
            var attributes = CulturePublication.GetPolicyAttributes(policy, map: null);

            // Assert
            Assert.Multiple(
                () => Assert.DoesNotContain("data-domain", attributes.Keys),
                () => Assert.DoesNotContain("data-max-age", attributes.Keys),
                () => Assert.DoesNotContain("data-secure", attributes.Keys),
                () => Assert.Equal("lax", attributes["data-same-site"]));
        }

        [Fact]
        public void Should_publish_the_address_policy_where_culture_addresses_give_a_map_whatever_the_policy()
        {
            // Arrange
            var map = new CultureRouteMap
            {
                Routes = [new() { Token = "he", Aliases = ["iw"], Culture = "he", UICulture = "he", Direction = "rtl" }],
                DefaultToken = "he",
                PrefixesDefault = true,
                SwitchesInPlace = false,
                Root = "/app/",
                CookieName = "route",
                CookiePath = "/app/",
            };

            // Act
            var attributes = CulturePublication.GetPolicyAttributes(new CultureSwitchPolicy { Persistence = CultureSwitchPersistence.Cookie }, map);

            // Assert
            using var routes = JsonDocument.Parse(attributes["data-routes"]);
            var route = Assert.Single(routes.RootElement.EnumerateArray());
            Assert.Multiple(
                () => Assert.Equal(
                    "address he always full-load /app/ route /app/",
                    string.Join(' ', AddressAttributes.Select(name => attributes[name]))),
                () => Assert.Equal(
                    "he iw he he rtl",
                    string.Join(' ', route.GetProperty("token").GetString(), route.GetProperty("aliases")[0].GetString(), route.GetProperty("culture").GetString(), route.GetProperty("uiCulture").GetString(), route.GetProperty("direction").GetString())));
        }
    }
}

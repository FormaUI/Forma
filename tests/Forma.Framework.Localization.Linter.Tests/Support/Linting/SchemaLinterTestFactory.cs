namespace Forma.Framework.Localization.Linter.Tests.Support.Linting;

using Forma.Framework.Localization.Linter.Linting;
using Forma.Framework.Localization.Linter.Rules;
using Forma.Framework.Localization.Linter.Rules.Files;

/// <summary>Builds a schema linter carrying the rules the linter ships with.</summary>
public static class SchemaLinterTestFactory
{
    /// <summary>A linter running every shipped file rule.</summary>
    public static SchemaLinter Create()
        => new(Checkers());

    /// <summary>Every file rule the linter ships with.</summary>
    public static IReadOnlyList<ILocalizationFileRuleChecker> Checkers()
        =>
        [
            new RequiredFieldsChecker(),
            new RequiredFieldOrderChecker(),
            new UnknownFieldChecker(),
            new SchemaVersionChecker(),
            new CultureMatchesChecker(),
            new TranslationHolesChecker(),
        ];
}

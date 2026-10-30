namespace Forma.Blazor.Browser.Tests;

using Xunit.Sdk;

/// <summary>A requirement the code doesn't meet yet, checked so that it keeps failing as recorded until a change meets it.</summary>
/// <remarks>
/// A known failure passes while its expectation fails, and fails once the expectation holds, naming the requirement,
/// so the change that meets it moves the check among the requirements met rather than leaving a gate that says
/// nothing. The suite reports the three kinds apart: a regression check is a plain test, a known failure carries the
/// <c>Kind</c> trait <c>KnownFailure</c>, and a met requirement is a plain test again, in the change that met it.
/// </remarks>
public static class KnownFailure
{
    /// <summary>The trait every known failure carries.</summary>
    public const string Kind = "KnownFailure";

    /// <summary>Runs <paramref name="expectation"/>, which must still fail.</summary>
    /// <param name="requirement">The requirement the expectation checks, as the plan's To verify names it.</param>
    /// <param name="expectation">The check of the requirement, which fails while the requirement is unmet.</param>
    /// <returns>A task that completes once the expectation has failed as recorded.</returns>
    public static async Task AssertStillFailsAsync(string requirement, Func<Task> expectation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requirement);
        ArgumentNullException.ThrowIfNull(expectation);

        try
        {
            await expectation().ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is XunitException or TimeoutException or PlaywrightException)
        {
            TestContext.Current.TestOutputHelper?.WriteLine($"still failing, as recorded: {requirement}{Environment.NewLine}{exception.Message}");

            return;
        }

        throw new XunitException($"Now met, so it is no longer a known failure: {requirement}. Make it a plain check in the change that met it.");
    }
}

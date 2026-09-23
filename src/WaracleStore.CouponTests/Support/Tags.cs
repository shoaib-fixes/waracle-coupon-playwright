namespace WaracleStore.CouponTests.Support;

/// <summary>
/// Gherkin tags the code reacts to. Names avoid hyphens because Reqnroll turns tags into
/// NUnit categories, and NUnit rejects category names containing <c>-</c>, <c>,</c>, <c>!</c> or <c>+</c>.
/// </summary>
public static class Tags
{
    /// <summary>Talks to the REST API only; no browser is started.</summary>
    public const string Api = "API";

    /// <summary>Fails on the current build because the release does not meet its acceptance criteria.</summary>
    public const string KnownDefect = "KnownDefect";

    /// <summary>The handful of scenarios worth running first; reported with critical severity.</summary>
    public const string Smoke = "Smoke";

    /// <summary>Measures timings; runs after everything else so other scenarios do not skew the numbers.</summary>
    public const string Performance = "Performance";
}

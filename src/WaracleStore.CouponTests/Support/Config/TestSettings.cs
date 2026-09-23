namespace WaracleStore.CouponTests.Support.Config;

public enum TraceMode
{
    /// <summary>Never record a Playwright trace.</summary>
    Off,

    /// <summary>Record and keep a trace for every scenario.</summary>
    On,

    /// <summary>Record every scenario, keep the trace only when the scenario fails.</summary>
    RetainOnFailure,
}

/// <summary>
/// Strongly typed test configuration. Values come from appsettings.json, an optional
/// appsettings.Local.json, then <c>TEST_</c>-prefixed environment variables
/// (for example <c>TEST_Browser=firefox</c> or <c>TEST_Credentials__Password=...</c>).
/// </summary>
public sealed class TestSettings
{
    private static readonly string[] SupportedBrowsers = ["chromium", "firefox", "webkit"];

    public string BaseUrl { get; init; } = string.Empty;

    public string ApiUrl { get; init; } = string.Empty;

    public string Browser { get; init; } = "chromium";

    public bool Headless { get; init; } = true;

    public int SlowMoMs { get; init; }

    public int DefaultTimeoutMs { get; init; } = 10_000;

    public TraceMode TraceMode { get; init; } = TraceMode.RetainOnFailure;

    /// <summary>Where traces and screenshots are written. Relative paths resolve against the test output directory.</summary>
    public string ArtifactsDirectory { get; init; } = "artifacts";

    public Credentials Credentials { get; init; } = new();

    public PerformanceBudgets Budgets { get; init; } = new();

    public string ResolvedArtifactsDirectory =>
        Path.IsPathRooted(ArtifactsDirectory)
            ? ArtifactsDirectory
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, ArtifactsDirectory));

    /// <summary>Fails fast, once, with every problem listed, rather than once per scenario.</summary>
    public void Validate()
    {
        var problems = new List<string>();

        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
            problems.Add($"BaseUrl '{BaseUrl}' is not an absolute URL.");

        if (!Uri.TryCreate(ApiUrl, UriKind.Absolute, out _))
            problems.Add($"ApiUrl '{ApiUrl}' is not an absolute URL.");

        if (!SupportedBrowsers.Contains(Browser, StringComparer.OrdinalIgnoreCase))
            problems.Add($"Browser '{Browser}' is not one of: {string.Join(", ", SupportedBrowsers)}.");

        if (DefaultTimeoutMs <= 0)
            problems.Add("DefaultTimeoutMs must be positive.");

        if (SlowMoMs < 0)
            problems.Add("SlowMoMs cannot be negative.");

        if (string.IsNullOrWhiteSpace(Credentials.Email) || string.IsNullOrWhiteSpace(Credentials.Password))
            problems.Add("Credentials.Email and Credentials.Password are required.");

        if (Budgets.PageLoadMs <= 0 || Budgets.LargestContentfulPaintMs <= 0 || Budgets.CouponApplyMs <= 0 || Budgets.SummaryApiP95Ms <= 0)
            problems.Add("Every Budgets value must be a positive number of milliseconds.");

        if (problems.Count > 0)
            throw new InvalidOperationException(
                "Invalid test configuration:" + Environment.NewLine + string.Join(Environment.NewLine, problems.Select(p => "  - " + p)));
    }
}

/// <summary>Smoke-level performance budgets in milliseconds. These catch regressions, they are not a load test.</summary>
public sealed class PerformanceBudgets
{
    /// <summary>Navigation Timing <c>loadEventEnd</c> for the cart page.</summary>
    public int PageLoadMs { get; init; } = 3_000;

    /// <summary>Largest Contentful Paint for the cart page.</summary>
    public int LargestContentfulPaintMs { get; init; } = 2_500;

    /// <summary>Click on Apply until the discount line is rendered.</summary>
    public int CouponApplyMs { get; init; } = 1_500;

    /// <summary>95th percentile of POST /api/cart/summary over a short burst of requests.</summary>
    public int SummaryApiP95Ms { get; init; } = 300;
}

public sealed class Credentials
{
    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}

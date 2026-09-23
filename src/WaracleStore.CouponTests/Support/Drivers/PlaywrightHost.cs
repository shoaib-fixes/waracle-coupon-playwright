using Microsoft.Playwright;
using WaracleStore.CouponTests.Support.Config;

namespace WaracleStore.CouponTests.Support.Drivers;

/// <summary>
/// Run-scoped Playwright host: one <see cref="IPlaywright"/> and one <see cref="IBrowser"/>
/// shared by every scenario, plus a JWT for the demo customer obtained once through the API.
/// Created in <c>[BeforeTestRun]</c>, never mutated afterwards, so it is safe to read from
/// parallel scenarios. Each scenario gets its own isolated <see cref="IBrowserContext"/>
/// from <see cref="BrowserDriver"/>.
/// </summary>
public sealed class PlaywrightHost : IAsyncDisposable
{
    private PlaywrightHost(IPlaywright playwright, IBrowser browser, string authToken)
    {
        Playwright = playwright;
        Browser = browser;
        AuthToken = authToken;
    }

    public IPlaywright Playwright { get; }

    public IBrowser Browser { get; }

    /// <summary>Bearer token for the configured demo customer, stored by the web app under <c>waracle_token</c>.</summary>
    public string AuthToken { get; }

    public static async Task<PlaywrightHost> StartAsync(TestSettings settings)
    {
        var playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        // Web-first assertions (Expect) have their own timeout, separate from action timeouts;
        // keep both driven by the same setting.
        Assertions.SetDefaultExpectTimeout(settings.DefaultTimeoutMs);

        await AssertStoreIsReachableAsync(playwright, settings);
        var token = await SignInThroughApiAsync(playwright, settings);

        var browserType = settings.Browser.ToLowerInvariant() switch
        {
            "chromium" => playwright.Chromium,
            "firefox" => playwright.Firefox,
            "webkit" => playwright.Webkit,
            var other => throw new InvalidOperationException($"Unsupported browser '{other}'."),
        };

        var browser = await browserType.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = settings.Headless,
            SlowMo = settings.SlowMoMs,
        });

        return new PlaywrightHost(playwright, browser, token);
    }

    /// <summary>
    /// Fail once with a useful message if the store is down, instead of every scenario
    /// timing out on its first navigation.
    /// </summary>
    private static async Task AssertStoreIsReachableAsync(IPlaywright playwright, TestSettings settings)
    {
        await using var api = await playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions { Timeout = settings.DefaultTimeoutMs });

        var problems = new List<string>();
        foreach (var (name, url) in new[] { ("API", $"{settings.ApiUrl.TrimEnd('/')}{ApiRoutes.Health}"), ("Web app", settings.BaseUrl) })
        {
            try
            {
                var response = await api.GetAsync(url);
                if (!response.Ok)
                    problems.Add($"{name} at {url} returned HTTP {response.Status}.");
            }
            catch (PlaywrightException ex)
            {
                problems.Add($"{name} at {url} is not reachable ({ex.Message.Split('\n')[0]}).");
            }
        }

        if (problems.Count > 0)
            throw new InvalidOperationException(
                "Waracle Store is not running:" + Environment.NewLine +
                string.Join(Environment.NewLine, problems.Select(p => "  - " + p)) + Environment.NewLine +
                "Start it from the qa-candidate-test repo with ./launch-web.sh (backend on :4000, web on :5173). See README.");
    }

    private static async Task<string> SignInThroughApiAsync(IPlaywright playwright, TestSettings settings)
    {
        await using var api = await playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
        {
            BaseURL = settings.ApiUrl,
            Timeout = settings.DefaultTimeoutMs,
        });

        var response = await api.PostAsync(ApiRoutes.Login, new APIRequestContextOptions
        {
            DataObject = new { email = settings.Credentials.Email, password = settings.Credentials.Password },
        });

        if (!response.Ok)
            throw new InvalidOperationException(
                $"Could not sign in as {settings.Credentials.Email}: HTTP {response.Status} {await response.TextAsync()}");

        var body = await response.JsonAsync();
        return body?.GetProperty("token").GetString()
               ?? throw new InvalidOperationException("Login response did not contain a token.");
    }

    public async ValueTask DisposeAsync()
    {
        await Browser.DisposeAsync();
        Playwright.Dispose();
    }
}

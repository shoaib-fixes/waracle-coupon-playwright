using System.Text.Json;
using Microsoft.Playwright;
using WaracleStore.CouponTests.Support.Config;

namespace WaracleStore.CouponTests.Support.Drivers;

/// <summary>
/// Scenario-scoped browser state. Reqnroll creates one instance per scenario, so every UI
/// scenario owns an isolated <see cref="IBrowserContext"/> (fresh cookies, localStorage and
/// cache) and a single <see cref="IPage"/>. Tracing runs per context; the caller decides
/// where the kept artefacts are attached.
/// </summary>
public sealed class BrowserDriver(PlaywrightHost host, TestSettings settings)
{
    // A desktop viewport wide enough for the two-column cart/checkout layouts; en-GB so
    // currency and dates render exactly as the assertions expect.
    private static readonly ViewportSize Viewport = new() { Width = 1280, Height = 900 };
    private const string Locale = "en-GB";

    private IBrowserContext? _context;
    private IPage? _page;

    public IPage Page => _page ?? throw new InvalidOperationException("The browser has not been started for this scenario.");

    public IBrowserContext Context => _context ?? throw new InvalidOperationException("The browser has not been started for this scenario.");

    public string AuthToken => host.AuthToken;

    public async Task StartAsync()
    {
        _context = await host.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = settings.BaseUrl,
            ViewportSize = Viewport,
            Locale = Locale,
        });
        _context.SetDefaultTimeout(settings.DefaultTimeoutMs);

        if (settings.TraceMode != TraceMode.Off)
        {
            await _context.Tracing.StartAsync(new TracingStartOptions
            {
                Screenshots = true,
                Snapshots = true,
                Sources = true,
            });
        }

        _page = await _context.NewPageAsync();
    }

    public Task GotoAsync(string path) => Page.GotoAsync(path);

    /// <summary>
    /// Writes a localStorage entry before the app boots on the first navigation of this context.
    /// The value is only written when the key is absent, so later full-page loads in the same
    /// scenario never clobber state the app has since changed.
    /// </summary>
    public Task SeedLocalStorageAsync(string key, string value)
    {
        var script = $$"""
            (() => {
              const key = {{JsonSerializer.Serialize(key)}};
              if (window.localStorage.getItem(key) === null) {
                window.localStorage.setItem(key, {{JsonSerializer.Serialize(value)}});
              }
            })();
            """;
        return Context.AddInitScriptAsync(script);
    }

    /// <summary>
    /// Stops tracing, keeps a screenshot on failure and the trace according to
    /// <see cref="TraceMode"/>, closes the context, and returns what was kept.
    /// </summary>
    public async Task<IReadOnlyList<Artifact>> StopAsync(string scenarioTitle, Exception? error)
    {
        if (_context is null)
            return [];

        var failed = error is not null;
        var keepTrace = settings.TraceMode == TraceMode.On || (settings.TraceMode == TraceMode.RetainOnFailure && failed);
        var kept = new List<Artifact>();

        if (failed && _page is not null && !_page.IsClosed)
        {
            var screenshotPath = Attachments.PathFor(settings, scenarioTitle, ".png");
            try
            {
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = true });
                kept.Add(new Artifact("Screenshot at failure", "image/png", screenshotPath));
            }
            catch (PlaywrightException)
            {
                // The page may already be gone (for example the browser crashed); the trace still tells the story.
            }
        }

        if (settings.TraceMode != TraceMode.Off)
        {
            var tracePath = keepTrace ? Attachments.PathFor(settings, scenarioTitle, ".trace.zip") : null;
            await _context.Tracing.StopAsync(new TracingStopOptions { Path = tracePath });
            if (tracePath is not null)
                kept.Add(new Artifact("Playwright trace (open at trace.playwright.dev)", "application/zip", tracePath));
        }

        await _context.CloseAsync();
        _context = null;
        _page = null;
        return kept;
    }

    public sealed record Artifact(string Name, string MimeType, string Path);
}

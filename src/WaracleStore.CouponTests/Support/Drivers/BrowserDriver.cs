using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using NUnit.Framework;
using WaracleStore.CouponTests.Support.Config;

namespace WaracleStore.CouponTests.Support.Drivers;

/// <summary>
/// Scenario-scoped browser state. Reqnroll creates one instance per scenario, so every
/// scenario owns an isolated <see cref="IBrowserContext"/> (fresh cookies, localStorage
/// and cache) and a single <see cref="IPage"/>. Tracing runs per context and the trace
/// is attached to the NUnit result on failure.
/// </summary>
public sealed partial class BrowserDriver(PlaywrightRunner runner, TestSettings settings)
{
    private IBrowserContext? _context;
    private IPage? _page;

    public IPage Page => _page ?? throw new InvalidOperationException("The browser has not been started for this scenario.");

    public IBrowserContext Context => _context ?? throw new InvalidOperationException("The browser has not been started for this scenario.");

    public string AuthToken => runner.AuthToken;

    public async Task StartAsync()
    {
        _context = await runner.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = settings.BaseUrl,
            ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
            Locale = "en-GB",
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

    public bool IsStarted => _context is not null;

    /// <summary>
    /// Stops tracing, keeps artefacts according to <see cref="TraceMode"/>, attaches them to the
    /// NUnit result and closes the context. Returns the kept artefacts so the caller can attach
    /// them elsewhere (the Allure report).
    /// </summary>
    public async Task<IReadOnlyList<Artifact>> StopAsync(string scenarioTitle, Exception? error)
    {
        if (_context is null)
            return [];

        var failed = error is not null;
        var keepTrace = settings.TraceMode == TraceMode.On || (settings.TraceMode == TraceMode.RetainOnFailure && failed);
        var kept = new List<Artifact>();

        if (keepTrace || failed)
            Directory.CreateDirectory(settings.ResolvedArtifactsDirectory);

        var stem = ArtifactStem(scenarioTitle);

        if (failed && _page is not null && !_page.IsClosed)
        {
            var screenshotPath = Path.Combine(settings.ResolvedArtifactsDirectory, stem + ".png");
            try
            {
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = true });
                TestContext.AddTestAttachment(screenshotPath, "Screenshot at failure");
                kept.Add(new Artifact("Screenshot at failure", "image/png", screenshotPath));
            }
            catch (PlaywrightException)
            {
                // The page may already be gone (for example the browser crashed); the trace still tells the story.
            }
        }

        if (settings.TraceMode != TraceMode.Off)
        {
            var tracePath = keepTrace ? Path.Combine(settings.ResolvedArtifactsDirectory, stem + ".trace.zip") : null;
            await _context.Tracing.StopAsync(new TracingStopOptions { Path = tracePath });
            if (tracePath is not null)
            {
                TestContext.AddTestAttachment(tracePath, "Playwright trace (open with: npx playwright show-trace)");
                kept.Add(new Artifact("Playwright trace (open at trace.playwright.dev)", "application/zip", tracePath));
            }
        }

        await _context.CloseAsync();
        _context = null;
        _page = null;
        return kept;
    }

    public sealed record Artifact(string Name, string MimeType, string Path);

    private static string ArtifactStem(string scenarioTitle)
    {
        var safe = UnsafeChars().Replace(scenarioTitle, "_").Trim('_');
        if (safe.Length > 80)
            safe = safe[..80];
        return $"{safe}_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..6]}";
    }

    [GeneratedRegex(@"[^A-Za-z0-9]+")]
    private static partial Regex UnsafeChars();
}

using Reqnroll;
using Reqnroll.BoDi;
using WaracleStore.CouponTests.Support;
using WaracleStore.CouponTests.Support.Config;
using WaracleStore.CouponTests.Support.Drivers;

namespace WaracleStore.CouponTests.Hooks;

[Binding]
public sealed class BrowserHooks
{
    /// <summary>
    /// Runs once per test run, before any scenario on any worker thread. Loads and validates
    /// configuration, checks the store is up, signs in through the API and launches the browser.
    /// Registered in the global container so scenarios receive them by constructor injection.
    /// </summary>
    [BeforeTestRun(Order = 0)]
    public static async Task StartRunAsync(IObjectContainer globalContainer)
    {
        var settings = SettingsLoader.Load();
        globalContainer.RegisterInstanceAs(settings);

        var host = await PlaywrightHost.StartAsync(settings);
        globalContainer.RegisterInstanceAs(host);
    }

    [AfterTestRun]
    public static async Task StopRunAsync(IObjectContainer globalContainer)
    {
        if (globalContainer.IsRegistered<PlaywrightHost>())
            await globalContainer.Resolve<PlaywrightHost>().DisposeAsync();
    }

    /// <summary>API scenarios never need a browser; everything else gets an isolated context.</summary>
    [BeforeScenario(Order = 0)]
    public Task StartBrowserAsync(BrowserDriver driver, ScenarioContext scenarioContext) =>
        scenarioContext.ScenarioInfo.CombinedTags.Contains(Tags.Api) ? Task.CompletedTask : driver.StartAsync();

    [AfterScenario]
    public async Task StopBrowserAsync(BrowserDriver driver, ScenarioContext scenarioContext)
    {
        var kept = await driver.StopAsync(scenarioContext.ScenarioInfo.Title, scenarioContext.TestError);
        foreach (var artifact in kept)
            Attachments.AddToTestCase(artifact.Name, artifact.MimeType, artifact.Path);
    }

    [AfterScenario]
    public Task DisposeApiAsync(ApiDriver api) => api.DisposeAsync().AsTask();
}

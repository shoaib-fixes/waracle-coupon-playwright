using Reqnroll;
using Reqnroll.BoDi;
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
    [BeforeTestRun]
    public static async Task StartRunAsync(IObjectContainer globalContainer)
    {
        var settings = SettingsLoader.Load();
        globalContainer.RegisterInstanceAs(settings);

        var runner = await PlaywrightRunner.StartAsync(settings);
        globalContainer.RegisterInstanceAs(runner);
    }

    [AfterTestRun]
    public static async Task StopRunAsync(IObjectContainer globalContainer)
    {
        if (globalContainer.IsRegistered<PlaywrightRunner>())
            await globalContainer.Resolve<PlaywrightRunner>().DisposeAsync();
    }

    [BeforeScenario(Order = 0)]
    public Task StartBrowserAsync(BrowserDriver driver) => driver.StartAsync();

    [AfterScenario]
    public Task StopBrowserAsync(BrowserDriver driver, ScenarioContext scenarioContext) =>
        driver.StopAsync(scenarioContext.ScenarioInfo.Title, scenarioContext.TestError);
}

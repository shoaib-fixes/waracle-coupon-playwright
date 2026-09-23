using Allure.Net.Commons;
using Reqnroll;
using Reqnroll.BoDi;
using WaracleStore.CouponTests.Support.Config;
using WaracleStore.CouponTests.Support.Drivers;

namespace WaracleStore.CouponTests.Hooks;

[Binding]
public sealed class BrowserHooks
{
    /// <summary>Scenarios tagged @API talk to the REST API only and never open a browser.</summary>
    private const string ApiTag = "API";

    private const string ReleaseObservationsUrl =
        "https://github.com/shoaib-fixes/waracle-coupon-playwright/blob/main/docs/ReleaseObservations.md";

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

        DescribeRunInAllure(settings);
    }

    [AfterTestRun]
    public static async Task StopRunAsync(IObjectContainer globalContainer)
    {
        if (globalContainer.IsRegistered<PlaywrightRunner>())
            await globalContainer.Resolve<PlaywrightRunner>().DisposeAsync();
    }

    [BeforeScenario(Order = 0)]
    public Task StartBrowserAsync(BrowserDriver driver, ScenarioContext scenarioContext) =>
        IsApiScenario(scenarioContext) ? Task.CompletedTask : driver.StartAsync();

    [BeforeScenario(Order = 1)]
    public void DescribeScenarioInAllure(ScenarioContext scenarioContext, TestSettings settings)
    {
        var tags = scenarioContext.ScenarioInfo.CombinedTags;
        var isApi = IsApiScenario(scenarioContext);

        AllureApi.AddEpic("WS-101 Coupon Codes at Checkout");
        AllureApi.AddLabel("layer", isApi ? "api" : "ui");
        if (!isApi)
            AllureApi.AddLabel("browser", settings.Browser);

        AllureApi.SetSeverity(tags.Contains("Smoke") ? SeverityLevel.critical : SeverityLevel.normal);

        if (tags.Contains("KnownDefect"))
        {
            AllureApi.AddLink("Known release defect – see observations", ReleaseObservationsUrl);
            AllureApi.SetDescription("Expected to fail on the current build: the release does not meet this acceptance criterion. Goes green when the defect is fixed.");
        }
    }

    [AfterScenario]
    public async Task StopBrowserAsync(BrowserDriver driver, ScenarioContext scenarioContext)
    {
        var kept = await driver.StopAsync(scenarioContext.ScenarioInfo.Title, scenarioContext.TestError);
        foreach (var artifact in kept)
            AttachToTestCase(artifact);
    }

    /// <summary>
    /// After-scenario hooks are reported by Allure as tear-down fixtures, so a plain
    /// <c>AllureApi.AddAttachment</c> here would file the trace under "Tear down". Copy it into
    /// the results directory and attach it to the test case itself, where a reader looks first.
    /// </summary>
    private static void AttachToTestCase(BrowserDriver.Artifact artifact)
    {
        var source = $"{Guid.NewGuid():N}-attachment{Path.GetExtension(artifact.Path)}";
        File.Copy(artifact.Path, Path.Combine(AllureLifecycle.Instance.ResultsDirectory, source));

        AllureLifecycle.Instance.UpdateTestCase(testCase =>
            testCase.attachments.Add(new Attachment { name = artifact.Name, type = artifact.MimeType, source = source }));
    }

    [AfterScenario]
    public Task DisposeApiAsync(ApiDriver api) => api.DisposeAsync().AsTask();

    private static bool IsApiScenario(ScenarioContext scenarioContext) =>
        scenarioContext.ScenarioInfo.CombinedTags.Contains(ApiTag);

    /// <summary>The environment block of the Allure report. Failure grouping and known-issue rules live in allurerc.mjs.</summary>
    private static void DescribeRunInAllure(TestSettings settings)
    {
        var resultsDirectory = AllureLifecycle.Instance.ResultsDirectory;
        Directory.CreateDirectory(resultsDirectory);

        File.WriteAllLines(Path.Combine(resultsDirectory, "environment.properties"),
        [
            $"Browser={settings.Browser}",
            $"Headless={settings.Headless}",
            $"Web={settings.BaseUrl}",
            $"API={settings.ApiUrl}",
            $"TraceMode={settings.TraceMode}",
            $"OS={Environment.OSVersion}",
            $".NET={Environment.Version}",
        ]);
    }
}

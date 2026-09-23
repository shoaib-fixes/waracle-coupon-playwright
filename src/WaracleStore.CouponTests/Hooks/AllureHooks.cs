using Allure.Net.Commons;
using Reqnroll;
using Reqnroll.BoDi;
using WaracleStore.CouponTests.Support;
using WaracleStore.CouponTests.Support.Config;

namespace WaracleStore.CouponTests.Hooks;

/// <summary>
/// Everything the Allure report needs beyond what the Reqnroll adapter records on its own:
/// the environment block, and per-scenario metadata derived from tags.
/// </summary>
[Binding]
public sealed class AllureHooks
{
    private const string Epic = "WS-101 Coupon Codes at Checkout";

    /// <summary>Runs after <see cref="BrowserHooks.StartRunAsync"/> has registered the settings.</summary>
    [BeforeTestRun(Order = 1)]
    public static void DescribeRun(IObjectContainer globalContainer)
    {
        var settings = globalContainer.Resolve<TestSettings>();
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

    [BeforeScenario(Order = 1)]
    public void DescribeScenario(ScenarioContext scenarioContext, TestSettings settings)
    {
        var tags = scenarioContext.ScenarioInfo.CombinedTags;
        var isApi = tags.Contains(Tags.Api);

        AllureApi.AddEpic(Epic);
        AllureApi.AddLabel("layer", isApi ? "api" : "ui");
        if (!isApi)
            AllureApi.AddLabel("browser", settings.Browser);

        AllureApi.SetSeverity(tags.Contains(Tags.Smoke) ? SeverityLevel.critical : SeverityLevel.normal);

        if (tags.Contains(Tags.KnownDefect))
        {
            AllureApi.AddLink("Known release defect – see observations", Repository.ObservationsUrl);
            AllureApi.SetDescription(
                "Expected to fail on the current build: the release does not meet this acceptance criterion. Goes green when the defect is fixed.");
        }
    }
}

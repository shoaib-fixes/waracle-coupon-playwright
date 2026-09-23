using System.Diagnostics;
using Allure.Net.Commons;
using Microsoft.Playwright;
using NUnit.Framework;
using Reqnroll;
using Shouldly;
using WaracleStore.CouponTests.Pages;
using WaracleStore.CouponTests.Support;
using WaracleStore.CouponTests.Support.Config;
using WaracleStore.CouponTests.Support.Data;
using WaracleStore.CouponTests.Support.Drivers;

namespace WaracleStore.CouponTests.StepDefinitions;

[Binding]
public sealed class PerformanceSteps(BrowserDriver driver, ApiDriver api, TestSettings settings, ScenarioState state, CartPage cartPage)
{
    private PerformanceBudgets Budgets => settings.Budgets;

    // Metric names double as the parameter names shown in the report.
    private const string CartDomContentLoaded = "Cart page: DOMContentLoaded (ms)";
    private const string CartLoad = "Cart page: load (ms)";
    private const string CartLcp = "Cart page: LCP (ms)";
    private const string CouponApply = "Coupon apply → summary updated (ms)";
    private const string SummaryRequests = "POST /api/cart/summary: requests";
    private const string SummaryMedian = "POST /api/cart/summary: median (ms)";
    private const string SummaryP95 = "POST /api/cart/summary: p95 (ms)";
    private const string SummaryMax = "POST /api/cart/summary: max (ms)";

    // Registers a PerformanceObserver before the app boots so LCP can be read after load.
    private const string LcpObserverScript = """
        window.__lcp = 0;
        new PerformanceObserver((list) => {
          for (const entry of list.getEntries()) window.__lcp = entry.startTime;
        }).observe({ type: 'largest-contentful-paint', buffered: true });
        """;

    [When("I open the cart and record its load timings")]
    public async Task WhenOpenCartAndRecordTimings()
    {
        await driver.Context.AddInitScriptAsync(LcpObserverScript);
        await cartPage.OpenAsync();
        await Assertions.Expect(cartPage.Summary.TotalRow).ToBeVisibleAsync();

        var timings = await driver.Page.EvaluateAsync<double[]>("""
            () => {
              const nav = performance.getEntriesByType('navigation')[0];
              return [nav.domContentLoadedEventEnd, nav.loadEventEnd, window.__lcp || 0];
            }
            """);

        Record(CartDomContentLoaded, timings[0]);
        Record(CartLoad, timings[1]);
        Record(CartLcp, timings[2]);
    }

    [Then("the cart page loads within the performance budget")]
    public void ThenCartLoadsWithinBudget()
    {
        WithinBudget((CartLoad, Budgets.PageLoadMs));

        // Only Chromium-based browsers report LCP; a zero would otherwise pass the budget vacuously.
        if (state.Timings[CartLcp] <= 0)
            Assert.Inconclusive($"{settings.Browser} does not report Largest Contentful Paint; only the load budget was checked.");
        WithinBudget((CartLcp, Budgets.LargestContentfulPaintMs));
    }

    [When("I apply the coupon {string} and time how long the summary takes to update")]
    public async Task WhenApplyCouponTimed(string code)
    {
        await cartPage.CouponField.FillAsync(code);

        var stopwatch = Stopwatch.StartNew();
        await cartPage.ApplyCouponButton.ClickAsync();
        await Assertions.Expect(cartPage.Summary.CouponRow).ToBeVisibleAsync();
        stopwatch.Stop();

        state.CouponCode = code;
        Record(CouponApply, stopwatch.Elapsed.TotalMilliseconds);
    }

    [Then("the coupon is reflected within the performance budget")]
    public void ThenCouponWithinBudget() =>
        WithinBudget((CouponApply, Budgets.CouponApplyMs));

    [When("I request the cart summary for {string} with coupon {string} {int} times")]
    public async Task WhenRequestSummaryRepeatedly(string basket, string coupon, int times)
    {
        var body = ApiPayloads.CartSummary(BasketParser.FromText(basket), coupon);

        var samples = new List<double>(times);
        for (var i = 0; i < times; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await api.PostAsync(ApiRoutes.CartSummary, body);
            stopwatch.Stop();
            response.Ok.ShouldBeTrue($"request {i + 1} returned HTTP {response.Status}");
            samples.Add(stopwatch.Elapsed.TotalMilliseconds);
        }

        samples.Sort();
        Record(SummaryRequests, times);
        Record(SummaryMedian, samples[samples.Count / 2]);
        Record(SummaryP95, samples[(int)Math.Ceiling(0.95 * samples.Count) - 1]);
        Record(SummaryMax, samples[^1]);
    }

    [Then("the 95th percentile response time is within the performance budget")]
    public void ThenP95WithinBudget() =>
        WithinBudget((SummaryP95, Budgets.SummaryApiP95Ms));

    /// <summary>Stores a measurement and surfaces it on the NUnit output and as an Allure parameter, so the report shows the numbers even when the budget is met.</summary>
    private void Record(string metric, double value)
    {
        var rounded = Math.Round(value, 1);
        state.Timings[metric] = rounded;
        TestContext.Out.WriteLine($"{metric}: {rounded}");
        AllureApi.AddTestParameter(metric, rounded);
    }

    private void WithinBudget(params (string Metric, int BudgetMs)[] checks)
    {
        var breaches = checks
            .Where(c => state.Timings[c.Metric] > c.BudgetMs)
            .Select(c => $"{c.Metric}: measured {state.Timings[c.Metric]}, budget {c.BudgetMs}")
            .ToList();

        if (breaches.Count > 0)
            throw new ShouldAssertException(
                "Performance budget exceeded:" + Environment.NewLine + string.Join(Environment.NewLine, breaches.Select(b => "  - " + b)));
    }
}

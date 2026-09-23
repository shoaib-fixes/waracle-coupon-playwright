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

        Record("Cart page: DOMContentLoaded (ms)", timings[0]);
        Record("Cart page: load (ms)", timings[1]);
        Record("Cart page: LCP (ms)", timings[2]);
    }

    [Then("the cart page loads within the performance budget")]
    public void ThenCartLoadsWithinBudget() =>
        WithinBudget(
            ("Cart page: load (ms)", Budgets.PageLoadMs),
            ("Cart page: LCP (ms)", Budgets.LargestContentfulPaintMs));

    [When("I apply the coupon {string} and time how long the summary takes to update")]
    public async Task WhenApplyCouponTimed(string code)
    {
        await cartPage.CouponField.FillAsync(code);

        var stopwatch = Stopwatch.StartNew();
        await cartPage.ApplyCouponButton.ClickAsync();
        await Assertions.Expect(cartPage.Summary.CouponRow).ToBeVisibleAsync();
        stopwatch.Stop();

        state.CouponCode = code;
        Record("Coupon apply → summary updated (ms)", stopwatch.Elapsed.TotalMilliseconds);
    }

    [Then("the coupon is reflected within the performance budget")]
    public void ThenCouponWithinBudget() =>
        WithinBudget(("Coupon apply → summary updated (ms)", Budgets.CouponApplyMs));

    [When("I request the cart summary for {string} with coupon {string} {int} times")]
    public async Task WhenRequestSummaryRepeatedly(string basket, string coupon, int times)
    {
        var lines = BasketParser.FromText(basket);
        var body = new
        {
            items = lines.Select(l => new { productId = l.Product.Id, quantity = l.Quantity }),
            couponCode = coupon,
        };

        var samples = new List<double>(times);
        for (var i = 0; i < times; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await api.PostAsync("/api/cart/summary", body);
            stopwatch.Stop();
            response.Ok.ShouldBeTrue($"request {i + 1} returned HTTP {response.Status}");
            samples.Add(stopwatch.Elapsed.TotalMilliseconds);
        }

        samples.Sort();
        Record("POST /api/cart/summary: requests", times);
        Record("POST /api/cart/summary: median (ms)", samples[samples.Count / 2]);
        Record("POST /api/cart/summary: p95 (ms)", samples[(int)Math.Ceiling(0.95 * samples.Count) - 1]);
        Record("POST /api/cart/summary: max (ms)", samples[^1]);
    }

    [Then("the 95th percentile response time is within the performance budget")]
    public void ThenP95WithinBudget() =>
        WithinBudget(("POST /api/cart/summary: p95 (ms)", Budgets.SummaryApiP95Ms));

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

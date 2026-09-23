using Microsoft.Playwright;
using Reqnroll;
using Shouldly;
using WaracleStore.CouponTests.Pages.Components;
using WaracleStore.CouponTests.Support;
using WaracleStore.CouponTests.Support.Pricing;

namespace WaracleStore.CouponTests.StepDefinitions;

[Binding]
public sealed class SummarySteps(ScenarioState state, OrderSummaryComponent summary)
{
    private OrderSummaryComponent Summary => summary;

    [Then("the shipping charge shown is {decimal}")]
    public Task ThenShippingIs(decimal expected) =>
        Assertions.Expect(Summary.ShippingRow).ToContainTextAsync(Money.Format(expected));

    /// <summary>Explicit expected values from the feature file, so a reviewer can check the arithmetic by eye.</summary>
    [Then("the order summary shows:")]
    public Task ThenSummaryShows(DisplayedSummary expected) => AssertSummaryAsync(expected, "the values in the feature file");

    /// <summary>Expected values derived from the acceptance criteria by <see cref="PricingOracle"/> for whatever the scenario has done so far.</summary>
    [Then("the order summary is priced according to the release rules")]
    public Task ThenSummaryFollowsRules()
    {
        var quote = PricingOracle.Quote(state.Basket, state.CouponCode);
        var expected = new DisplayedSummary(quote.Subtotal, quote.HasDiscount ? quote.Discount : null, quote.Shipping, quote.Total, CouponLabel: null);
        return AssertSummaryAsync(expected, $"AC-2/3/4 applied to {state.DescribeBasket()} with coupon \"{state.CouponCode}\"");
    }

    private async Task AssertSummaryAsync(DisplayedSummary expected, string basis)
    {
        // Web-first wait on the total: the summary is re-priced asynchronously after every
        // change, so this retries until the store settles (or the timeout proves it never will).
        try
        {
            await Assertions.Expect(Summary.TotalRow).ToContainTextAsync(Money.Format(expected.Total));
        }
        catch (PlaywrightException)
        {
            // Fall through: the detailed comparison below produces the useful message.
        }

        var displayed = await Summary.ReadAsync();

        AmountAssertions.ShouldAllMatch(
            $"Order summary does not match {basis}.{Environment.NewLine}  expected:  {expected}{Environment.NewLine}  displayed: {displayed}",
            ("subtotal", expected.Subtotal, displayed.Subtotal),
            ("discount", expected.Discount, displayed.Discount),
            ("shipping", expected.Shipping, displayed.Shipping),
            ("total", expected.Total, displayed.Total));
    }
}

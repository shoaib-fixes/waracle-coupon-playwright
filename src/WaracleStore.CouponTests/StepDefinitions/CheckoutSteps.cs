using Microsoft.Playwright;
using Reqnroll;
using Shouldly;
using WaracleStore.CouponTests.Pages;
using WaracleStore.CouponTests.Support;
using WaracleStore.CouponTests.Support.Pricing;

namespace WaracleStore.CouponTests.StepDefinitions;

[Binding]
public sealed class CheckoutSteps(ScenarioState state, CartPage cartPage, CheckoutPage checkoutPage, OrderConfirmationPage confirmationPage)
{
    [When("I proceed to checkout")]
    public async Task WhenProceedToCheckout()
    {
        await cartPage.ProceedToCheckoutButton.ClickAsync();
        await checkoutPage.WaitForAsync();
    }

    [When("I complete checkout with sample shipping and card details")]
    public async Task WhenCompleteCheckout()
    {
        await checkoutPage.FillShippingAsync(ShippingDetails.Sample);
        await checkoutPage.FillPaymentAsync(CardDetails.Sample);

        // Remember what the customer was shown at the moment of paying, for AC-6 comparisons.
        state.CheckoutSummary = await checkoutPage.Summary.ReadAsync();
        state.PayButtonAmount = await checkoutPage.PayButtonAmountAsync();

        await checkoutPage.PlaceOrderAsync();
    }

    [Then("the order confirmation is shown")]
    public async Task ThenConfirmationShown()
    {
        await confirmationPage.WaitForAsync();
        state.Confirmation = await confirmationPage.ReadAsync();
    }

    [Then("the order confirmation shows the coupon {string}")]
    public async Task ThenConfirmationShowsCoupon(string code)
    {
        await Assertions.Expect(confirmationPage.CouponRow).ToBeVisibleAsync();
        await Assertions.Expect(confirmationPage.CouponRow).ToContainTextAsync(code, new LocatorAssertionsToContainTextOptions { IgnoreCase = true });
    }

    [Then("the order confirmation shows no coupon line")]
    public Task ThenConfirmationShowsNoCoupon() => Assertions.Expect(confirmationPage.CouponRow).ToHaveCountAsync(0);

    [Then("the order confirmation shows the same discount and total as the checkout summary")]
    public void ThenConfirmationMatchesCheckout()
    {
        var checkout = state.CheckoutSummary.ShouldNotBeNull("checkout summary was not captured");
        var confirmation = Confirmation();

        AmountAssertions.ShouldAllMatch(
            $"Confirmation disagrees with what the checkout showed.{Environment.NewLine}  checkout:     {checkout}{Environment.NewLine}  confirmation: {confirmation}",
            ("discount", checkout.Discount, confirmation.Discount),
            ("total", checkout.Total, confirmation.TotalPaid));
    }

    [Then("the order confirmation is priced according to the release rules")]
    public void ThenConfirmationFollowsRules()
    {
        var quote = PricingOracle.Quote(state.Basket, state.CouponCode);
        var confirmation = Confirmation();

        AmountAssertions.ShouldAllMatch(
            $"Confirmation does not follow AC-2/3/4 for coupon \"{state.CouponCode}\".{Environment.NewLine}  expected:     {quote}{Environment.NewLine}  confirmation: {confirmation}",
            ("discount", quote.HasDiscount ? quote.Discount : null, confirmation.Discount),
            ("total paid", quote.Total, confirmation.TotalPaid));
    }

    [Then("the total paid is {decimal}")]
    public void ThenTotalPaidIs(decimal expected) => Confirmation().TotalPaid.ShouldBe(expected);

    [Then("the total paid equals the amount that was on the Pay button")]
    public void ThenTotalPaidEqualsPayButton() =>
        Confirmation().TotalPaid.ShouldBe(state.PayButtonAmount.ShouldNotBeNull("Pay button amount was not captured"));

    private DisplayedConfirmation Confirmation() =>
        state.Confirmation.ShouldNotBeNull("the order confirmation has not been read; add 'Then the order confirmation is shown' first");
}

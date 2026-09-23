using System.Text.Json;
using Reqnroll;
using Shouldly;
using WaracleStore.CouponTests.Support;
using WaracleStore.CouponTests.Support.Data;
using WaracleStore.CouponTests.Support.Drivers;
using WaracleStore.CouponTests.Support.Pricing;

namespace WaracleStore.CouponTests.StepDefinitions;

[Binding]
public sealed class CouponApiSteps(ApiDriver api, ScenarioState state)
{
    // ---- cart summary -------------------------------------------------------------------------

    [When("I request a cart summary for {string}")]
    public Task WhenRequestSummary(string basket) => RequestSummaryAsync(BasketParser.FromText(basket), coupon: null);

    [When("I request a cart summary for {string} with coupon {string}")]
    public Task WhenRequestSummary(string basket, string coupon) => RequestSummaryAsync(BasketParser.FromText(basket), coupon);

    [When("I request a cart summary for {string} with coupon {string} surrounded by spaces")]
    public Task WhenRequestSummaryPadded(string basket, string coupon) => RequestSummaryAsync(BasketParser.FromText(basket), $"   {coupon}   ");

    [When("I request a cart summary for an empty basket with coupon {string}")]
    public Task WhenRequestSummaryEmpty(string coupon) => RequestSummaryAsync([], coupon);

    [When("I request a cart summary for an unknown product")]
    public Task WhenRequestSummaryUnknownProduct() =>
        api.PostAsync(ApiRoutes.CartSummary, new { items = new[] { new { productId = "p-does-not-exist", quantity = 1 } } });

    // ---- orders ---------------------------------------------------------------------------------

    [When("I place an order for {string} with coupon {string}")]
    public Task WhenPlaceOrder(string basket, string coupon) => PlaceOrderAsync(BasketParser.FromText(basket), coupon, signedIn: true);

    [When("I place an order for {string} without a coupon")]
    public Task WhenPlaceOrderNoCoupon(string basket) => PlaceOrderAsync(BasketParser.FromText(basket), coupon: null, signedIn: true);

    [When("I place an order for {string} with coupon {string} without signing in")]
    public Task WhenPlaceOrderAnonymously(string basket, string coupon) => PlaceOrderAsync(BasketParser.FromText(basket), coupon, signedIn: false);

    [When("I fetch the order I just placed")]
    public Task WhenFetchPlacedOrder()
    {
        var id = state.PlacedOrder.ShouldNotBeNull("no order has been placed in this scenario").GetProperty("id").GetString();
        return api.GetOrderAsync(id!);
    }

    // ---- assertions -----------------------------------------------------------------------------

    [Then("the response status is {int}")]
    public void ThenStatus(int expected) =>
        api.LastResponse.ShouldNotBeNull().Status.ShouldBe(expected, $"Response body: {api.LastBody}");

    [Then("the response message mentions {string}")]
    public void ThenMessageMentions(string text) =>
        api.LastJson.GetProperty("message").GetString().ShouldNotBeNull().ShouldContain(text, Case.Insensitive);

    [Then("the response reports the coupon as applied")]
    public void ThenCouponApplied()
    {
        var pricing = LastPricing();
        pricing.CouponApplied.ShouldBeTrue($"couponApplied was false: {pricing}");
        pricing.CouponCode.ShouldNotBeNull("couponCode was null although couponApplied was true");
    }

    [Then("the response reports the coupon as not applied")]
    public void ThenCouponNotApplied()
    {
        var pricing = LastPricing();
        pricing.CouponApplied.ShouldBeFalse($"couponApplied was true: {pricing}");
        pricing.CouponCode.ShouldBeNull("couponCode should be null when no coupon applies");
    }

    [Then("the response has no discount")]
    public void ThenNoDiscount() => LastPricing().Discount.ShouldBe(0m);

    [Then("the response is priced according to the release rules")]
    public void ThenPricedByRules()
    {
        var quote = PricingOracle.Quote(state.Basket, state.CouponCode);
        var pricing = LastPricing();

        AmountAssertions.ShouldAllMatch(
            $"API pricing does not follow AC-2/3/4 for {state.DescribeBasket()} with coupon \"{state.CouponCode}\".{Environment.NewLine}  expected: {quote}{Environment.NewLine}  response: {pricing}",
            ("subtotal", quote.Subtotal, pricing.Subtotal),
            ("discount", quote.Discount, pricing.Discount),
            ("shipping", quote.Shipping, pricing.Shipping),
            ("total", quote.Total, pricing.Total));
    }

    [Then("the order records the coupon {string}")]
    public void ThenOrderRecordsCoupon(string code) =>
        OrderElement().GetProperty("couponCode").GetString().ShouldBe(code, StringCompareShould.IgnoreCase);

    [Then("the order records no coupon")]
    public void ThenOrderRecordsNoCoupon()
    {
        OrderElement().GetProperty("couponCode").ValueKind.ShouldBe(JsonValueKind.Null);
        LastPricing().Discount.ShouldBe(0m);
    }

    [Then("the fetched order matches the placed order")]
    public void ThenFetchedMatchesPlaced()
    {
        var placed = state.PlacedOrder.ShouldNotBeNull("no order has been placed in this scenario");
        var fetched = OrderElement();

        fetched.GetProperty("orderNumber").GetString().ShouldBe(placed.GetProperty("orderNumber").GetString());
        ApiPricing.FromOrder(fetched).ShouldBe(ApiPricing.FromOrder(placed));
    }

    [Then("the order amounts match the cart summary for the same basket and coupon")]
    public async Task ThenOrderMatchesSummary()
    {
        var order = LastPricing();
        await RequestSummaryAsync(state.Basket, state.CouponCode);
        var summary = LastPricing();

        AmountAssertions.ShouldAllMatch(
            $"The order was charged differently from what the summary quoted.{Environment.NewLine}  summary: {summary}{Environment.NewLine}  order:   {order}",
            ("subtotal", summary.Subtotal, order.Subtotal),
            ("discount", summary.Discount, order.Discount),
            ("shipping", summary.Shipping, order.Shipping),
            ("total", summary.Total, order.Total));
    }

    // ---- helpers --------------------------------------------------------------------------------

    private Task RequestSummaryAsync(IReadOnlyList<BasketLine> basket, string? coupon)
    {
        Remember(basket, coupon);
        return api.CartSummaryAsync(state.Basket, coupon);
    }

    private async Task PlaceOrderAsync(IReadOnlyList<BasketLine> basket, string? coupon, bool signedIn)
    {
        Remember(basket, coupon);
        var response = await api.PlaceOrderAsync(state.Basket, coupon, signedIn);

        if (response.Ok)
            state.PlacedOrder = api.LastJson.GetProperty("order");
    }

    private void Remember(IReadOnlyList<BasketLine> basket, string? coupon)
    {
        state.ReplaceBasket(basket);
        state.CouponCode = coupon;
    }

    /// <summary>The pricing block of the last response, whether it was a summary or an order.</summary>
    private ApiPricing LastPricing() =>
        api.LastJson.TryGetProperty("order", out var order) ? ApiPricing.FromOrder(order) : ApiPricing.FromSummary(api.LastJson);

    private JsonElement OrderElement() =>
        api.LastJson.TryGetProperty("order", out var order)
            ? order
            : throw new ShouldAssertException($"The last response is not an order: {api.LastBody}");

}

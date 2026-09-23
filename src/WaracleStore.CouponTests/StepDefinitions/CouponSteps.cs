using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Reqnroll;
using Shouldly;
using WaracleStore.CouponTests.Pages;
using WaracleStore.CouponTests.Support;
using WaracleStore.CouponTests.Support.Drivers;

namespace WaracleStore.CouponTests.StepDefinitions;

[Binding]
public sealed partial class CouponSteps(BrowserDriver driver, ScenarioState state, CartPage cartPage)
{
    [When("I apply the coupon {string}")]
    public Task WhenApplyCoupon(string code) => ApplyAsync(code);

    [When("I apply the coupon {string} surrounded by spaces")]
    public Task WhenApplyCouponPadded(string code) => ApplyAsync($"   {code}   ");

    [When("I apply an empty coupon code")]
    public Task WhenApplyEmptyCoupon() => ApplyAsync(string.Empty);

    [When("I apply a coupon code made only of spaces")]
    public Task WhenApplySpacesCoupon() => ApplyAsync("     ");

    [When("I apply a coupon code of {int} characters")]
    public Task WhenApplyLongCoupon(int length) => ApplyAsync(new string('W', length));

    [Then("the cart confirms the coupon {string} is applied")]
    public async Task ThenCouponConfirmed(string code)
    {
        await Assertions.Expect(cartPage.CouponAppliedNote).ToBeVisibleAsync();
        await Assertions.Expect(cartPage.CouponAppliedNote).ToContainTextAsync(code, new LocatorAssertionsToContainTextOptions { IgnoreCase = true });
    }

    [Then("the cart confirms the coupon is applied")]
    public Task ThenCouponConfirmed() => Assertions.Expect(cartPage.CouponAppliedNote).ToBeVisibleAsync();

    /// <summary>
    /// Works on the cart and the checkout: both render the same Order Summary card.
    /// Also bound as a When so scenarios can use it as a synchronisation point
    /// ("And the order summary shows a coupon line" before the next action).
    /// </summary>
    [When("the order summary shows a coupon line for {string}")]
    [Then("the order summary shows a coupon line for {string}")]
    public async Task ThenCouponLineFor(string code)
    {
        await Assertions.Expect(cartPage.Summary.CouponRow).ToBeVisibleAsync();
        await Assertions.Expect(cartPage.Summary.CouponRow).ToContainTextAsync(code, new LocatorAssertionsToContainTextOptions { IgnoreCase = true });
    }

    [Then("the order summary shows a coupon line")]
    public Task ThenCouponLine() => Assertions.Expect(cartPage.Summary.CouponRow).ToBeVisibleAsync();

    [Then("no discount is applied")]
    public async Task ThenNoDiscount()
    {
        // The summary re-prices asynchronously after Apply. Wait for the store to settle on
        // "no coupon" rather than asserting on whatever happened to be rendered first.
        await Assertions.Expect(cartPage.Summary.CouponRow).ToHaveCountAsync(0);
        await Assertions.Expect(cartPage.CouponAppliedNote).ToHaveCountAsync(0);
    }

    [Then("I am shown a clear message that the coupon was not accepted")]
    public async Task ThenClearRejectionMessage()
    {
        var rejection = driver.Page.GetByText(RejectionWording());
        try
        {
            await Assertions.Expect(rejection).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 3_000 });
        }
        catch (PlaywrightException)
        {
            var shown = state.ToastsAfterCoupon.Count == 0
                ? "no message at all"
                : string.Join(" | ", state.ToastsAfterCoupon.Select(t => $"\"{t}\""));

            throw new ShouldAssertException(
                $"AC-5 expects a clear message when the coupon \"{state.CouponCode}\" is rejected " +
                $"(wording such as invalid / not recognised / enter a code). The store showed: {shown}.");
        }
    }

    private async Task ApplyAsync(string code)
    {
        await cartPage.ApplyCouponAsync(code);
        state.CouponCode = code;
        state.ToastsAfterCoupon = await CaptureToastsAsync();
    }

    /// <summary>Toasts appear synchronously with the click and vanish ~3 s later, so grab them now for later diagnostics.</summary>
    private async Task<IReadOnlyList<string>> CaptureToastsAsync()
    {
        try
        {
            await cartPage.Toasts.All.First.WaitForAsync(new LocatorWaitForOptions { Timeout = 1_000 });
        }
        catch (TimeoutException)
        {
            // No toast is itself a finding for AC-5; the assertion step reports it.
        }

        return await cartPage.Toasts.TextsAsync();
    }

    [GeneratedRegex("invalid|not valid|not recognised|not recognized|unknown|expired|incorrect|does not exist|enter a|required|empty|missing", RegexOptions.IgnoreCase)]
    private static partial Regex RejectionWording();
}

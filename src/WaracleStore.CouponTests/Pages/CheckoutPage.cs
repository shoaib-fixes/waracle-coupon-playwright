using System.Text.RegularExpressions;
using Microsoft.Playwright;
using WaracleStore.CouponTests.Pages.Components;
using WaracleStore.CouponTests.Support.Data;
using WaracleStore.CouponTests.Support.Drivers;
using WaracleStore.CouponTests.Support.Pricing;

namespace WaracleStore.CouponTests.Pages;

/// <summary>
/// The single-page checkout. Its inputs have labels that are not associated with the
/// fields (no <c>for</c>/<c>id</c>), so they are located by placeholder. See README → "Testability notes".
/// </summary>
public sealed class CheckoutPage(BrowserDriver driver)
{
    private IPage Page => driver.Page;

    public OrderSummaryComponent Summary => new(driver);

    public ILocator PayButton => Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { NameRegex = new Regex("^Pay ") });

    public ILocator AddressField => Page.GetByPlaceholder("10 Digital Drive");

    public ILocator CityField => Page.GetByPlaceholder("Edinburgh");

    public ILocator PostcodeField => Page.GetByPlaceholder("EH1 1AA");

    public ILocator CardNumberField => Page.GetByPlaceholder("4242 4242 4242 4242");

    public ILocator ExpiryField => Page.GetByPlaceholder("12 / 26");

    public ILocator CvcField => Page.GetByPlaceholder("123");

    public async Task WaitForAsync()
    {
        await Assertions.Expect(Page).ToHaveURLAsync(new Regex("/checkout$"));
        await Assertions.Expect(PayButton).ToBeVisibleAsync();
    }

    public async Task FillShippingAsync(ShippingDetails shipping)
    {
        await AddressField.FillAsync(shipping.Address);
        await CityField.FillAsync(shipping.City);
        await PostcodeField.FillAsync(shipping.Postcode);
    }

    public async Task FillPaymentAsync(CardDetails card)
    {
        await CardNumberField.FillAsync(card.Number);
        await ExpiryField.FillAsync(card.Expiry);
        await CvcField.FillAsync(card.Cvc);
    }

    public async Task<decimal> PayButtonAmountAsync() => Money.Parse(await PayButton.InnerTextAsync());

    public Task PlaceOrderAsync() => PayButton.ClickAsync();
}

using System.Text.RegularExpressions;
using Microsoft.Playwright;
using WaracleStore.CouponTests.Pages.Components;
using WaracleStore.CouponTests.Support.Drivers;

namespace WaracleStore.CouponTests.Pages;

public sealed class CartPage(BrowserDriver driver)
{
    private IPage Page => driver.Page;

    public OrderSummaryComponent Summary => new(Page);

    public ToastComponent Toasts => new(Page);

    public ILocator Heading => Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Shopping Cart", Exact = true });

    public ILocator EmptyState => Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Your cart is empty", Exact = true });

    public ILocator CouponField => Page.GetByPlaceholder("e.g. WARACLE25");

    public ILocator ApplyCouponButton => Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Apply", Exact = true });

    /// <summary>The inline "Coupon “CODE” applied" confirmation under the coupon field.</summary>
    public ILocator CouponAppliedNote => Summary.Root.GetByText(new Regex("^Coupon .+ applied$"));

    public ILocator ProceedToCheckoutButton => Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Proceed to Checkout", Exact = true });

    public async Task OpenAsync()
    {
        await driver.GotoAsync("/cart");
        await Assertions.Expect(Heading.Or(EmptyState)).ToBeVisibleAsync();
    }

    public async Task ApplyCouponAsync(string code)
    {
        await CouponField.FillAsync(code);
        await ApplyCouponButton.ClickAsync();
    }

    public ILocator LineItem(string productName) =>
        Page.Locator(".card").Filter(new LocatorFilterOptions
        {
            Has = Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = productName, Exact = true }),
        });

    public ILocator QuantityOf(string productName) =>
        LineItem(productName).GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "+", Exact = true })
            .Locator("xpath=preceding-sibling::span[1]");

    public Task IncreaseQuantityAsync(string productName) =>
        LineItem(productName).GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "+", Exact = true }).ClickAsync();

    public Task RemoveAsync(string productName) =>
        LineItem(productName).GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Remove", Exact = true }).ClickAsync();
}

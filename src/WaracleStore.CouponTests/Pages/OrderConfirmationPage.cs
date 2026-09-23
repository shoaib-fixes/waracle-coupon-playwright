using System.Text.RegularExpressions;
using Microsoft.Playwright;
using WaracleStore.CouponTests.Support.Drivers;
using WaracleStore.CouponTests.Support.Pricing;

namespace WaracleStore.CouponTests.Pages;

public sealed record DisplayedConfirmation(string OrderNumber, decimal TotalPaid, string? CouponLabel, decimal? Discount)
{
    public override string ToString() =>
        $"order {OrderNumber}, coupon {(CouponLabel ?? "none")}, discount {(Discount is { } d ? Money.Format(d) : "none")}, total paid {Money.Format(TotalPaid)}";
}

public sealed class OrderConfirmationPage(BrowserDriver driver)
{
    private IPage Page => driver.Page;

    public ILocator Heading => Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Thank you for your order!", Exact = true });

    public ILocator OrderNumber => Detail("Order Number");

    public ILocator TotalPaid => Detail("Total Paid");

    /// <summary>The "Coupon (CODE)" line in the itemised list. Absent when no discount applied.</summary>
    public ILocator CouponRow => Page.Locator("div:has(> span:text-matches(\"^Coupon \\\\(\"))");

    public async Task WaitForAsync()
    {
        await Assertions.Expect(Page).ToHaveURLAsync(new Regex("/order-success/"));
        await Assertions.Expect(Heading).ToBeVisibleAsync();
    }

    public async Task<DisplayedConfirmation> ReadAsync()
    {
        await Assertions.Expect(TotalPaid).ToBeVisibleAsync();
        var hasCoupon = await CouponRow.CountAsync() > 0;
        return new DisplayedConfirmation(
            (await OrderNumber.InnerTextAsync()).Trim(),
            Money.Parse(await TotalPaid.InnerTextAsync()),
            hasCoupon ? (await CouponRow.Locator("span").First.InnerTextAsync()).Trim() : null,
            hasCoupon ? Math.Abs(Money.Parse(await CouponRow.Locator("span").Last.InnerTextAsync())) : null);
    }

    /// <summary>The "LABEL / value" pairs in the order card, located by their label.</summary>
    private ILocator Detail(string label) => Page.Locator($"div:has(> p:text-is(\"{label}\"))").Locator("p").Last;
}

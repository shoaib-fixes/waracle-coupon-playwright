using Microsoft.Playwright;
using WaracleStore.CouponTests.Support.Pricing;

namespace WaracleStore.CouponTests.Pages.Components;

/// <summary>What the order summary currently shows, parsed into numbers.</summary>
public sealed record DisplayedSummary(decimal Subtotal, decimal? Discount, decimal Shipping, decimal Total, string? CouponLabel)
{
    public override string ToString() =>
        $"subtotal {Money.Format(Subtotal)}, discount {(Discount is { } d ? Money.Format(d) : "none")}, shipping {Money.Format(Shipping)}, total {Money.Format(Total)}";
}

/// <summary>
/// The "Order Summary" card. The cart and the checkout render the same rows
/// (Subtotal / Coupon (CODE) / Shipping / Total), so one component serves both pages.
/// </summary>
/// <remarks>
/// The store exposes no test ids or ARIA structure on these rows, so rows are located by their
/// visible label and the amount is the last cell of that row. See README → "Testability notes".
/// </remarks>
public sealed class OrderSummaryComponent
{
    private readonly ILocator _root;

    public OrderSummaryComponent(IPage page)
    {
        _root = page.Locator(".card").Filter(new LocatorFilterOptions
        {
            Has = page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Order Summary", Exact = true }),
        });
    }

    public ILocator Root => _root;

    public ILocator SubtotalRow => Row("Subtotal");

    public ILocator ShippingRow => Row("Shipping");

    public ILocator TotalRow => Row("Total");

    /// <summary>The discount line, labelled "Coupon (CODE)". Absent when no discount applies.</summary>
    public ILocator CouponRow => _root.Locator("div:has(> span:text-matches(\"^Coupon \\\\(\"))");

    public async Task<decimal> SubtotalAsync() => await AmountAsync(SubtotalRow);

    public async Task<decimal> ShippingAsync() => await AmountAsync(ShippingRow);

    public async Task<decimal> TotalAsync() => await AmountAsync(TotalRow);

    /// <summary>The discount as a positive amount, or null when no coupon line is shown.</summary>
    public async Task<decimal?> DiscountAsync()
    {
        if (await CouponRow.CountAsync() == 0)
            return null;
        return Math.Abs(await AmountAsync(CouponRow));
    }

    public async Task<string?> CouponLabelAsync()
    {
        if (await CouponRow.CountAsync() == 0)
            return null;
        return (await CouponRow.Locator("span").First.InnerTextAsync()).Trim();
    }

    public async Task<DisplayedSummary> ReadAsync()
    {
        await Assertions.Expect(TotalRow).ToBeVisibleAsync();
        return new DisplayedSummary(
            await SubtotalAsync(),
            await DiscountAsync(),
            await ShippingAsync(),
            await TotalAsync(),
            await CouponLabelAsync());
    }

    private ILocator Row(string label) => _root.Locator($"div:has(> span:text-is(\"{label}\"))");

    private static async Task<decimal> AmountAsync(ILocator row) =>
        Money.Parse(await row.Locator("span").Last.InnerTextAsync());
}

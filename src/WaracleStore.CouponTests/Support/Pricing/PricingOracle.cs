using WaracleStore.CouponTests.Support.Data;

namespace WaracleStore.CouponTests.Support.Pricing;

/// <summary>
/// The expected pricing behaviour, written from the WS-101 acceptance criteria and
/// nothing else. Tests compare what the application shows against this, so a
/// scenario fails when the app disagrees with the spec, not when it disagrees with itself.
/// </summary>
/// <remarks>
/// AC-2: WARACLE25 reduces the subtotal by 25%.
/// AC-3: Standard shipping of £5.00 applies to any non-empty basket.
/// AC-4: Order total = subtotal − discount + shipping.
/// The ACs do not state a rounding rule; half-up to the penny is assumed (see README).
/// The ACs do not state whether the code is case-sensitive; the launch code is accepted
/// in any case here because that is what the release does, and the README flags it.
/// </remarks>
public static class PricingOracle
{
    public const string LaunchCoupon = "WARACLE25";
    public const decimal CouponRate = 0.25m;
    public const decimal StandardShipping = 5.00m;

    public static bool IsValidCoupon(string? code) =>
        string.Equals(code?.Trim(), LaunchCoupon, StringComparison.OrdinalIgnoreCase);

    public static PriceBreakdown Quote(IReadOnlyCollection<BasketLine> lines, string? couponCode = null)
    {
        var subtotal = RoundToPenny(lines.Sum(l => l.LineTotal));
        var discount = IsValidCoupon(couponCode) ? RoundToPenny(subtotal * CouponRate) : 0m;
        var shipping = subtotal > 0m ? StandardShipping : 0m;
        var total = RoundToPenny(subtotal - discount + shipping);
        return new PriceBreakdown(subtotal, discount, shipping, total);
    }

    private static decimal RoundToPenny(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

public sealed record PriceBreakdown(decimal Subtotal, decimal Discount, decimal Shipping, decimal Total)
{
    public bool HasDiscount => Discount > 0m;

    public override string ToString() =>
        $"subtotal {Money.Format(Subtotal)}, discount {Money.Format(Discount)}, shipping {Money.Format(Shipping)}, total {Money.Format(Total)}";
}

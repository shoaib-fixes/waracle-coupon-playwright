using Shouldly;
using WaracleStore.CouponTests.Support.Data;
using WaracleStore.CouponTests.Support.Pricing;

namespace WaracleStore.CouponTests.UnitTests;

/// <summary>
/// The oracle is what the UI scenarios compare against, so it is checked on its own here
/// with values a reviewer can verify by hand.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.All)]
public sealed class PricingOracleTests
{
    private static BasketLine Line(string product, int quantity)
    {
        var p = Catalogue.ByName(product);
        return new BasketLine(p, quantity, p.DefaultSize);
    }

    [Test]
    public void No_coupon_charges_subtotal_plus_flat_shipping()
    {
        var quote = PricingOracle.Quote([Line("Waracle Cap", 1)]);

        quote.ShouldBe(new PriceBreakdown(19.99m, 0m, 5.00m, 24.99m));
    }

    [TestCase("WARACLE25")]
    [TestCase("waracle25")]
    [TestCase("  WARACLE25  ")]
    public void Launch_coupon_takes_a_quarter_off_the_subtotal(string code)
    {
        var quote = PricingOracle.Quote([Line("Waracle Headset", 2)], code);

        quote.Subtotal.ShouldBe(119.98m);
        quote.Discount.ShouldBe(30.00m, "29.995 rounds half-up to the penny");
        quote.Shipping.ShouldBe(5.00m);
        quote.Total.ShouldBe(94.98m);
    }

    [TestCase("WARACLE50")]
    [TestCase("WARACLE25EXTRA")]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void Any_other_code_gives_no_discount(string? code)
    {
        var quote = PricingOracle.Quote([Line("Waracle Cap", 1)], code);

        quote.Discount.ShouldBe(0m);
        quote.HasDiscount.ShouldBeFalse();
        quote.Total.ShouldBe(24.99m);
    }

    [Test]
    public void Discount_is_rounded_to_the_penny_before_the_total_is_computed()
    {
        // 3 × 24.99 = 74.97; 25% = 18.7425 → 18.74; total = 74.97 − 18.74 + 5 = 61.23
        var quote = PricingOracle.Quote([Line("Men's Logo T-Shirt", 3)], "WARACLE25");

        quote.Discount.ShouldBe(18.74m);
        quote.Total.ShouldBe(61.23m);
    }

    [Test]
    public void Empty_basket_has_no_shipping_and_no_discount()
    {
        var quote = PricingOracle.Quote([], "WARACLE25");

        quote.ShouldBe(new PriceBreakdown(0m, 0m, 0m, 0m));
    }

    [Test]
    public void Mixed_basket_sums_every_line()
    {
        var quote = PricingOracle.Quote([Line("Waracle Headset", 2), Line("Waracle Cap", 1)], "WARACLE25");

        quote.Subtotal.ShouldBe(139.97m);
        quote.Discount.ShouldBe(34.99m);
        quote.Total.ShouldBe(109.98m);
    }
}

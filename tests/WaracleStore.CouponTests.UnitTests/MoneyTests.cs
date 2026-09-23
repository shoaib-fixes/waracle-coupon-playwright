using Shouldly;
using WaracleStore.CouponTests.Support.Pricing;

namespace WaracleStore.CouponTests.UnitTests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public sealed class MoneyTests
{
    [TestCase("£24.99", 24.99)]
    [TestCase("£1,234.56", 1234.56)]
    [TestCase("£1234.56", 1234.56)]
    [TestCase("Pay £64.99", 64.99)]
    [TestCase("£5", 5)]
    public void Parses_gbp_amounts(string text, decimal expected) => Money.Parse(text).ShouldBe(expected);

    [TestCase("–£30.00")] // en dash, as rendered on the cart and confirmation
    [TestCase("−£30.00")] // minus sign, as rendered on the checkout
    [TestCase("-£30.00")] // hyphen-minus
    public void Treats_every_sign_glyph_as_negative(string text) => Money.Parse(text).ShouldBe(-30.00m);

    [Test]
    public void Rejects_text_without_an_amount() =>
        Should.Throw<FormatException>(() => Money.Parse("free shipping"));

    [Test]
    public void Formats_with_pound_sign_and_two_decimals() => Money.Format(1234.5m).ShouldBe("£1,234.50");
}

using Reqnroll;
using WaracleStore.CouponTests.Pages.Components;
using WaracleStore.CouponTests.Support.Pricing;

namespace WaracleStore.CouponTests.Support.Transforms;

/// <summary>Lets steps take <c>£24.99</c> straight from Gherkin as a <see cref="decimal"/>.</summary>
[Binding]
public sealed class StepTransforms
{
    [StepArgumentTransformation(@"(£[\d,]+\.\d{2})")]
    public decimal ToMoney(string text) => Money.Parse(text);

    /// <summary>
    /// A one-row table with Subtotal / Discount / Shipping / Total columns. Discount may be
    /// "none" to assert that no discount line is shown at all.
    /// </summary>
    [StepArgumentTransformation]
    public DisplayedSummary ToExpectedSummary(Table table)
    {
        if (table.RowCount != 1)
            throw new ArgumentException("Expected exactly one row of Subtotal | Discount | Shipping | Total.");

        var row = table.Rows[0];
        var discountText = row["Discount"].Trim();
        decimal? discount = discountText.Equals("none", StringComparison.OrdinalIgnoreCase) ? null : Money.Parse(discountText);

        return new DisplayedSummary(
            Money.Parse(row["Subtotal"]),
            discount,
            Money.Parse(row["Shipping"]),
            Money.Parse(row["Total"]),
            CouponLabel: null);
    }
}

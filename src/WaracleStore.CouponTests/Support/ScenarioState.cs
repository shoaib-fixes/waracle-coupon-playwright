using WaracleStore.CouponTests.Pages;
using WaracleStore.CouponTests.Pages.Components;
using WaracleStore.CouponTests.Support.Data;
using WaracleStore.CouponTests.Support.Pricing;

namespace WaracleStore.CouponTests.Support;

/// <summary>
/// What the scenario has done so far, so later steps can compute what the store *should*
/// show. One instance per scenario via Reqnroll's scenario container; never static.
/// </summary>
public sealed class ScenarioState
{
    public List<BasketLine> Basket { get; } = [];

    /// <summary>The coupon text most recently submitted, exactly as typed. Null when none has been submitted.</summary>
    public string? CouponCode { get; set; }

    /// <summary>Toast messages captured immediately after the last coupon submission.</summary>
    public IReadOnlyList<string> ToastsAfterCoupon { get; set; } = [];

    public DisplayedSummary? CheckoutSummary { get; set; }

    public decimal? PayButtonAmount { get; set; }

    public DisplayedConfirmation? Confirmation { get; set; }

    /// <summary>The <c>order</c> object returned by POST /api/orders in an API scenario.</summary>
    public System.Text.Json.JsonElement? PlacedOrder { get; set; }

    /// <summary>Measurements taken by performance scenarios, keyed by metric name, in milliseconds.</summary>
    public Dictionary<string, double> Timings { get; } = new();

    /// <summary>Replaces the basket. Safe to call with <see cref="Basket"/> itself.</summary>
    public void ReplaceBasket(IEnumerable<BasketLine> lines)
    {
        var snapshot = lines.ToList();
        Basket.Clear();
        foreach (var line in snapshot)
            AddToBasket(line);
    }

    public string DescribeBasket() =>
        Basket.Count == 0
            ? "an empty basket"
            : string.Join(", ", Basket.Select(l => $"{l.Quantity} x {l.Product.Name} @ {Money.Format(l.Product.Price)}"));

    public void AddToBasket(BasketLine line)
    {
        var existing = Basket.FindIndex(l => l.Product.Id == line.Product.Id && l.Size == line.Size);
        if (existing >= 0)
            Basket[existing] = Basket[existing] with { Quantity = Basket[existing].Quantity + line.Quantity };
        else
            Basket.Add(line);
    }

    public void ChangeQuantity(string productName, int delta)
    {
        var index = Basket.IndexOf(SingleLineNamed(productName));
        Basket[index] = Basket[index] with { Quantity = Basket[index].Quantity + delta };
    }

    public void Remove(string productName) => Basket.Remove(SingleLineNamed(productName));

    /// <summary>
    /// Cart steps address lines by product name, which is unambiguous only while a product
    /// appears in one size; a scenario that needs two sizes of one product must address lines
    /// by size as well, so this refuses to guess.
    /// </summary>
    private BasketLine SingleLineNamed(string productName)
    {
        var matches = Basket.Where(l => string.Equals(l.Product.Name, productName, StringComparison.OrdinalIgnoreCase)).ToList();
        return matches.Count switch
        {
            1 => matches[0],
            0 => throw new InvalidOperationException($"'{productName}' is not in the scenario's basket."),
            _ => throw new InvalidOperationException($"'{productName}' is in the basket in {matches.Count} sizes; steps address lines by name only."),
        };
    }
}

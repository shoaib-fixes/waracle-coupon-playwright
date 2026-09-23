using WaracleStore.CouponTests.Pages;
using WaracleStore.CouponTests.Pages.Components;
using WaracleStore.CouponTests.Support.Data;

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
        var index = Basket.FindIndex(l => string.Equals(l.Product.Name, productName, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
            throw new InvalidOperationException($"'{productName}' is not in the scenario's basket.");
        Basket[index] = Basket[index] with { Quantity = Basket[index].Quantity + delta };
    }

    public void Remove(string productName) =>
        Basket.RemoveAll(l => string.Equals(l.Product.Name, productName, StringComparison.OrdinalIgnoreCase));
}

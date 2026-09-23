namespace WaracleStore.CouponTests.Support.Data;

public sealed record Product(string Id, string Name, decimal Price, string Image, IReadOnlyList<string> Sizes)
{
    public string DefaultSize => Sizes[0];
}

/// <summary>
/// The nine-product sample catalogue that ships with the store (web, API and mobile all share it).
/// Prices are inputs to the scenarios, not the thing under test; the coupon maths is checked
/// by <see cref="Pricing.PricingOracle"/>.
/// </summary>
public static class Catalogue
{
    private static readonly IReadOnlyList<Product> Products =
    [
        new("p-mens-tee", "Men's Logo T-Shirt", 24.99m, "tshirt-black", ["XS", "S", "M", "L", "XL", "XXL"]),
        new("p-womens-tee", "Women's Logo T-Shirt", 24.99m, "tee-w-black", ["XS", "S", "M", "L", "XL"]),
        new("p-headset", "Waracle Headset", 59.99m, "headset", ["One Size"]),
        new("p-mens-shorts", "Men's Logo Shorts", 29.99m, "shorts-black", ["S", "M", "L", "XL"]),
        new("p-womens-shorts", "Women's Logo Shorts", 29.99m, "shorts-heather", ["XS", "S", "M", "L"]),
        new("p-womens-white-tee", "Women's White T-Shirt", 24.99m, "tee-w-white", ["XS", "S", "M", "L", "XL"]),
        new("p-mens-grey-tee", "Men's Grey T-Shirt", 24.99m, "tshirt-grey", ["S", "M", "L", "XL", "XXL"]),
        new("p-cap", "Waracle Cap", 19.99m, "cap", ["One Size"]),
        new("p-headset-premium", "Waracle Premium Headset", 79.99m, "headset", ["One Size"]),
    ];

    public static Product ByName(string name)
    {
        var normalised = name.Trim().Replace('’', '\'');
        return Products.FirstOrDefault(p => string.Equals(p.Name, normalised, StringComparison.OrdinalIgnoreCase))
               ?? throw new ArgumentException(
                   $"Unknown product '{name}'. Known products: {string.Join(", ", Products.Select(p => p.Name))}", nameof(name));
    }
}

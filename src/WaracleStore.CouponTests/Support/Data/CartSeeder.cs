using System.Text.Json;

namespace WaracleStore.CouponTests.Support.Data;

/// <summary>
/// Builds the JSON the store keeps in <c>localStorage["waracle_cart"]</c>, so a scenario
/// can start on the cart page with a known basket instead of clicking through the catalogue.
/// The shape mirrors the app's <c>CartItem</c> type exactly.
/// </summary>
public static class CartSeeder
{
    public const string StorageKey = "waracle_cart";

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string ToStorageJson(IEnumerable<BasketLine> lines) =>
        JsonSerializer.Serialize(
            lines.Select(l => new
            {
                productId = l.Product.Id,
                name = l.Product.Name,
                price = l.Product.Price,
                image = l.Product.Image,
                size = l.Size,
                quantity = l.Quantity,
            }),
            Options);
}

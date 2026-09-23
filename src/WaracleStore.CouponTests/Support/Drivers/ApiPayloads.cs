using WaracleStore.CouponTests.Support.Data;

namespace WaracleStore.CouponTests.Support.Drivers;

/// <summary>Request bodies for the store's API, built from the same basket model the UI scenarios use.</summary>
public static class ApiPayloads
{
    /// <summary>Body for <c>POST /api/cart/summary</c>.</summary>
    public static object CartSummary(IEnumerable<BasketLine> basket, string? couponCode) => new
    {
        items = basket.Select(l => new { productId = l.Product.Id, quantity = l.Quantity }).ToArray(),
        couponCode,
    };

    /// <summary>Body for <c>POST /api/orders</c>, with the sample customer and address.</summary>
    public static object Order(IEnumerable<BasketLine> basket, string? couponCode) => new
    {
        items = basket.Select(l => new { productId = l.Product.Id, quantity = l.Quantity, size = l.Size }).ToArray(),
        couponCode,
        customer = new { name = CustomerDetails.Sample.Name, email = CustomerDetails.Sample.Email },
        shipping = new
        {
            address = ShippingDetails.Sample.Address,
            city = ShippingDetails.Sample.City,
            postcode = ShippingDetails.Sample.Postcode,
            country = ShippingDetails.Sample.Country,
        },
    };
}

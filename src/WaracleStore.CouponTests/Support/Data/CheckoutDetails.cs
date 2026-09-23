namespace WaracleStore.CouponTests.Support.Data;

/// <summary>Sample customer, address and card details for placing orders through the UI or the API.</summary>
public sealed record CustomerDetails(string Name, string Email)
{
    public static CustomerDetails Sample => new("John Doe", "john.doe@example.com");
}

public sealed record ShippingDetails(string Address, string City, string Postcode, string Country = "United Kingdom")
{
    public static ShippingDetails Sample => new("10 Digital Drive", "Edinburgh", "EH1 1AA");
}

public sealed record CardDetails(string Number, string Expiry, string Cvc)
{
    public static CardDetails Sample => new("4242 4242 4242 4242", "12 / 26", "123");
}

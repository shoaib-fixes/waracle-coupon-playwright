namespace WaracleStore.CouponTests.Support.Drivers;

/// <summary>The store's REST routes, relative to <c>ApiUrl</c>.</summary>
public static class ApiRoutes
{
    public const string Health = "/api/health";

    public const string Login = "/api/auth/login";

    public const string CartSummary = "/api/cart/summary";

    public const string Orders = "/api/orders";

    public static string Order(string id) => $"{Orders}/{id}";
}

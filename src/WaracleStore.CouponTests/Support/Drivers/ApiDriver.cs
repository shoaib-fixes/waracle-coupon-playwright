using System.Text.Json;
using Microsoft.Playwright;
using WaracleStore.CouponTests.Support.Config;
using WaracleStore.CouponTests.Support.Data;
using WaracleStore.CouponTests.Support.Pricing;

namespace WaracleStore.CouponTests.Support.Drivers;

/// <summary>The price fields both <c>POST /api/cart/summary</c> and an order carry.</summary>
public sealed record ApiPricing(decimal Subtotal, decimal Discount, decimal Shipping, decimal Total, string? CouponCode, bool CouponApplied)
{
    public override string ToString() =>
        $"subtotal {Money.Format(Subtotal)}, discount {Money.Format(Discount)}, shipping {Money.Format(Shipping)}, total {Money.Format(Total)}, coupon {(CouponCode ?? "none")}, applied {CouponApplied}";

    public static ApiPricing FromSummary(JsonElement summary) => new(
        summary.GetProperty("subtotal").GetDecimal(),
        summary.GetProperty("discount").GetDecimal(),
        summary.GetProperty("shipping").GetDecimal(),
        summary.GetProperty("total").GetDecimal(),
        summary.GetProperty("couponCode").ValueKind == JsonValueKind.String ? summary.GetProperty("couponCode").GetString() : null,
        summary.GetProperty("couponApplied").GetBoolean());

    public static ApiPricing FromOrder(JsonElement order)
    {
        var coupon = order.GetProperty("couponCode").ValueKind == JsonValueKind.String ? order.GetProperty("couponCode").GetString() : null;
        return new ApiPricing(
            order.GetProperty("subtotal").GetDecimal(),
            order.GetProperty("discount").GetDecimal(),
            order.GetProperty("shipping_cost").GetDecimal(),
            order.GetProperty("total").GetDecimal(),
            coupon,
            coupon is not null);
    }
}

/// <summary>
/// Scenario-scoped HTTP client for the store's REST API, built on Playwright's request
/// context so API scenarios need no browser and no extra HTTP library. Remembers the last
/// response so assertion steps can inspect it.
/// </summary>
public sealed class ApiDriver(PlaywrightHost host, TestSettings settings) : IAsyncDisposable
{
    private IAPIRequestContext? _api;
    private JsonElement? _lastJson;

    public IAPIResponse? LastResponse { get; private set; }

    public string LastBody { get; private set; } = string.Empty;

    /// <summary>The last response body as JSON. Throws with the raw body when it was not a JSON object.</summary>
    public JsonElement LastJson =>
        _lastJson ?? throw new InvalidOperationException($"The last response was not a JSON object: {LastBody}");

    public Task<IAPIResponse> CartSummaryAsync(IEnumerable<BasketLine> basket, string? couponCode) =>
        PostAsync(ApiRoutes.CartSummary, ApiPayloads.CartSummary(basket, couponCode));

    public Task<IAPIResponse> PlaceOrderAsync(IEnumerable<BasketLine> basket, string? couponCode, bool signedIn = true) =>
        PostAsync(ApiRoutes.Orders, ApiPayloads.Order(basket, couponCode), signedIn);

    public Task<IAPIResponse> GetOrderAsync(string id) => GetAsync(ApiRoutes.Order(id), signedIn: true);

    public Task<IAPIResponse> PostAsync(string path, object body, bool signedIn = false) =>
        SendAsync(async api => await api.PostAsync(path, new APIRequestContextOptions
        {
            DataObject = body,
            Headers = signedIn ? BearerHeader() : null,
        }));

    public Task<IAPIResponse> GetAsync(string path, bool signedIn = false) =>
        SendAsync(async api => await api.GetAsync(path, new APIRequestContextOptions
        {
            Headers = signedIn ? BearerHeader() : null,
        }));

    private async Task<IAPIResponse> SendAsync(Func<IAPIRequestContext, Task<IAPIResponse>> send)
    {
        _api ??= await host.Playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
        {
            BaseURL = settings.ApiUrl,
            Timeout = settings.DefaultTimeoutMs,
        });

        var response = await send(_api);
        LastResponse = response;
        LastBody = await response.TextAsync();
        _lastJson = null;
        if (LastBody.TrimStart().StartsWith('{'))
        {
            using var document = JsonDocument.Parse(LastBody);
            _lastJson = document.RootElement.Clone();
        }

        return response;
    }

    private Dictionary<string, string> BearerHeader() => new() { ["Authorization"] = $"Bearer {host.AuthToken}" };

    public async ValueTask DisposeAsync()
    {
        if (_api is not null)
            await _api.DisposeAsync();
    }
}

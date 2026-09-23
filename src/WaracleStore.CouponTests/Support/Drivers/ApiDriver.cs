using System.Text.Json;
using Microsoft.Playwright;
using WaracleStore.CouponTests.Support.Config;
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
public sealed class ApiDriver(PlaywrightRunner runner, TestSettings settings) : IAsyncDisposable
{
    private IAPIRequestContext? _api;

    public IAPIResponse? LastResponse { get; private set; }

    public JsonElement LastJson { get; private set; }

    public string LastBody { get; private set; } = string.Empty;

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
        _api ??= await runner.Playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
        {
            BaseURL = settings.ApiUrl,
            Timeout = settings.DefaultTimeoutMs,
        });

        var response = await send(_api);
        LastResponse = response;
        LastBody = await response.TextAsync();
        LastJson = LastBody.Length > 0 && LastBody.TrimStart().StartsWith('{')
            ? JsonDocument.Parse(LastBody).RootElement
            : default;
        return response;
    }

    private Dictionary<string, string> BearerHeader() => new() { ["Authorization"] = $"Bearer {runner.AuthToken}" };

    public async ValueTask DisposeAsync()
    {
        if (_api is not null)
            await _api.DisposeAsync();
    }
}

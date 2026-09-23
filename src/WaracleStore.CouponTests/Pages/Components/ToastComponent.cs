using Microsoft.Playwright;
using WaracleStore.CouponTests.Support.Drivers;

namespace WaracleStore.CouponTests.Pages.Components;

/// <summary>
/// The transient notifications the store shows bottom-right. They auto-dismiss after ~3 s,
/// so callers must assert with web-first <c>Expect</c> calls rather than read-then-compare.
/// </summary>
public sealed class ToastComponent(BrowserDriver driver)
{
    /// <summary>Every toast currently on screen. Toasts have no role or test id; the animation class is their only stable hook.</summary>
    public ILocator All => driver.Page.Locator(".animate-toast-in");

    public ILocator WithText(string text) => All.Filter(new LocatorFilterOptions { HasText = text });

    /// <summary>Message text only: the leading one-character icon (✓ / ! / i) is dropped.</summary>
    public async Task<IReadOnlyList<string>> TextsAsync() =>
        (await All.AllInnerTextsAsync())
            .Select(t => string.Join(" ", t.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Where(line => line.Length > 1)))
            .ToList();
}

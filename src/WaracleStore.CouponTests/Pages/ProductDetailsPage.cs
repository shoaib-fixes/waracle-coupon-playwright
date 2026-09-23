using Microsoft.Playwright;
using WaracleStore.CouponTests.Support.Data;
using WaracleStore.CouponTests.Support.Drivers;

namespace WaracleStore.CouponTests.Pages;

public sealed class ProductDetailsPage(BrowserDriver driver)
{
    private IPage Page => driver.Page;

    public ILocator Title => Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Level = 1 });

    public ILocator AddToCartButton => Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Add to Cart", Exact = true });

    public ILocator IncreaseQuantityButton => Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "+", Exact = true });

    public ILocator SizeButton(string size) => Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = size, Exact = true });

    public async Task OpenAsync(Product product)
    {
        await driver.GotoAsync($"/products/{product.Id}");
        await Assertions.Expect(Title).ToHaveTextAsync(product.Name);
    }

    public async Task AddToCartAsync(string size, int quantity)
    {
        await SizeButton(size).ClickAsync();
        for (var i = 1; i < quantity; i++)
            await IncreaseQuantityButton.ClickAsync();
        await AddToCartButton.ClickAsync();
    }
}

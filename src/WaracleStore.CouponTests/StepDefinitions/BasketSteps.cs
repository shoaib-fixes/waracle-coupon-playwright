using Microsoft.Playwright;
using Reqnroll;
using WaracleStore.CouponTests.Pages;
using WaracleStore.CouponTests.Pages.Components;
using WaracleStore.CouponTests.Support;
using WaracleStore.CouponTests.Support.Data;
using WaracleStore.CouponTests.Support.Drivers;

namespace WaracleStore.CouponTests.StepDefinitions;

[Binding]
public sealed class BasketSteps(BrowserDriver driver, ScenarioState state, CartPage cartPage, ProductDetailsPage productPage, ToastComponent toasts)
{
    [Given("my basket contains:")]
    public Task GivenBasketContains(Table table) => SeedBasketAsync(BasketParser.FromTable(table));

    [Given("my basket contains {string}")]
    public Task GivenBasketContains(string basket) => SeedBasketAsync(BasketParser.FromText(basket));

    [Given("my basket is empty")]
    public void GivenBasketIsEmpty() => state.Basket.Clear();

    [When("I add {int} x {string} to the cart from its product page")]
    public async Task WhenAddFromProductPage(int quantity, string productName)
    {
        var product = Catalogue.ByName(productName);
        await productPage.OpenAsync(product);
        await productPage.AddToCartAsync(product.DefaultSize, quantity);
        await Assertions.Expect(toasts.WithText($"{product.Name} added to cart")).ToBeVisibleAsync();
        state.AddToBasket(new BasketLine(product, quantity, product.DefaultSize));
    }

    [When("I open the cart")]
    public Task WhenOpenCart() => cartPage.OpenAsync();

    [When("I increase the quantity of {string}")]
    public async Task WhenIncreaseQuantity(string productName)
    {
        var before = int.Parse(await cartPage.QuantityOf(productName).InnerTextAsync());
        await cartPage.IncreaseQuantityAsync(productName);
        await Assertions.Expect(cartPage.QuantityOf(productName)).ToHaveTextAsync((before + 1).ToString());
        state.ChangeQuantity(productName, +1);
    }

    [When("I remove {string} from the cart")]
    public async Task WhenRemove(string productName)
    {
        await cartPage.RemoveAsync(productName);
        await Assertions.Expect(cartPage.LineItem(productName)).ToHaveCountAsync(0);
        state.Remove(productName);
    }

    [Then("the cart shows the empty basket message")]
    public Task ThenEmptyBasketMessage() => Assertions.Expect(cartPage.EmptyState).ToBeVisibleAsync();

    [Then("no order summary is shown")]
    public Task ThenNoOrderSummary() => Assertions.Expect(cartPage.Summary.Root).ToHaveCountAsync(0);

    [Then("the cart is still usable")]
    public async Task ThenCartStillUsable()
    {
        await Assertions.Expect(cartPage.Heading).ToBeVisibleAsync();
        await Assertions.Expect(cartPage.CouponField).ToBeEditableAsync();
        await Assertions.Expect(cartPage.ProceedToCheckoutButton).ToBeEnabledAsync();
    }

    private Task SeedBasketAsync(IReadOnlyList<BasketLine> lines)
    {
        state.ReplaceBasket(lines);
        return driver.SeedLocalStorageAsync(StorageKeys.Cart, CartSeeder.ToStorageJson(state.Basket));
    }
}

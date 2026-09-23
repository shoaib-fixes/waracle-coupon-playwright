using Microsoft.Playwright;
using WaracleStore.CouponTests.Support.Drivers;

namespace WaracleStore.CouponTests.Pages;

public sealed class LoginPage(BrowserDriver driver)
{
    private IPage Page => driver.Page;

    public ILocator EmailField => Page.GetByLabel("Email", new PageGetByLabelOptions { Exact = true });

    public ILocator PasswordField => Page.GetByLabel("Password", new PageGetByLabelOptions { Exact = true });

    public ILocator SignInButton => Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Sign In", Exact = true });

    public ILocator ErrorBanner => Page.GetByText("Incorrect email or password.");

    public Task OpenAsync() => driver.GotoAsync("/login");

    public async Task SignInAsync(string email, string password)
    {
        await EmailField.FillAsync(email);
        await PasswordField.FillAsync(password);
        await SignInButton.ClickAsync();
    }
}

using Microsoft.Playwright;
using WaracleStore.CouponTests.Support.Drivers;

namespace WaracleStore.CouponTests.Pages.Components;

/// <summary>The site header shown on every page except login and registration.</summary>
public sealed class NavbarComponent(BrowserDriver driver)
{
    /// <summary>"Hi, {first name}" — the only visible sign that the customer is signed in.</summary>
    public ILocator Greeting(string firstName) => driver.Page.GetByText($"Hi, {firstName}", new PageGetByTextOptions { Exact = true });

    public ILocator CartLink => driver.Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Cart", Exact = true });
}

using Microsoft.Playwright;
using Reqnroll;
using WaracleStore.CouponTests.Pages;
using WaracleStore.CouponTests.Support.Config;
using WaracleStore.CouponTests.Support.Drivers;

namespace WaracleStore.CouponTests.StepDefinitions;

[Binding]
public sealed class SessionSteps(BrowserDriver driver, TestSettings settings, LoginPage loginPage)
{
    /// <summary>
    /// Signs in without the login form: the JWT obtained once through the API is placed where
    /// the web app keeps it (<c>localStorage["waracle_token"]</c>) before the app boots.
    /// The end-to-end journey covers the login form itself.
    /// </summary>
    [Given("I am signed in as the demo customer")]
    public Task GivenSignedIn() => driver.SeedLocalStorageAsync("waracle_token", driver.AuthToken);

    [Given("I am on the login page")]
    public Task GivenOnLoginPage() => loginPage.OpenAsync();

    [When("I sign in with the demo credentials")]
    public Task WhenSignInWithDemoCredentials() =>
        loginPage.SignInAsync(settings.Credentials.Email, settings.Credentials.Password);

    [Then("I am greeted as {string}")]
    public Task ThenGreetedAs(string firstName) =>
        Assertions.Expect(driver.Page.GetByText($"Hi, {firstName}")).ToBeVisibleAsync();
}
